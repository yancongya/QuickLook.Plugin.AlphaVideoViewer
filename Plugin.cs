using System;
using System.IO;
using System.Windows;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.AlphaVideoViewer.Probing;

namespace QuickLook.Plugin.AlphaVideoViewer
{
    /// <summary>
    /// 接管「Alpha/RGB 左右或上下拼接」的透明视频（MP4/MOV），透明合成播放。
    /// 只接管探测确认为拼接布局的文件；普通 MP4 一律返回 false，交回内置 VideoViewer（宁漏勿抢）。
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
            return probe != null && probe.Layout != AlphaLayout.None;
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
                _viewerPanel.SetDetectedLayout(probe.Layout, probe.Width, probe.Height);

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
