using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.AlphaVideoViewer.Probing
{
    /// <summary>拼接布局：None = 不是 Alpha 拼接视频</summary>
    public enum AlphaLayout
    {
        None,
        SideBySide,   // 左右拼接（宽高比 ≈ 2:1）
        TopBottom,    // 上下拼接（宽高比 ≈ 1:2）
    }

    public class ProbeResult
    {
        public int Width;
        public int Height;
        public AlphaLayout Layout = AlphaLayout.None;
        public DateTime CheckedAt;
    }

    /// <summary>
    /// 轻量 ISO BMFF（MP4/MOV/M4V）容器探测：只读 ftyp 与 tkhd box，不解码任何帧，毫秒级完成。
    /// 判定规则：视频轨道宽高比 ≈2:1 → SideBySide；≈1:2 → TopBottom；其余一律 None（不接管）。
    /// 容差 ±2%（普通视频 16:9=1.78、4:1 极少见，误抢概率极低）。
    /// 结果按路径缓存 60 秒（QuickLook 对同一文件先 CanHandle 后 View，必须命中缓存；期间文件被改动则立即失效）。
    /// </summary>
    public static class Mp4Probe
    {
        private const double AspectTolerance = 0.02;
        private const long MaxMoovSize = 64L * 1024 * 1024;
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
                var moov = FindMoov(fs);
                if (moov == null) return None();

                int bestW = 0, bestH = 0;
                foreach (var trak in Boxes(moov, 0, moov.Length))
                {
                    if (trak.Key != "trak") continue;
                    foreach (var tkhd in Boxes(trak.Value, 0, trak.Value.Length))
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

                // 左右拼接：整帧宽 = 2×高；上下拼接：整帧高 = 2×宽
                if (Near(bestW, 2.0 * bestH))
                    return new ProbeResult { Width = bestW, Height = bestH, Layout = AlphaLayout.SideBySide, CheckedAt = DateTime.Now };
                if (Near(bestH, 2.0 * bestW))
                    return new ProbeResult { Width = bestW, Height = bestH, Layout = AlphaLayout.TopBottom, CheckedAt = DateTime.Now };

                return None();
            }
        }

        private static bool Near(double actual, double expected)
        {
            return Math.Abs(actual - expected) <= Math.Max(2.0, expected * AspectTolerance);
        }

        /// <summary>顶层 box 顺序扫描；mdat 等按 size 跳过，复杂度 O(box 数)，与文件体积无关。</summary>
        private static byte[] FindMoov(FileStream fs)
        {
            long len = fs.Length;
            long pos = 0;
            var header = new byte[16];

            while (pos + 8 <= len)
            {
                fs.Seek(pos, SeekOrigin.Begin);
                if (!ReadExact(fs, header, 0, 8)) return null;

                uint size32 = Be32(header, 0);
                string type = Ascii(header, 4, 4);

                long size = size32;
                int headerLen = 8;

                if (size32 == 1)
                {
                    if (!ReadExact(fs, header, 8, 8)) return null;
                    size = Be64(header, 8);
                    headerLen = 16;
                }
                else if (size32 == 0)
                {
                    size = len - pos; // box 延伸到文件尾
                }

                if (size < headerLen || pos + size > len) return null;

                if (pos == 0 && type != "ftyp") return null; // 首个 box 必须是 ftyp
                if (type == "moov")
                {
                    long payloadLen = size - headerLen;
                    if (payloadLen > MaxMoovSize) return null;
                    var buf = new byte[payloadLen];
                    if (!ReadExact(fs, buf, 0, (int)payloadLen)) return null;
                    return buf;
                }

                pos += size;
            }
            return null;
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
