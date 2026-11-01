# 产品边界与证据

以下用于定位证据，更新时重新读目标源码，不能将其当永不变化的功能清单。

## PAG / SVGA
- Plugin.cs / 项目面板 / Resources/Web：格式路由、播放器桥接、清理。
- PAG 使用 libpag；SVGA 1.x/2.x 需分别确认解码依赖。
- 功能、快捷键、主题来自播放器实现；网页 Space 是 QuickLook 开窗流程，不能冒充播放器内 Space 快捷键。

## AlphaVideoViewer
- Plugin.cs：MP4/MOV/M4V、Priority、vapc 快速路径、FrameProbe.NotAlpha 回退和不确定时保留初筛。
- Probing/Mp4Probe.cs、FrameProbe.cs：元数据与采样识别。
- Resources/Web/alpha-compositor.js：两个源矩形、Alpha 与 RGB 预乘合成，不等于两个独立文件。
- 原理插图可用两张图解释通道，需标明实际通常打包在同一视频。不可把一切黑底视频或所有 YYEVA 动态素材承诺为受支持。

## PluginManager
- Plugin.cs + DataRootResolver：QuickLook 数据根目录路径；.qlplugin 明确交给官方安装器。
- InstalledPluginService：用户/内置区分、保护自身与内置、{GUID}.to_be_deleted 延迟删除。
- GitHubReleaseService + SafeArchive：资产选择、临时下载、归档路径/Metadata 校验及 SHA-256（若提供）。不自行解压安装。
- ConfigLogService.ReadText 是只读预览；TryClearLog 会截断异常日志，PluginManagerPanel.ClearLogAsync 先确认。
- RepairService 的重启、清理、解除锁定是真实有影响的动作；网页只模拟，不表示已操作电脑。
- .NET Framework/WPF 依赖以 csproj 为证，不加 WebView2 安装要求。
