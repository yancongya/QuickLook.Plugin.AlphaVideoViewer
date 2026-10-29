using System;
using System.IO;
using System.Windows;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.AlphaVideoViewer.Probing;

namespace QuickLook.Plugin.AlphaVideoViewer
{
    /// <summary>
    /// 接管「Alpha/RGB 左右或上下拼接」的透明视频（MP4/MOV），透明合成播放。
    /// 三级判定：vapc 元数据（腾讯 VAP v2 官方对位坐标）命中即定案；
    /// 无 vapc 时走容器 tkhd 宽高比初筛 + 解码像素自检（FrameProbe）确认拼接特征；
    /// 普通 MP4（两半都彩色）经自检确认后交回内置 VideoViewer，无法定论时才沿用初筛（宁漏勿抢）。
    /// </summary>
    public class Plugin : IViewer
    {
        private static double _lastWidth = 600;
        private static double _lastHeight = 440;

        private AlphaVideoViewerPanel _viewerPanel;

        public int Priority => 100;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path)) return false;
            if (!path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase))
                return false;

            var probe = Mp4Probe.Probe(path);
            if (probe == null || probe.Layout == AlphaLayout.None) return false;

            // vapc 是官方写进文件的权威对位信息，命中即定案，无需像素自检
            if (probe.Layout == AlphaLayout.Vapc) return true;

            // 几何初筛只按宽高比放宽（普通 16:9/4:3 切半后也落在区间内），无法区分真实拼接与普通视频；
            // 再解几帧做像素自检：确认为普通视频（两半都彩色）→ 交回内置 VideoViewer；
            // 命中拼接或无法定论（解码失败/单色歧义）→ 沿用几何初筛接管，细节交前端逐帧自检。宁漏勿抢。
            var check = FrameProbe.Check(path, probe.Width, probe.Height, probe.DurationSeconds);
            if (check == SplitCheck.NotAlpha) return false;
            return true;
        }

        public void Prepare(string path, ContextObject context)
        {
            context.SetPreferredSizeFit(new Size(_lastWidth, _lastHeight), 0.9);
        }

        public void View(string path, ContextObject context)
        {
            _viewerPanel = new AlphaVideoViewerPanel();
            _viewerPanel.SetTheme(context.Theme);

            // CanHandle 阶段结果已缓存（60s TTL），此处直接复用探测数据
            var probe = Mp4Probe.Probe(path);
            if (probe != null && probe.Layout != AlphaLayout.None)
                _viewerPanel.SetDetectedSize(probe.Width, probe.Height);

            // vapc 元数据直送前端：精确矩形跳过像素检测，对位由官方坐标决定
            if (probe != null && probe.Layout == AlphaLayout.Vapc && probe.Vap != null)
                _viewerPanel.SetVapRects(probe.Vap.RgbRect, probe.Vap.AlphaRect, probe.Vap.OutW, probe.Vap.OutH);

            _viewerPanel.LoadFile(path);

            context.ViewerContent = _viewerPanel;
            context.Title = $"{Path.GetFileName(path)}";

            _viewerPanel.Dispatcher.Invoke(() => { context.IsBusy = false; },
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            if (_viewerPanel != null)
            {
                _lastWidth = _viewerPanel.ActualWidth > 0 ? _viewerPanel.ActualWidth : _lastWidth;
                _lastHeight = _viewerPanel.ActualHeight > 0 ? _viewerPanel.ActualHeight : _lastHeight;
            }

            _viewerPanel?.Dispose();
            _viewerPanel = null;
            GC.SuppressFinalize(this);
        }
    }
}
