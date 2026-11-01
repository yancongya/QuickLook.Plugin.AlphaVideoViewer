---
name: quicklook-plugin-ship
description: 开发、维护、核对与交付 QuickLook 插件；用于 PAG、SVGA、AlphaVideo 和原生 WPF 插件管理器的主程序、落地页、文档、打包流水线与 Skills 同步对齐。触发于复制插件模板、制作同系列落地页、打包 qlplugin、检查版本与发布差异、同步插件开发规范。不把网页演示验收当成 Windows 宿主验收。
version: 2.0.0
---

# QuickLook 插件全链路对齐

覆盖四条实现路线，不强制统一渲染技术、品牌色、面板或功能。先核对证据，再改所需层；保留既有未提交文件。

## 1. 从事实开始

1. 明确本次范围：插件代码、网页、文档、CI、Skill 源/副本/工具链接，分别标记可修改与只读对象。
2. 记录 Git 分支、HEAD、远端、dirty 状态和 sparse-checkout。磁盘缺文件先用 git ls-tree / git show 判断是否仅未展开；不因此全量拉取 Examples、Samples、官方主程序或大素材。
3. 读 Plugin.cs、csproj、README、CHANGELOG、打包脚本、工作流、页面实际引用入口，以及本次涉及的最小服务代码。
4. 建立“主插件 → README → 落地页宣称/演示 → Release → Pages”的证据矩阵。HEAD、公开 Release 与在线页面是三个不同版本。
5. 用 scripts/audit_alignment.py 生成可复查报告；静态检查只作为线索。远端结论另查 GitHub API，记录 SHA、run ID、资产清单、Pages 配置及检查时间。

## 2. 选择正确路线

| 插件 | 实现 / 输入 | 核对重点 |
|---|---|---|
| PagViewer | WebView2 + libpag WASM；.pag；当前 Priority 0 | PAG 文件头、SDK 资源、缩放/音量/PNG 等真实控件 |
| SvgaViewer | WebView2 + SVGAPlayer；.svga 1.x/2.x；当前 Priority 10 | 1.x ZIP 与 2.x zlib，JSZip/JSZipUtils；不可把历史错名资产当正确包 |
| AlphaVideoViewer | WebView2 + 自写 WebGL；.mp4/.mov/.m4v；当前 Priority 100 | vapc 优先，像素检测兜底；不承诺全部 YYEVA 动态替换能力 |
| PluginManager | 原生 WPF，无 WebView2；QuickLook 数据根目录；当前 Priority 100 | 明确排除 .qlplugin；官方安装器交接；受保护插件；延迟删除 |

这些值是已核对的当前实现，不是未来所有插件的默认值。路由/限制与证据锚点见 references/product-contracts.md。仅 WebView2 路线需要 references/webview2-patterns.md；Manager 不需要浏览器播放器、音量、PNG 导出或自动隐藏播放栏。

## 3. 主插件和文档对齐

- 依实际 CanHandle 路由说明输入；Manager 接受数据目录路径，不能要求所有插件拒绝目录或读取魔数。
- 视频可采用安全的文件映射/流式加载；不把整文件 base64 注入定为所有插件要求。
- 区分“配置/日志预览只读”和“显式清空异常日志”：Manager 的清空动作会写文件，需单独确认，不得声称所有日志操作均不修改文件。
- Alpha 的不确定检测可能沿用几何初筛；不能承诺任何普通视频都绝不接管。相关项目链接不等于完整格式兼容承诺。
- README 中英对等，包含安装与重启、真实依赖、操作、构建入口、可用落地页或标明本地待发布。CHANGELOG 已编号版本必须与 tag/Release 对照，未发布内容不可冒充已发版。
- 页面是示意图时明确标注；不得伪造设备状态、下载进度实测或把演示动作描述成本地文件操作。

## 4. 同系列落地页

用户要求复制时，实际复制既有 HTML/CSS/JS 框架再改造；保留结构和交互基础，重写产品专属文案、Logo、favicon、文件包装、插图与动态内容。具体规则见 references/landing-contract.md。

- 先列页面模块和产品证据，再按真实痛点/原理组织内容；不机械复制 PAG/SVGA 的论述。
- Logo 是独立标识；favicon、下载包装和页面引用保持同源。用户只要改颜色/Logo 时，保持包装透视、裁切、留白和比例。
- Space 的“选中 → 弹窗 → 关闭”骨架与内容分离；重做播放内容不应擅改已有交互。
- 桌面/中屏/窄屏、中文/英文、reduced-motion、手动接管、离屏暂停均需真实浏览器测试。
- 不用固定宽高把 mockup 拉伸；查初始、运动中间、终态、关闭以及重复触发后的布局。
- canonical/社交元数据/结构化信息和索引文件按实际发布地址设置；未部署页不能伪装成已上线。系列导航只链接已验证的网页，待发布项可链接仓库。

## 5. 构建、Release 与 Pages 分开验收

1. 命名源为 Scripts/build.ps1 的 project/pluginFile；CI 调用 -SkipBuild -SkipVersion，不另写打包实现。
2. .ps1 保持 UTF-8 BOM / PowerShell 5.1 兼容。无 tag 时 update-version 不能将 commit hash 写成版本。
3. 校验包里的主 DLL、Metadata、依赖与资源；安装包不是源码 ZIP。若发布名混入其他插件，先报告，不自动删除历史资产。
4. 下载选精确程序集名 .qlplugin；不能取列表第一项或只按扩展名任取。第三方不标准命名需检查元数据/明确用户选择，不能盲目套系列命名。
5. master push / PR 只构建，v* tag 才发布；记录实际工作流条件。CI 绿灯不是 Windows 上已安装可用。
6. Pages 先读 build_type / source，保留 legacy master:/docs 或既有 workflow，不为“统一”迁移模式。插件构建成功不代表 Pages 成功。
7. 部署失败先读失败 job 日志；in-progress deployment 冲突与页面构建错误分开处理。只有当前任务明确授权部署时才取消/重试部署，且最终检查在线页面与提交对应关系。
8. 提交、push、tag、公开发布和官方 Issue 分别遵循用户授权；研究/同步 Skill 不自动授权发布插件或页面。

## 6. Skill 源和副本同步

规范源是独立 quicklook-plugin-ship 仓库。四个插件的 skills/quicklook-plugin-ship 是可随仓库携带的副本，不能各改各的。

- 改源前保存基线；同步用 scripts/sync_replicas.py，默认 dry-run。只覆盖仍等于基线的文件；目标本地改动/额外文件报冲突，不强行清理。
- 部分拉取的仓库只增加 skills/quicklook-plugin-ship，保留原 sparse 列表；不展开样例和官方主程序。
- 更新来源标记和内容哈希；未提交状态写明 working-tree digest，不能用旧 commit 冒充更新来源。
- 通过 skilldo-cli 管理中心和已有工具链接。需要使用本地未发布修改时，明确记录 track-local 来源，随后 update 和 sync；不要把全局工具目录当独立源码仓库。
- 检查中心文件哈希、目标真实软链接及 SkillDo 注册状态。不得以数据库 status=ok 代替磁盘检查。
- 项目仓库内副本保持真实文件，不替换为指向本机的软链接。同步报告区分本地副本、SkillDo 中心/链接、Git 远端；未 push 就不说远端已同步。

## 7. 完成定义

按 checklist.md 分层报告：源码/静态、浏览器、Windows QuickLook 宿主、CI/Release、在线 Pages、Skill 分发。不能验证的层明确待验；允许带着与本次修改无关的公开问题交付审计，但不能声称全链路已通过。

工作流见 workflow.md；回归情景见 references/scenarios.md。不在 Skill 中写入凭据、机器绝对路径或某次运行的临时状态。
