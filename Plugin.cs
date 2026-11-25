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

            if (HasForceMarker(path)) return true;

            var probe = Mp4Probe.Probe(path);
            if (probe == null || probe.Layout == AlphaLayout.None) return false;

            // vapc 是官方写进文件的权威对位信息，命中即定案，无需像素自检
            if (probe.Layout == AlphaLayout.Vapc) return true;

            // 严格模式：仅当解码像素自检明确确认「近灰度遮罩 + 有色内容」特征（AlphaLike）时接管；
            // 普通视频（NotAlpha）或无法定论的素材（Unknown，如特殊编码 .mov / 纯暗灰画面）交回原生 VideoViewer。
            var check = FrameProbe.Check(path, probe.Width, probe.Height, probe.DurationSeconds);
            return check == SplitCheck.AlphaLike;
        }

        private static bool HasForceMarker(string path)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(stem)) return false;
            return stem.EndsWith("_alpha", StringComparison.OrdinalIgnoreCase) ||
                   stem.EndsWith(".alpha", StringComparison.OrdinalIgnoreCase) ||
                   stem.EndsWith("-alpha", StringComparison.OrdinalIgnoreCase) ||
                   stem.EndsWith("_vap", StringComparison.OrdinalIgnoreCase) ||
                   stem.EndsWith(".vap", StringComparison.OrdinalIgnoreCase) ||
                   stem.EndsWith("-vap", StringComparison.OrdinalIgnoreCase);
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
                _viewerPanel.SetVapRects(probe.Vap.RgbRect, probe.Vap.AlphaRect, probe.Vap.OutW, probe.Vap.OutH, probe.Vap.IsVapx);

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
