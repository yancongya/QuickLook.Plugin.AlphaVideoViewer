using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.AlphaVideoViewer.Probing
{
    public enum SplitCheck
    {
        /// <summary>某帧呈现「近灰度遮罩半 + 有色内容半」→ 像拼接素材，接管</summary>
        AlphaLike,
        /// <summary>解码成功且出现过「没有任何近灰度半」的帧（典型普通视频，两半都彩色）→ 交回原生播放器</summary>
        NotAlpha,
        /// <summary>解码失败 / 尺寸不符 / 只遇到无法定论的帧（单色、半黑）→ 不定论，沿用几何初筛结果</summary>
        Unknown,
    }

    /// <summary>
    /// 在 Mp4Probe 的几何初筛之上叠加一道「解码自检」：用 Media Foundation 解几帧真实画面，
    /// 确认存在「近灰度遮罩半 + 有色内容半」的拼接特征才接管；典型普通视频（两半都彩色）交回原生 VideoViewer。
    ///
    /// 设计要点：
    ///   · 同步 IMFSourceReader（不传回调即同步），跑在调用方线程（CanHandle 在 UI 线程），几帧封顶、不读完整文件；
    ///   · 单调安全：只会把「确认普通」的从接管改成交回原生；任何异常/取不到解码器/尺寸不符/单色歧义都返回
    ///     Unknown，由调用处沿用几何初筛 → 绝不因自检失败而误杀真实素材（宁漏勿抢）；
    ///   · 只读像素不渲染、不建窗口、不依赖 WPF 合成。
    /// COM 方法通过手工 vtable 派发调用（取对象虚表槽指针转委托），只依赖槽序号；导出函数用 DllImport。
    /// 假定 x64（QuickLook 为 x64）：x64 下 COM StdCall 即标准调用约定，首个 IntPtr 参数即 this。
    /// 判据与前端逐帧自检同族（一半近灰 × 另一半有色），阈值偏保守。
    /// </summary>
    public static class FrameProbe
    {
        // 近灰度像素：通道极差 <= 该值
        private const int GraySpread = 14;
        // 遮罩半区：近灰占比 >= MaskGrayMin 且 平均色度 <= MaskChromaMax
        private const double MaskGrayMin = 0.86;
        private const double MaskChromaMax = 14.0;
        // 内容半区：近灰占比 <= ContentGrayMax 且 平均色度 >= ContentChromaMin
        private const double ContentGrayMax = 0.70;
        private const double ContentChromaMin = 16.0;

        private static readonly double[] SeekFractions = { 0.10, 0.45, 0.80 };
        private const int SequentialLimit = 24;
        private const long MaxTime100ns = 36_000L * 10_000_000L; // 1 小时上限

        // vtable 槽（含 IUnknown 0..2；序号取自 Windows SDK v7.0A 的 mfreadwrite.h 与 mfobjects.h 中 *Vtbl 结构声明顺序）
        // IMFSourceReader 运行时虚表直接以 IUnknown 起手 + 自有方法（不铺 IMFMediaEventGenerator/IMFMediaSource 继承链）：
        //   3 GetStreamSelection / 4 SetStreamSelection / 5 GetNativeMediaType / 6 GetCurrentMediaType /
        //   7 SetCurrentMediaType / 8 SetCurrentPosition / 9 ReadSample / 10 Flush
        // IMFAttributes（IMFMediaType / IMFSample 皆继承它，属性方法在前且 Get/Set 交错排列）：SetGUID 第 24 槽，实测 round-trip 验证。
        private const int Slot_Attributes_SetGUID = 24;          // IMFAttributes
        private const int Slot_Attributes_SetUINT32 = 21;        // IMFAttributes（SetUINT32 排在 SetItem/Delete* 之后）
        private const int Slot_Attributes_SetUINT64 = 22;        // IMFAttributes
        private const int Slot_Sample_ConvertToContiguous = 41;  // IMFSample（: IMFAttributes，属性方法 3..32 在前 + 自有方法）
        private const int Slot_Buffer_Lock = 3;                  // IMFMediaBuffer（: IUnknown）
        private const int Slot_Buffer_Unlock = 4;
        private const int Slot_Reader_SetCurrentMediaType = 7;   // IMFSourceReader
        private const int Slot_Reader_SetCurrentPosition = 8;
        private const int Slot_Reader_ReadSample = 9;

        private static readonly object InitLock = new object();
        private static bool _mfReady;
        private static bool _mfTried;

        // 按路径缓存解码自检结论，避免同一文件反复预览时重复解码；Unknown 不缓存（环境可能恢复）
        private sealed class CacheEntry { public SplitCheck Result; public DateTime At; }
        private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        public static SplitCheck Check(string path, int width, int height, double durationSeconds)
        {
            if (Cache.TryGetValue(path, out var hit) && DateTime.Now - hit.At < CacheTtl)
                return hit.Result;

            string diag;
            var result = Check(path, width, height, durationSeconds, out diag);
            if (result != SplitCheck.Unknown)
                Cache[path] = new CacheEntry { Result = result, At = DateTime.Now };
            return result;
        }

        public static SplitCheck Check(string path, int width, int height, double durationSeconds, out string diag)
        {
            diag = null;
            if (width <= 16 || height <= 16 || (long)width * height > 8_000_000L)
            {
                diag = "dims out of range";
                return SplitCheck.Unknown;
            }

            if (!EnsureStarted())
            {
                diag = "MFStartup failed / unavailable";
                return SplitCheck.Unknown;
            }

            IntPtr reader = IntPtr.Zero;
            IntPtr mediaType = IntPtr.Zero;
            IntPtr attrs = IntPtr.Zero;
            try
            {
                // 启用「高级视频处理」：让源读取器内部挂载 Video Processor，RGB32 这类需要色度转换的输出才接受（否则 SetCurrentMediaType 报 E_INVALIDARG）。
                int hrA = MFCreateAttributes(out attrs, 1);
                if (hrA >= 0 && attrs != IntPtr.Zero)
                {
                    Guid keyAdv = MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING;
                    Attributes_SetUINT32(attrs, ref keyAdv, 1);
                }
                else { attrs = IntPtr.Zero; }

                int hr = MFCreateSourceReaderFromURL(path, attrs, out reader);
                if (attrs != IntPtr.Zero) { Marshal.Release(attrs); attrs = IntPtr.Zero; }
                if (hr < 0 || reader == IntPtr.Zero)
                {
                    diag = "CreateSourceReader hr=" + hr.ToString("X8");
                    return SplitCheck.Unknown;
                }

                hr = MFCreateMediaType(out mediaType);
                if (hr < 0 || mediaType == IntPtr.Zero)
                {
                    diag = "CreateMediaType hr=" + hr.ToString("X8"); return SplitCheck.Unknown;
                }

                Guid keyMajor = MF_MT_MAJOR_TYPE, valVideo = MFMediaType_Video;
                hr = Attributes_SetGUID(mediaType, ref keyMajor, ref valVideo);
                if (hr < 0) { diag = "SetGUID(major) hr=" + hr.ToString("X8"); return SplitCheck.Unknown; }
                Guid keySub = MF_MT_SUBTYPE, valRgb32 = MFVideoFormat_RGB32;
                hr = Attributes_SetGUID(mediaType, ref keySub, ref valRgb32);
                if (hr < 0) { diag = "SetGUID(subtype) hr=" + hr.ToString("X8"); return SplitCheck.Unknown; }
                // 未压缩输出（RGB32）需要显式帧尺寸，否则同样 E_INVALIDARG。帧大小：高 32 位 = 宽、低 32 位 = 高。
                Guid keyFrame = MF_MT_FRAME_SIZE;
                hr = Attributes_SetUINT64(mediaType, ref keyFrame, ((ulong)(uint)width << 32) | (uint)height);
                if (hr < 0) { diag = "SetUINT64(frameSize) hr=" + hr.ToString("X8"); return SplitCheck.Unknown; }

                hr = Reader_SetCurrentMediaType(reader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, IntPtr.Zero, mediaType);
                if (hr < 0) { diag = "SetCurrentMediaType hr=" + hr.ToString("X8"); return SplitCheck.Unknown; }

                bool sawSplit = false, sawClear = false, sawAny = false;
                if (durationSeconds > 0.0)
                {
                    foreach (var frac in SeekFractions)
                    {
                        long t = (long)(durationSeconds * frac * 10_000_000.0);
                        if (t <= 0 || t > MaxTime100ns) continue;
                        if (Seek(reader, t) < 0) continue;
                        var frame = ReadAndClassify(reader, width, height);
                        if (frame == FrameResult.None) continue;
                        sawAny = true;
                        if (frame == FrameResult.Split) sawSplit = true;
                        else if (frame == FrameResult.ClearNoMask) sawClear = true;
                        if (sawSplit) break;
                    }
                }
                else
                {
                    for (int i = 0; i < SequentialLimit; i++)
                    {
                        var frame = ReadAndClassify(reader, width, height);
                        if (frame == FrameResult.None) break;
                        sawAny = true;
                        if (frame == FrameResult.Split) { sawSplit = true; break; }
                        if (frame == FrameResult.ClearNoMask) sawClear = true;
                    }
                }

                if (!sawAny) diag = "no frame decoded";
                if (sawSplit) { diag = "split found"; return SplitCheck.AlphaLike; }
                if (sawClear) { diag = "ordinary (both halves colorful)"; return SplitCheck.NotAlpha; }
                diag = diag ?? "ambiguous only";
                return SplitCheck.Unknown;
            }
            catch (Exception ex)
            {
                diag = "exception " + ex.GetType().Name + ": " + ex.Message;
                return SplitCheck.Unknown;
            }
            finally
            {
                if (mediaType != IntPtr.Zero) Marshal.Release(mediaType);
                if (reader != IntPtr.Zero) Marshal.Release(reader);
            }
        }

        private enum FrameResult { None, Split, ClearNoMask, Ambiguous }

        private static int Seek(IntPtr reader, long time100ns)
        {
            var pos = new PROPVARIANT { vt = VT_I8, hVal = time100ns };
            Guid timeFormat = Guid.Empty; // GUID_NULL → 100ns 单位
            return Reader_SetCurrentPosition(reader, ref timeFormat, ref pos);
        }

        private static FrameResult ReadAndClassify(IntPtr reader, int width, int height)
        {
            int hr = Reader_ReadSample(reader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0,
                out _, out _, out _, out IntPtr sample);
            if (hr < 0 || sample == IntPtr.Zero) return FrameResult.None;

            try
            {
                hr = Sample_ConvertToContiguousBuffer(sample, out IntPtr buffer);
                if (hr < 0 || buffer == IntPtr.Zero) return FrameResult.None;
                try
                {
                    hr = Buffer_Lock(buffer, out IntPtr data, out _, out uint curLen);
                    if (hr < 0 || data == IntPtr.Zero) return FrameResult.None;
                    try
                    {
                        long need = (long)width * height * 4;
                        if (curLen < need) return FrameResult.None; // 解码尺寸与容器不符
                        return Classify(data, width, height);
                    }
                    finally { Buffer_Unlock(buffer); }
                }
                finally { Marshal.Release(buffer); }
            }
            finally { Marshal.Release(sample); }
        }

        private static FrameResult Classify(IntPtr data, int width, int height)
        {
            var s = new FrameStats();
            int halfW = width / 2, halfH = height / 2;
            int pitch = width * 4;
            int stepX = Math.Max(1, width / 160), stepY = Math.Max(1, height / 160);
            for (int y = 0; y < height; y += stepY)
            {
                bool top = y < halfH;
                int row = y * pitch;
                for (int x = 0; x < width; x += stepX)
                {
                    int o = row + x * 4;
                    int b = Marshal.ReadByte(data, o);
                    int g = Marshal.ReadByte(data, o + 1);
                    int r = Marshal.ReadByte(data, o + 2);
                    int spread = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
                    s.Add(x < halfW, top, spread);
                }
            }

            if (AxisSplit(s.L, s.R) || AxisSplit(s.T, s.B)) return FrameResult.Split;
            bool anyMask = s.L.IsMask() || s.R.IsMask() || s.T.IsMask() || s.B.IsMask();
            if (!anyMask) return FrameResult.ClearNoMask; // 没有近灰度半 → 明确是彩色普通视频
            return FrameResult.Ambiguous; // 有一片灰但没找到明确的彩色另一半（单色/半黑）
        }

        private static bool AxisSplit(Region a, Region b)
        {
            return (a.IsMask() && b.IsContent()) || (b.IsMask() && a.IsContent());
        }

        private static bool EnsureStarted()
        {
            if (_mfReady) return true;
            lock (InitLock)
            {
                if (_mfTried) return _mfReady;
                _mfTried = true;
                try { _mfReady = MFStartup(MF_VERSION, 0) >= 0; }
                catch { _mfReady = false; } // 缺 mfplat → 永久退回几何初筛
                return _mfReady;
            }
        }

        // ---- vtable 手工派发 ----

        private static IntPtr Slot(IntPtr obj, int index)
        {
            IntPtr vtbl = Marshal.ReadIntPtr(obj);
            return Marshal.ReadIntPtr(vtbl, index * IntPtr.Size);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetGuidFn(IntPtr self, ref Guid key, ref Guid value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetUInt32Fn(IntPtr self, ref Guid key, uint value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetUInt64Fn(IntPtr self, ref Guid key, ulong value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetCurrentPositionFn(IntPtr self, ref Guid timeFormat, ref PROPVARIANT position);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ReadSampleFn(IntPtr self, uint streamIndex, uint controlFlags,
            out uint actualStreamIndex, out uint streamFlags, out long timestamp, out IntPtr sample);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetCurrentMediaTypeFn(IntPtr self, uint streamIndex, IntPtr reserved, IntPtr mediaType);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ConvertToContiguousBufferFn(IntPtr self, out IntPtr buffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int BufferLockFn(IntPtr self, out IntPtr data, out int maxLength, out uint currentLength);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int BufferUnlockFn(IntPtr self);

        private static int Attributes_SetGUID(IntPtr mediaType, ref Guid key, ref Guid value)
        {
            var fn = (SetGuidFn)Marshal.GetDelegateForFunctionPointer(Slot(mediaType, Slot_Attributes_SetGUID), typeof(SetGuidFn));
            return fn(mediaType, ref key, ref value);
        }

        private static int Attributes_SetUINT32(IntPtr attrs, ref Guid key, uint value)
        {
            var fn = (SetUInt32Fn)Marshal.GetDelegateForFunctionPointer(Slot(attrs, Slot_Attributes_SetUINT32), typeof(SetUInt32Fn));
            return fn(attrs, ref key, value);
        }

        private static int Attributes_SetUINT64(IntPtr attrs, ref Guid key, ulong value)
        {
            var fn = (SetUInt64Fn)Marshal.GetDelegateForFunctionPointer(Slot(attrs, Slot_Attributes_SetUINT64), typeof(SetUInt64Fn));
            return fn(attrs, ref key, value);
        }

        private static int Reader_SetCurrentPosition(IntPtr reader, ref Guid fmt, ref PROPVARIANT pos)
        {
            var fn = (SetCurrentPositionFn)Marshal.GetDelegateForFunctionPointer(Slot(reader, Slot_Reader_SetCurrentPosition), typeof(SetCurrentPositionFn));
            return fn(reader, ref fmt, ref pos);
        }

        private static int Reader_ReadSample(IntPtr reader, uint stream, uint ctrl,
            out uint actual, out uint flags, out long ts, out IntPtr sample)
        {
            var fn = (ReadSampleFn)Marshal.GetDelegateForFunctionPointer(Slot(reader, Slot_Reader_ReadSample), typeof(ReadSampleFn));
            return fn(reader, stream, ctrl, out actual, out flags, out ts, out sample);
        }

        private static int Reader_SetCurrentMediaType(IntPtr reader, uint stream, IntPtr reserved, IntPtr mt)
        {
            var fn = (SetCurrentMediaTypeFn)Marshal.GetDelegateForFunctionPointer(Slot(reader, Slot_Reader_SetCurrentMediaType), typeof(SetCurrentMediaTypeFn));
            return fn(reader, stream, reserved, mt);
        }

        private static int Sample_ConvertToContiguousBuffer(IntPtr sample, out IntPtr buffer)
        {
            var fn = (ConvertToContiguousBufferFn)Marshal.GetDelegateForFunctionPointer(Slot(sample, Slot_Sample_ConvertToContiguous), typeof(ConvertToContiguousBufferFn));
            return fn(sample, out buffer);
        }

        private static int Buffer_Lock(IntPtr buffer, out IntPtr data, out int maxLen, out uint curLen)
        {
            var fn = (BufferLockFn)Marshal.GetDelegateForFunctionPointer(Slot(buffer, Slot_Buffer_Lock), typeof(BufferLockFn));
            return fn(buffer, out data, out maxLen, out curLen);
        }

        private static int Buffer_Unlock(IntPtr buffer)
        {
            var fn = (BufferUnlockFn)Marshal.GetDelegateForFunctionPointer(Slot(buffer, Slot_Buffer_Unlock), typeof(BufferUnlockFn));
            return fn(buffer);
        }

        // ---- 导出函数（非 COM 方法）----

        private const uint MF_VERSION = 0x00020070;
        private const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
        private const ushort VT_I8 = 20;

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PROPVARIANT
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public long hVal;
        }

        private static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        private static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        private static readonly Guid MF_MT_FRAME_SIZE = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
        private static readonly Guid MFMediaType_Video = new Guid("73646976-0000-0010-8000-00aa00389b71");
        // RGB32 由 DEFINE_MEDIATYPE_GUID(name, D3DFMT_X8R8G8B8) 生成 → Data1 = 22 = 0x16；输出字节序 BGRA
        private static readonly Guid MFVideoFormat_RGB32 = new Guid("00000016-0000-0010-8000-00aa00389b71");
        // 让源读取器挂载 Video Processor，支持 YUV→RGB32 等需转换的输出
        private static readonly Guid MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING = new Guid("0f81da2c-b537-4672-a8b2-a681b17307a3");

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFStartup(uint version, uint flags);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFCreateMediaType(out IntPtr ppType);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFCreateAttributes(out IntPtr ppAttributes, uint initialCapacity);

        [DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int MFCreateSourceReaderFromURL(
            [MarshalAs(UnmanagedType.LPWStr)] string pwszURL, IntPtr pInpAttributes, out IntPtr ppSourceReader);

        private struct Region
        {
            public long N, Gray;
            public double Sum;
            public void Add(int spread) { N++; if (spread <= GraySpread) Gray++; Sum += spread; }
            public double GrayRatio => N == 0 ? 1.0 : (double)Gray / N;
            public double ChromaMean => N == 0 ? 255.0 : Sum / N;
            public bool IsMask() => N > 0 && GrayRatio >= MaskGrayMin && ChromaMean <= MaskChromaMax;
            public bool IsContent() => N > 0 && GrayRatio <= ContentGrayMax && ChromaMean >= ContentChromaMin;
        }

        private struct FrameStats
        {
            public Region L, R, T, B;
            public void Add(bool left, bool top, int spread)
            {
                if (left) L.Add(spread); else R.Add(spread);
                if (top) T.Add(spread); else B.Add(spread);
            }
        }
    }
}
