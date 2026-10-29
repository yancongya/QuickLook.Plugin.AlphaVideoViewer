using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuickLook.Plugin.AlphaVideoViewer.Probing
{
    /// <summary>拼接布局：None = 不像 Alpha 拼接视频</summary>
    public enum AlphaLayout
    {
        None,
        SideBySide,   // 左右拼接（宽 > 高）
        TopBottom,    // 上下拼接（高 > 宽）
        Vapc,         // 腾讯 VAP v2：vapc 顶级盒携带权威对位坐标，几何/像素判定全部让位
    }

    /// <summary>
    /// 腾讯 VAP v2 的 vapc 盒元数据（官方权威对位信息，payload 是包着 info 字段的 UTF-8 JSON）。
    /// 所有坐标为整帧像素（videoW×videoH），Rect* 已归一化到 0..1，可直接喂给 WebGL 合成器。
    /// </summary>
    public class VapMetadata
    {
        public double VideoW;         // 整帧宽（归一化分母）
        public double VideoH;         // 整帧高
        public double OutW;           // 输出内容宽 = info.w
        public double OutH;           // 输出内容高 = info.h
        public double[] RgbRect;      // [x, y, w, h] 归一化，画面矩形
        public double[] AlphaRect;    // [x, y, w, h] 归一化，遮罩矩形（可能半分辨率）
        public bool IsVapx;           // true = 逐帧 rect 序列（frame[]），当前未支持，仅记录
        public int Fps;
    }

    public class ProbeResult
    {
        public int Width;
        public int Height;
        public AlphaLayout Layout = AlphaLayout.None;

        /// <summary>vapc 元数据；仅当 Layout == Vapc 时非 null。</summary>
        public VapMetadata Vap;

        /// <summary>时长（秒），来自 mvhd；解析不到则为 0。供 FrameProbe 按比例 seek 取样帧。</summary>
        public double DurationSeconds;

        /// <summary>
        /// 窄边占比（0..1）：短边 / 长边。
        /// =1 表示两个半区都是正方形（严格 2:1 素材），&lt;1 表示另一边是窄条或内容不满（VAP 常见）。
        /// 仅作参考；页面端会用逐帧像素自检重新判定实际拼接布局，不依赖此值。
        /// </summary>
        public double AlphaScale = 1.0;

        public DateTime CheckedAt;
    }

    /// <summary>
    /// 轻量 ISO BMFF（MP4/MOV/M4V）容器探测：只读 ftyp、tkhd、mvhd 与 vapc box，不解码任何帧，毫秒级完成。
    /// vapc（腾讯 VAP v2 顶级盒）存在且可解析时直接定案为 Vapc 布局，后续不再走几何/像素判定。
    ///
    /// 判定放宽为「可被对半切分」而不是「严格 2:1」——真实产业素材（腾讯 VAP、字节 AlphaPlayer、
    /// YYEVA）的 alpha 半区普遍做了降采样，整帧几乎不会正好 2:1。规则：
    ///   · 长边沿其法线方向切半，切出的内容矩形宽高比落在 [MIN_SPLIT, MAX_SPLIT] → 接管
    ///   · 方向由长短边决定：宽 &gt; 高 → SideBySide；高 &gt; 宽 → TopBottom
    ///   · 16:9(1.78) 切半后内容 0.89:1（等价 1.125，超出上限）→ 不接管；
    ///     9:16 竖屏同理，从 2.5 起才可能命中，但 9:16=0.5625 落在下限之外 → 不接管
    /// 误判风险由前端像素自检（一半近灰度）+ 「普通视频模式」开关兜住。
    /// 结果按路径缓存 60 秒（QuickLook 对同一文件先 CanHandle 后 View，必须命中缓存；期间文件被改动则立即失效）。
    /// </summary>
    public static class Mp4Probe
    {
        // 切半后内容矩形的长/短边比允许区间；1.0 = 正方形内容（严格 2:1 素材）
        private const double MinContentAspect = 1.0;
        private const double MaxContentAspect = 2.5;
        // 极接近正方形时两个切法都行，按默认约定取左右
        private const double SquareEpsilon = 0.02;

        private const long MaxMoovSize = 64L * 1024 * 1024;
        private const long MaxVapcSize = 64L * 1024;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
        private static readonly ConcurrentDictionary<string, ProbeResult> Cache =
            new ConcurrentDictionary<string, ProbeResult>();

        // 每次新建实例并盖时间戳：共享静态实例会让「非 Alpha」缓存永不过期、每次 CanHandle 重复解析
        private static ProbeResult None() =>
            new ProbeResult { Layout = AlphaLayout.None, CheckedAt = DateTime.Now };

        public static ProbeResult Probe(string path)
        {
            if (Cache.TryGetValue(path, out var cached) &&
                DateTime.Now - cached.CheckedAt < CacheTtl)
                return cached;

            ProbeResult result;
            try
            {
                result = Parse(path);
            }
            catch
            {
                result = None();
            }

            if (Cache.Count > 512)
            {
                var keys = new List<string>(Cache.Keys);
                foreach (var key in keys)
                {
                    if (Cache.TryGetValue(key, out var old) &&
                        DateTime.Now - old.CheckedAt >= CacheTtl)
                        Cache.TryRemove(key, out _);
                }
            }

            Cache[path] = result;
            return result;
        }

        private static ProbeResult Parse(string path)
        {
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length < 16) return None();

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] moov, vapc;
                ScanTopLevel(fs, out moov, out vapc);

                // vapc 是腾讯 VAP 的官方权威对位信息：命中即定案，几何初筛与像素自检全部让位
                if (vapc != null)
                {
                    var vap = ParseVapc(vapc);
                    if (vap != null)
                    {
                        double vapDuration = 0.0;
                        if (moov != null)
                        {
                            foreach (var child in Boxes(moov, 0, moov.Length))
                            {
                                if (child.Key != "mvhd") continue;
                                ReadDuration(child.Value, out vapDuration);
                                break;
                            }
                        }

                        return new ProbeResult
                        {
                            Width = (int)Math.Round(vap.VideoW),
                            Height = (int)Math.Round(vap.VideoH),
                            Layout = AlphaLayout.Vapc,
                            Vap = vap,
                            DurationSeconds = vapDuration,
                            CheckedAt = DateTime.Now
                        };
                    }
                }

                if (moov == null) return None();

                int bestW = 0, bestH = 0;
                double duration = 0.0;
                foreach (var child in Boxes(moov, 0, moov.Length))
                {
                    if (child.Key == "mvhd")
                    {
                        ReadDuration(child.Value, out duration);
                        continue;
                    }
                    if (child.Key != "trak") continue;
                    foreach (var tkhd in Boxes(child.Value, 0, child.Value.Length))
                    {
                        if (tkhd.Key != "tkhd") continue;
                        ReadDimensions(tkhd.Value, out int w, out int h);
                        if ((long)w * h > (long)bestW * bestH)
                        {
                            bestW = w;
                            bestH = h;
                        }
                    }
                }

                if (bestW <= 0 || bestH <= 0) return None();

                double longSide = Math.Max(bestW, bestH);
                double shortSide = Math.Min(bestW, bestH);

                // 长边切半后得到内容矩形 (longSide/2) × shortSide；
                // 其长/短边比 = max(long/2, short) / min(long/2, short)
                double half = longSide / 2.0;
                double hi = Math.Max(half, shortSide);
                double lo = Math.Min(half, shortSide);
                if (lo < 1.0) return None();

                double contentAspect = hi / lo;
                if (contentAspect < MinContentAspect || contentAspect > MaxContentAspect) return None();

                // 近乎正方形时两个切法都可行，按默认约定取左右
                AlphaLayout layout;
                if (bestH >= bestW * (1.0 + SquareEpsilon)) layout = AlphaLayout.TopBottom;
                else layout = AlphaLayout.SideBySide;

                return new ProbeResult
                {
                    Width = bestW,
                    Height = bestH,
                    Layout = layout,
                    DurationSeconds = duration,
                    // 短边 / 长边：alpha 窄区相对正方形内容区的占比
                    AlphaScale = shortSide / longSide,
                    CheckedAt = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 顶层 box 顺序扫描：一次遍历同时抓 moov 与 vapc 两个顶级盒（vapc 可能出现在 moov 之前，
        /// 例：vap_demo.mp4 是 ftyp→vapc→free→moov→mdat）；mdat 等按 size 跳过，复杂度 O(box 数)。
        /// </summary>
        private static void ScanTopLevel(FileStream fs, out byte[] moov, out byte[] vapc)
        {
            moov = null;
            vapc = null;
            long len = fs.Length;
            long pos = 0;
            var header = new byte[16];

            while (pos + 8 <= len && (moov == null || vapc == null))
            {
                fs.Seek(pos, SeekOrigin.Begin);
                if (!ReadExact(fs, header, 0, 8)) return;

                uint size32 = Be32(header, 0);
                string type = Ascii(header, 4, 4);

                long size = size32;
                int headerLen = 8;

                if (size32 == 1)
                {
                    if (!ReadExact(fs, header, 8, 8)) return;
                    size = Be64(header, 8);
                    headerLen = 16;
                }
                else if (size32 == 0)
                {
                    size = len - pos; // box 延伸到文件尾
                }

                if (size < headerLen || pos + size > len) return;

                if (pos == 0 && type != "ftyp") return; // 首个 box 必须是 ftyp

                if (type == "moov" && moov == null)
                {
                    long payloadLen = size - headerLen;
                    if (payloadLen > MaxMoovSize) return;
                    var buf = new byte[payloadLen];
                    if (!ReadExact(fs, buf, 0, (int)payloadLen)) return;
                    moov = buf;
                }
                else if (type == "vapc" && vapc == null)
                {
                    long payloadLen = size - headerLen;
                    if (payloadLen <= MaxVapcSize)
                    {
                        var buf = new byte[payloadLen];
                        if (!ReadExact(fs, buf, 0, (int)payloadLen)) return;
                        vapc = buf;
                    }
                }

                pos += size;
            }
        }

        /// <summary>
        /// vapc payload = 包着 info 字段的 UTF-8 JSON（w/h/videoW/videoH/fps/isVapx/aFrame/rgbFrame）。
        /// 坐标是整帧像素；这里归一化到 0..1 并校验边界。缺字段/越界/坏 JSON → null，走几何兜底。
        /// </summary>
        private static VapMetadata ParseVapc(byte[] payload)
        {
            if (payload == null || payload.Length < 16) return null;
            try
            {
                // box payload 理论上可能带尾部填充，截到最后一个 '}' 为止
                int end = Array.LastIndexOf(payload, (byte)'}');
                if (end < 0) return null;
                var json = new byte[end + 1];
                Array.Copy(payload, json, end + 1);

                using (var doc = JsonDocument.Parse(json))
                {
                    var info = doc.RootElement.GetProperty("info");
                    double videoW = info.GetProperty("videoW").GetDouble();
                    double videoH = info.GetProperty("videoH").GetDouble();
                    double outW = info.GetProperty("w").GetDouble();
                    double outH = info.GetProperty("h").GetDouble();
                    if (videoW <= 0 || videoH <= 0 || outW <= 0 || outH <= 0) return null;

                    var rgb = ReadRect(info.GetProperty("rgbFrame"), videoW, videoH);
                    var alpha = ReadRect(info.GetProperty("aFrame"), videoW, videoH);
                    if (rgb == null || alpha == null) return null;

                    int fps = 0;
                    try { fps = info.GetProperty("fps").GetInt32(); } catch { }
                    bool isVapx = false;
                    try { isVapx = info.GetProperty("isVapx").GetInt32() != 0; } catch { }

                    return new VapMetadata
                    {
                        VideoW = videoW,
                        VideoH = videoH,
                        OutW = outW,
                        OutH = outH,
                        RgbRect = rgb,
                        AlphaRect = alpha,
                        IsVapx = isVapx,
                        Fps = fps
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>[x,y,w,h] 像素坐标 → 归一化；缺项/非正尺寸/越界（容 1px 取整误差）返回 null。</summary>
        private static double[] ReadRect(JsonElement el, double videoW, double videoH)
        {
            if (el.ValueKind != JsonValueKind.Array || el.GetArrayLength() < 4) return null;
            double x = el[0].GetDouble();
            double y = el[1].GetDouble();
            double w = el[2].GetDouble();
            double h = el[3].GetDouble();
            if (w <= 0 || h <= 0) return null;
            const double tol = 1.0;
            if (x < -tol || y < -tol || x + w > videoW + tol || y + h > videoH + tol) return null;
            return new[]
            {
                Math.Max(0, x / videoW),
                Math.Max(0, y / videoH),
                Math.Min(1, w / videoW),
                Math.Min(1, h / videoH)
            };
        }

        /// <summary>tkhd 的宽高固定在 payload 末尾 8 字节，16.16 定点数，与 version 无关。</summary>
        private static void ReadDimensions(byte[] payload, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (payload.Length < 8) return;
            width = (int)Math.Round(Be32(payload, payload.Length - 8) / 65536.0);
            height = (int)Math.Round(Be32(payload, payload.Length - 4) / 65536.0);
        }

        /// <summary>mvhd 时长 = duration / timescale；v0 各字段 4 字节、v1 时间戳与时长为 8 字节。</summary>
        private static void ReadDuration(byte[] payload, out double seconds)
        {
            seconds = 0.0;
            if (payload == null || payload.Length < 4) return;
            if (payload[0] == 1)
            {
                if (payload.Length < 32) return;
                uint timescale = Be32(payload, 20);
                long dur = Be64(payload, 24);
                if (timescale != 0 && dur > 0) seconds = (double)dur / timescale;
            }
            else
            {
                if (payload.Length < 20) return;
                uint timescale = Be32(payload, 12);
                uint dur = Be32(payload, 16);
                if (timescale != 0 && dur > 0) seconds = (double)dur / timescale;
            }
        }

        private static IEnumerable<KeyValuePair<string, byte[]>> Boxes(byte[] buf, int start, int end)
        {
            int p = start;
            while (p + 8 <= end)
            {
                uint size32 = Be32(buf, p);
                string type = Ascii(buf, p + 4, 4);

                long size = size32;
                int headerLen = 8;

                if (size32 == 1)
                {
                    if (p + 16 > end) yield break;
                    size = Be64(buf, p + 8);
                    headerLen = 16;
                }
                else if (size32 == 0)
                {
                    size = end - p;
                }

                if (size < headerLen || p + size > end) yield break;

                int payloadLen = (int)(size - headerLen);
                var payload = new byte[payloadLen];
                Array.Copy(buf, p + headerLen, payload, 0, payloadLen);
                yield return new KeyValuePair<string, byte[]>(type, payload);

                p += (int)size;
            }
        }

        private static bool ReadExact(Stream s, byte[] buf, int offset, int count)
        {
            while (count > 0)
            {
                int read = s.Read(buf, offset, count);
                if (read <= 0) return false;
                offset += read;
                count -= read;
            }
            return true;
        }

        private static uint Be32(byte[] b, int o)
        {
            return ((uint)b[o] << 24) | ((uint)b[o + 1] << 16) | ((uint)b[o + 2] << 8) | b[o + 3];
        }

        private static long Be64(byte[] b, int o)
        {
            return ((long)Be32(b, o) << 32) | Be32(b, o + 4);
        }

        private static string Ascii(byte[] b, int o, int len)
        {
            var chars = new char[len];
            for (int i = 0; i < len; i++) chars[i] = (char)b[o + i];
            return new string(chars);
        }
    }
}
