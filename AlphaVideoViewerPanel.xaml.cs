using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.AlphaVideoViewer.Probing;

namespace QuickLook.Plugin.AlphaVideoViewer
{
    public partial class AlphaVideoViewerPanel : UserControl
    {
        private string _videoFilePath;
        private readonly string _webAssetsDir;
        private WebView2 _webView;
        private Themes _theme = Themes.Dark;
        private Window _hostWindow;

        private double _rawWidth;
        private double _rawHeight;

        // vapc 元数据（腾讯 VAP v2 官方对位坐标，已归一化）；非 null 时前端跳过像素检测直接合成
        private double[] _vapRgbRect;
        private double[] _vapAlphaRect;
        private double _vapOutW;
        private double _vapOutH;
        private bool _isVapx;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public AlphaVideoViewerPanel()
        {
            _webAssetsDir = Path.Combine(
                Path.GetDirectoryName(typeof(AlphaVideoViewerPanel).Assembly.Location) ?? "",
                "Resources", "Web");

            if (!IsWebView2Available())
            {
                Content = CreateDownloadButton();
                return;
            }

            InitializeComponent();
            var isZh = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh";
            TxtStatus.Text = isZh ? "加载中..." : "Loading...";
            _webView = WebView;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public void LoadFile(string path)
        {
            _videoFilePath = path;
        }

        public void SetTheme(Themes theme)
        {
            _theme = theme;
        }

        public void SetDetectedSize(double width, double height)
        {
            // 拼接方案（半幅 / 四分幅、Alpha 在哪一侧）由前端按解码帧像素自检决定，
            // 这里只透传探测到的帧尺寸供信息面板展示。
            _rawWidth = width;
            _rawHeight = height;
        }

        public void SetVapRects(double[] rgbRect, double[] alphaRect, double outW, double outH, bool isVapx = false)
        {
            _vapRgbRect = rgbRect;
            _vapAlphaRect = alphaRect;
            _vapOutW = outW;
            _vapOutH = outH;
            _isVapx = isVapx;
        }

        private static bool IsWebView2Available()
        {
            try
            {
                return !string.IsNullOrEmpty(
                    CoreWebView2Environment.GetAvailableBrowserVersionString());
            }
            catch
            {
                return false;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var htmlPath = Path.Combine(_webAssetsDir, "alpha-player.html");
                if (!File.Exists(htmlPath))
                {
                    ShowError("alpha-player.html not found.");
                    return;
                }

                var env = await CoreWebView2Environment.CreateAsync(
                    null,
                    Path.Combine(Path.GetTempPath(), "AlphaVideoViewer_WebView2"));

                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.AddWebResourceRequestedFilter(
                    "https://alphavideo.local/*",
                    CoreWebView2WebResourceContext.All);
                _webView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "alphaassets.local",
                    _webAssetsDir,
                    CoreWebView2HostResourceAccessKind.Allow);

                // 走虚拟域名而不是 file:///：ES module import 在 file:// 下会被 CORS 拦掉
                _webView.CoreWebView2.Navigate("https://alphaassets.local/alpha-player.html");

                _hostWindow = Window.GetWindow(this);
                if (_hostWindow != null)
                    _hostWindow.DpiChanged += OnDpiChanged;
            }
            catch (Exception ex)
            {
                ShowError($"WebView2 init failed: {ex.Message}");
            }
        }

        private void OnWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (string.IsNullOrEmpty(_videoFilePath) || !File.Exists(_videoFilePath))
            {
                e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
                return;
            }

            try
            {
                var fi = new FileInfo(_videoFilePath);
                var total = fi.Length;
                var ext = Path.GetExtension(_videoFilePath).ToLowerInvariant();
                var mime = ext == ".mov" ? "video/quicktime"
                         : ext == ".webm" ? "video/webm"
                         : "video/mp4";

                string rangeHeader = null;
                foreach (var h in e.Request.Headers)
                {
                    if (string.Equals(h.Key, "Range", StringComparison.OrdinalIgnoreCase))
                    {
                        rangeHeader = h.Value;
                        break;
                    }
                }

                long start = 0;
                long end = total > 0 ? total - 1 : 0;
                bool isRange = false;

                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
                {
                    var spec = rangeHeader.Substring(6).Split(',')[0].Trim();
                    var dash = spec.IndexOf('-');
                    if (dash >= 0)
                    {
                        var sPart = spec.Substring(0, dash).Trim();
                        var ePart = spec.Substring(dash + 1).Trim();
                        if (!string.IsNullOrEmpty(sPart) && long.TryParse(sPart, out var parsedStart))
                        {
                            start = Math.Max(0, parsedStart);
                            end = (!string.IsNullOrEmpty(ePart) && long.TryParse(ePart, out var parsedEnd))
                                ? Math.Min(total - 1, parsedEnd)
                                : total - 1;
                            isRange = true;
                        }
                        else if (!string.IsNullOrEmpty(ePart) && long.TryParse(ePart, out var suffixLen))
                        {
                            start = Math.Max(0, total - suffixLen);
                            end = total - 1;
                            isRange = true;
                        }
                    }
                }

                if (total <= 0 || start >= total || end < start)
                {
                    e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 416, "Range Not Satisfiable", $"Content-Range: bytes */{total}");
                    return;
                }

                var sliceLen = end - start + 1;
                var fs = new FileStream(_videoFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var stream = new RangeStream(fs, start, sliceLen);

                if (isRange)
                {
                    var headers = $"Content-Type: {mime}\r\nAccept-Ranges: bytes\r\nContent-Range: bytes {start}-{end}/{total}\r\nContent-Length: {sliceLen}\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Expose-Headers: Content-Range, Content-Length, Accept-Ranges";
                    e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(stream, 206, "Partial Content", headers);
                }
                else
                {
                    var headers = $"Content-Type: {mime}\r\nAccept-Ranges: bytes\r\nContent-Length: {total}\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Expose-Headers: Content-Range, Content-Length, Accept-Ranges";
                    e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(stream, 200, "OK", headers);
                }
            }
            catch
            {
                e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Internal Server Error", "");
            }
        }

        private sealed class RangeStream : Stream
        {
            private readonly FileStream _inner;
            private readonly long _start;
            private readonly long _length;
            private long _pos;

            public RangeStream(FileStream inner, long start, long length)
            {
                _inner = inner;
                _start = start;
                _length = length;
                _inner.Seek(_start, SeekOrigin.Begin);
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position
            {
                get => _pos;
                set => Seek(value, SeekOrigin.Begin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remaining = _length - _pos;
                if (remaining <= 0) return 0;
                var toRead = (int)Math.Min(count, remaining);
                var read = _inner.Read(buffer, offset, toRead);
                _pos += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long next;
                if (origin == SeekOrigin.Begin) next = offset;
                else if (origin == SeekOrigin.Current) next = _pos + offset;
                else next = _length + offset;
                next = Math.Max(0, Math.Min(_length, next));
                _pos = next;
                _inner.Seek(_start + _pos, SeekOrigin.Begin);
                return _pos;
            }

            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) _inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("data:")) return;
            if (e.Uri.StartsWith("blob:")) return;
            if (e.Uri.StartsWith("https://alphaassets.local/")) return;
            if (e.Uri.StartsWith("https://alphavideo.local/")) return;
            if (e.Uri.StartsWith("file:")) return;
            e.Cancel = true;
        }

        private static readonly JavaScriptSerializer JsonSer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                var msg = JsonSer.Deserialize<Dictionary<string, object>>(json);

                if (msg == null || !msg.TryGetValue("type", out var typeObj) || typeObj == null) return;
                var type = typeObj.ToString();

                if (type == "ready")
                {
                    await ApplyTheme();
                    await SendFileToWebView();
                }
                else if (type == "loaded")
                {
                    Dispatcher.Invoke(() => LoadingPanel.Visibility = Visibility.Collapsed);
                }
                else if (type == "error")
                {
                    var message = msg.TryGetValue("message", out var m) && m != null ? m.ToString() : "Unknown error";
                    Dispatcher.Invoke(() => ShowError($"Alpha Video Error: {message}"));
                }
                else if (type == "startDrag")
                {
                    Dispatcher.Invoke(() => StartWindowDrag());
                }
                else if (type == "copyImage")
                {
                    var raw = msg.TryGetValue("dataUrl", out var du) ? du?.ToString()
                            : (msg.TryGetValue("data", out var d) ? d?.ToString() : null);
                    Dispatcher.Invoke(() => CopyImageToClipboard(StripDataUrlPrefix(raw)));
                }
                else if (type == "saveImage")
                {
                    var raw = msg.TryGetValue("dataUrl", out var du) ? du?.ToString()
                            : (msg.TryGetValue("data", out var d) ? d?.ToString() : null);
                    Dispatcher.Invoke(() => SaveImage(StripDataUrlPrefix(raw)));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"WebMessage error: {ex.Message}"));
            }
        }

        private static string StripDataUrlPrefix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var idx = s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? s.Substring(idx + 7) : s;
        }

        private void CopyImageToClipboard(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var dir = Path.GetDirectoryName(_videoFilePath) ?? "";
                var baseName = Path.GetFileNameWithoutExtension(_videoFilePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var savePath = Path.Combine(dir, $"{baseName}_{timestamp}.png");
                File.WriteAllBytes(savePath, bytes);
                Dispatcher.Invoke(() =>
                {
                    var fileList = new System.Collections.Specialized.StringCollection { savePath };
                    Clipboard.SetFileDropList(fileList);
                });
            }
            catch { }
        }

        private void SaveImage(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var dir = Path.GetDirectoryName(_videoFilePath) ?? "";
                var baseName = Path.GetFileNameWithoutExtension(_videoFilePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var savePath = Path.Combine(dir, $"{baseName}_{timestamp}.png");
                File.WriteAllBytes(savePath, bytes);
            }
            catch { }
        }

        private void StartWindowDrag()
        {
            if (_hostWindow == null) return;
            var helper = new System.Windows.Interop.WindowInteropHelper(_hostWindow);
            ReleaseCapture();
            SendMessage(helper.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private async Task ApplyTheme()
        {
            string bg, fg, fgAlt;
            if (_theme == Themes.Light)
            {
                bg = "#ffffff"; fg = "#1a1a1a"; fgAlt = "#666666";
            }
            else
            {
                bg = "#202020"; fg = "#cccccc"; fgAlt = "#888888";
            }

            var translations = LoadTranslations();
            var isEn = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "zh";
            var lang = isEn ? "en" : "zh";

            var script = $@"
                document.documentElement.style.setProperty('--bg', '{bg}');
                document.documentElement.style.setProperty('--fg', '{fg}');
                document.documentElement.style.setProperty('--fg-alt', '{fgAlt}');
                window.__lang = '{lang}';
                window.__translations = {translations};
                if (typeof applyI18n === 'function') applyI18n();
            ";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private string LoadTranslations()
        {
            try
            {
                var exeDir = Path.GetDirectoryName(typeof(AlphaVideoViewerPanel).Assembly.Location) ?? "";
                var configPath = Path.Combine(exeDir, "Translations.config");
                if (!File.Exists(configPath)) return "{}";

                var doc = new System.Xml.XmlDocument();
                doc.Load(configPath);
                var dict = new System.Collections.Generic.Dictionary<string, object>();
                var nodes = doc.SelectNodes("//Entry");
                if (nodes == null) return "{}";
                foreach (System.Xml.XmlNode entry in nodes)
                {
                    var key = entry.Attributes?["Key"]?.Value;
                    if (key == null) continue;
                    dict[key] = new
                    {
                        zh = entry.Attributes["Value"]?.Value ?? "",
                        en = entry.Attributes["ValueEn"]?.Value ?? ""
                    };
                }
                return JsonSer.Serialize(dict);
            }
            catch { return "{}"; }
        }

        private async Task SendFileToWebView()
        {
            if (string.IsNullOrEmpty(_videoFilePath) || !File.Exists(_videoFilePath))
            {
                ShowError("Video file not found.");
                return;
            }

            try
            {
                var fi = new FileInfo(_videoFilePath);
                var videoUrl = "https://alphavideo.local/__video__?t=" + fi.LastWriteTimeUtc.Ticks;

                var msg = new
                {
                    type = "loadFile",
                    rawWidth = _rawWidth,
                    rawHeight = _rawHeight,
                    fileSize = fi.Length,
                    isVapx = _isVapx,
                    // vapc 元数据（归一化矩形 + 输出尺寸）；无 vapc 时为 null，前端走像素自检
                    vapc = _vapRgbRect != null && _vapAlphaRect != null
                        ? (object)new
                        {
                            rgb = _vapRgbRect,
                            alpha = _vapAlphaRect,
                            outW = _vapOutW,
                            outH = _vapOutH
                        }
                        : null,
                    videoUrl = videoUrl
                };

                var jsonMsg = JsonSer.Serialize(msg);
                await _webView.CoreWebView2.ExecuteScriptAsync(
                    "window.postMessage(" + jsonMsg + ", '*');");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load file: {ex.Message}");
            }
        }

        private void OnDpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (_webView?.CoreWebView2 != null)
            {
                _webView.InvalidateVisual();
                _webView.UpdateLayout();
            }
        }

        private void ShowError(string message)
        {
            LoadingSpinner.Visibility = Visibility.Collapsed;
            TxtStatus.Text = message;
            TxtStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
            TxtStatus.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Visible;
        }

        private static Button CreateDownloadButton()
        {
            var button = new Button
            {
                Content = "WebView2 Runtime is required. Click to download.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20, 6, 20, 6)
            };
            button.Click += (s, e) =>
                Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            return button;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_hostWindow != null)
                _hostWindow.DpiChanged -= OnDpiChanged;

            Dispose();
        }

        public void Dispose()
        {
            if (_webView != null)
            {
                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                    _webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
                }
                _webView.Dispose();
                _webView = null;
            }
            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
        }
    }
}
