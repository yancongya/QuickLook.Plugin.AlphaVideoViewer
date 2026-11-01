# WebView2 播放器参考模式

仅供 PAG/SVGA 等字节注入播放器参考，不适用于原生 WPF Manager，也不要求 Alpha 视频改成整文件 base64。入口、资源释放和 URL 映射以目标源码为准；以下是示例，不是可直接套用的完整实现。通用工作流见 ../SKILL.md。

### csproj 关键配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net462</TargetFramework>
    <RootNamespace>QuickLook.Plugin.{Format}</RootNamespace>
    <AssemblyName>QuickLook.Plugin.{Format}</AssemblyName>
    <Platforms>AnyCPU;x64</Platforms>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
    <DebugType>full</DebugType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <EnableDefaultPageItems>false</EnableDefaultPageItems>
    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
  </PropertyGroup>
  <!-- 配置、Content、Compile、PackageReference... -->
</Project>
```

### 命名空间迁移步骤

1. 全局替换 `QuickLook.Plugin.PagViewer` → `QuickLook.Plugin.{Format}`
2. 重命名 `.csproj` 和 `.sln` 文件
3. 重命名 XAML 的 `x:Class`
4. 更新 `AssemblyInfo.cs`
5. 更新 `QuickLook.Plugin.Metadata.config`
6. 改脚本与 CI 中的**插件名**（模板从 PagViewer 复制时全部是源插件名，漏改即产生错名 Release）：
   - `Scripts/build.ps1`：`$project`、`$pluginFile`
   - `Scripts/pack-zip.ps1`：输出 `.zip`/`.qlplugin` 资产名（3 处）
   - `Scripts/update-version.ps1`：`AssemblyTitle`/`AssemblyProduct`
   - `.github/workflows/build.yml`：`env.PLUGIN_NAME`（workflow 内其余命名均引用它，只改这一处）
   - `AGENTS.md`：项目描述与 `## CI / Release` 节中的资产名
7. **残留检查（不可跳过）**：

```powershell
Get-ChildItem -Recurse -File -Include *.ps1,*.yml,*.yaml,*.md,*.cs,*.xaml,*.csproj,*.config |
  Select-String -Pattern 'PagViewer'
# 期望 0 结果；skills/ 内嵌模板与 README Credits/参考链接中的历史提及除外
```

## 阶段 2：核心实现

### IViewer 最小实现

```csharp
public class Plugin : IViewer
{
    public int Priority => 0;

    public void Init() { }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path)) return false;
        if (!path.EndsWith(".ext", StringComparison.OrdinalIgnoreCase)) return false;
        // 验证文件头魔数字节
        try
        {
            using (var fs = File.OpenRead(path))
            {
                if (fs.Length < 4) return false;
                var header = new byte[3];
                fs.Read(header, 0, 3);
                return header[0] == 'X' && header[1] == 'Y' && header[2] == 'Z';
            }
        }
        catch { return false; }
    }

    public void Prepare(string path, ContextObject context)
    {
        context.SetPreferredSizeFit(new Size(600, 400), 0.9);
    }

    public void View(string path, ContextObject context)
    {
        var panel = new FormatPanel();
        panel.LoadFile(path);
        context.ViewerContent = panel;
        context.Title = Path.GetFileName(path);
        panel.Dispatcher.Invoke(() => context.IsBusy = false,
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);
    }
}
```

### WebView2 面板核心模式

```csharp
public partial class FormatPanel : UserControl
{
    private WebView2 _webView;
    private string _webAssetsDir;
    private string _filePath;
    private Window _hostWindow;

    // Win32 窗口拖拽
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public FormatPanel()
    {
        _webAssetsDir = Path.Combine(
            Path.GetDirectoryName(typeof(FormatPanel).Assembly.Location) ?? "",
            "Resources", "Web");
        if (!IsWebView2Available()) { Content = CreateDownloadButton(); return; }
        InitializeComponent();
        _webView = WebView;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var env = await CoreWebView2Environment.CreateAsync(null,
            Path.Combine(Path.GetTempPath(), "{Format}_WebView2"));
        await _webView.EnsureCoreWebView2Async(env);
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "assets.local", _webAssetsDir, CoreWebView2HostResourceAccessKind.Allow);
        _webView.CoreWebView2.Navigate(Path.Combine(_webAssetsDir, "player.html"));
        _hostWindow = Window.GetWindow(this);
    }

    // 关键：拦截外部导航
    private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (e.Uri.StartsWith("data:")) return;
        if (e.Uri.StartsWith("https://assets.local/")) return;
        if (e.Uri.StartsWith("file:")) return;
        e.Cancel = true;
    }

    // 关键：完整 Dispose
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
    }
}
```

### 文件加载模式（避免 file:// 安全错误）

```csharp
// C# 端：读取字节，转 base64，传给 WebView
private async Task SendFileToWebView()
{
    var bytes = File.ReadAllBytes(_filePath);
    var base64 = Convert.ToBase64String(bytes);
    var msg = JsonSerializer.Serialize(new
    {
        type = "loadFile",
        playerSrc = "https://assets.local/player.js",
        base64 = base64
    });
    await _webView.CoreWebView2.ExecuteScriptAsync(
        "window.postMessage(" + msg + ", '*');");
}
```

```javascript
// JS 端：接收 base64，转 ArrayBuffer
window.addEventListener('message', function(e) {
    var msg = e.data;
    if (msg.type === 'loadFile') {
        var raw = atob(msg.base64);
        var buf = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) buf[i] = raw.charCodeAt(i);
        var ab = buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength);
        // 用 ab 加载格式...
    }
});
```

### 主题传递

```csharp
private async Task ApplyTheme()
{
    var bg = _theme == Themes.Light ? "#ffffff" : "#202020";
    var fg = _theme == Themes.Light ? "#1a1a1a" : "#cccccc";
    await _webView.CoreWebView2.ExecuteScriptAsync($@"
        document.documentElement.style.setProperty('--bg', '{bg}');
        document.documentElement.style.setProperty('--fg', '{fg}');
    ");
}
```

### i18n 传递

```csharp
private string LoadTranslations()
{
    var doc = new XmlDocument();
    doc.Load(Path.Combine(exeDir, "Translations.config"));
    // 返回 JSON: {"KEY": {"zh": "...", "en": "..."}}
}

// 在 ApplyTheme 中传递
await _webView.CoreWebView2.ExecuteScriptAsync($@"
    window.__lang = '{lang}';
    window.__translations = {translations};
    if (typeof applyI18n === 'function') applyI18n();
");
```

## 阶段 3：本地验证

### 验证顺序（不可跳过）

1. **浏览器验证**：先在 Chrome 中打开 player.html，确认格式能正常渲染
2. **WebView2 验证**：安装 .qlplugin 后在 QuickLook 中测试
3. **样例回归**：用官方样例 + 自建样例 + 坏文件测试

### 必测场景

| 场景 | 预期行为 |
|------|---------|
| 正常文件 | 能预览，控制栏正常 |
| 空文件 (0 字节) | 显示错误提示，不崩溃 |
| 坏文件（随机字节） | 显示错误提示，不崩溃 |
| 超大文件 (>100MB) | 能加载，不 OOM |
| 文件名含中文/空格 | 正常处理 |
| 快速连续预览 | 不残留旧实例 |
| 关闭预览窗口 | WebView2 正确释放 |

### WebView2 典型错误排查

| 错误 | 原因 | 修复 |
|------|------|------|
| `file:` URLs are treated as unique security origins | 直接用 file:// 加载 | 改用 virtual host mapping |
| `Cannot set properties of undefined` | DOM 元素未找到 | 检查元素 ID 和脚本执行顺序 |
| 空白页面 | JS 报错或资源路径错 | F12 查看 Console |
| `Initialize data type error` | API 传参类型错 | 检查 SDK 文档要求的类型 |

## 阶段 4：优化体验

### UI 对齐官方风格

参考 VideoViewer / ImageViewer：
- 控制栏高度 32px
- Segoe Fluent Icons 字体
- 自动隐藏（鼠标移动显示，1.5s 后淡出）
- 进度条细线 + 悬停显示 Thumb
- DynamicResource 主题色

### 错误处理

```javascript
// 每个关键步骤都要有状态提示
statusEl.textContent = 'Loading SDK...';
try {
    // ...
    statusEl.textContent = 'Decoding...';
    // ...
} catch(err) {
    statusEl.textContent = 'Error: ' + err.message;
}
```

### 性能监控（可选）

```javascript
// 在 onAnimationUpdate 中获取 debugData
var debug = pagView.getDebugData();
// FPS 显示在左上角
// 进度条颜色反映渲染压力
```



## 实战经验（PagViewer + SvgaViewer + AlphaVideoViewer 总结）

### 覆盖内置预览插件的先例（AlphaVideoViewer）

```csharp
// 1. Priority 取 100，稳超官方内置全集（int.MinValue ~ 11，VideoViewer = -3）
public int Priority => 100;

// 2. CanHandle 做双向判定，不抢不属于自己的文件：
//    - 确认为普通文件（解码像素自检：两半都是彩色画面）→ return false，交回内置 VideoViewer
//    - 命中目标特征、或无法定论（解码器缺失/单色歧义/解码失败）→ return true 接管（宁漏勿抢）
// 3. CanHandle 的探测结果在 View 复用前缓存（60s TTL），避免重复解码
```

### 元数据优先，像素兜底（AlphaVideoViewer / 腾讯 VAP 先例）

若格式容器携带官方对位/布局元数据（自定义盒、sidecar 字段等），**优先解析元数据直送渲染器，
像素统计只做无元数据素材的兜底**——VAP 的 `vapc` 顶级盒里写着 `rgbFrame`/`aFrame` 精确坐标时，
任何"半区 + 象限"网格拟合都是放着标准答案不抄、自己在猜。

- 一次顶层扫描顺手抓元数据盒（它可能出现在 `moov` 之前，如 ftyp→vapc→free→moov→mdat），不要分两次开文件
- 元数据命中即定案，跳过像素自检（像素自检是给没有元数据的压平素材用的）
- 元数据坏了（坏 JSON / 缺字段 / 坐标越界）静默回退几何/像素路径，宁可多走兜底也不因脏数据误接管
- 归一化矩形连同输出尺寸一起下发前端，合成器只负责"两个任意源矩形"，对位决策留在探测层

### WebView2 关键配置

```csharp
// 必须禁用 WebView2 自带缩放
_webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

// 使用 virtual host 加载本地资源（避免 file:// 安全问题）
_webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "assets.local", _webAssetsDir, CoreWebView2HostResourceAccessKind.Allow);

// 拦截外部导航
_webView.CoreWebView2.NavigationStarting += (s, e) => {
    if (!e.Uri.StartsWith("https://assets.local/") && !e.Uri.StartsWith("file:"))
        e.Cancel = true;
};
```

### 文件加载最佳实践

```csharp
// C# 端：读取字节 → base64 → 传给 WebView
var bytes = File.ReadAllBytes(path);
var base64 = Convert.ToBase64String(bytes);
var msg = JsonSerializer.Serialize(new { type = "loadFile", base64 = base64 });
await _webView.CoreWebView2.ExecuteScriptAsync("window.postMessage(" + msg + ", '*');");
```

```javascript
// JS 端：接收 base64 → 转 ArrayBuffer/Blob
var raw = atob(base64);
var buf = new Uint8Array(raw.length);
for (var i = 0; i < raw.length; i++) buf[i] = raw.charCodeAt(i);
// 根据 SDK 要求选择传递方式：
// 1. ArrayBuffer: buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength)
// 2. Blob URL: URL.createObjectURL(new Blob([buf]))
// 3. Data URI: 'data:svga/1.0;base64,' + base64
```

### Canvas 缩放陷阱

```css
/* ❌ object-fit 对 canvas 不生效 */
#canvas { max-width: 100%; max-height: 100%; object-fit: contain; }

/* ✅ 需要手动计算缩放 */
function fitCanvasToContainer() {
    var scaleX = container.clientWidth / canvas.width;
    var scaleY = container.clientHeight / canvas.height;
    var scale = Math.min(scaleX, scaleY);
    canvas.style.width = Math.floor(canvas.width * scale) + 'px';
    canvas.style.height = Math.floor(canvas.height * scale) + 'px';
}
```

### SVGA 格式特殊处理

```javascript
// 1.x 和 2.x 属性名不同
var fps = videoItem.FPS || videoItem.fps || 20;           // 1.x: FPS, 2.x: fps
var size = videoItem.videoSize || videoItem.size || {};   // 1.x: videoSize, 2.x: size

// 1.x 需要 JSZip + JSZipUtils（两个都需要！）
<script src="jszip.min.js"></script>
<script src="jszip-utils.min.js"></script>  <!-- 必须！SVGAPlayer-Web 检测两个全局变量 -->
<script src="svga.min.js"></script>

// 1.x 使用 data URI 格式
var dataUri = 'data:svga/1.0;base64,' + base64;
parser.load(dataUri, success, failure);

// 2.x 图片是 Uint8Array，不是 base64 字符串
if (typeof imgData === 'string') {
    imgSize = imgData.length * 0.75;  // 1.x base64
} else if (imgData instanceof Uint8Array) {
    imgSize = imgData.length;          // 2.x binary
    base64Data = btoa(String.fromCharCode.apply(null, imgData));
}
```

### 元数据面板最佳实践

```javascript
// 点击固定 + 高亮状态（使用 pinned 类，不要用 active）
var metaPinned = false;
btnMeta.addEventListener('click', function(e) {
    e.stopPropagation();
    metaPinned = !metaPinned;
    btnMeta.classList.toggle('pinned', metaPinned);
    // ...
});

// 自定义滚动条
#metaPanel::-webkit-scrollbar { width: 4px; }
#metaPanel::-webkit-scrollbar-track { background: transparent; }
#metaPanel::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.2); border-radius: 2px; }
```

### 双语支持

```xml
<!-- Metadata.config: 单字段双语 -->
<Description>Preview PAG files. 预览 PAG 动画文件。</Description>

<!-- Translations.config: 中英分开 -->
<Entry Key="BTN_Play" Value="播放" ValueEn="Play" />
```

### 常见错误清单

| 错误 | 原因 | 修复 |
|------|------|------|
| `file:` URLs are treated as unique security origins | 直接用 file:// 加载 | 改用 virtual host mapping |
| `Cannot set properties of undefined` | DOM 元素未找到 | 检查元素 ID 和脚本执行顺序 |
| `incorrect header check` | zlib 解压失败 | 检查文件格式，使用正确的 data URI 前缀 |
| Canvas 空白 | object-fit 不生效 | 手动计算 style.width/height |
| 图片不显示 | 1.x/2.x 图片格式不同 | 检查是 string 还是 Uint8Array |
| WebView2 缩放干扰 | Ctrl+滚轮触发 WebView2 缩放 | 设置 IsZoomControlEnabled = false |
- ContextObject：https://github.com/QL-Win/QuickLook.Common/blob/master/Plugin/ContextObject.cs
