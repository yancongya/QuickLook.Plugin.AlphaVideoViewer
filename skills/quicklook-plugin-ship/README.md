# quicklook-plugin-ship

QuickLook 四类插件的开发、落地页、文档、打包发布和规范同步工作流。支持 PAG、SVGA、AlphaVideo 和原生 WPF PluginManager。

## 文件
- SKILL.md：入口与分层验收规则。
- references/product-contracts.md：产品差异及源码锚点。
- references/landing-contract.md：复制改造、资产与动态交互契约。
- references/webview2-patterns.md：按需读取的播放器模式；不适用于所有产品。
- workflow.md / checklist.md：执行与验收。
- scripts/audit_alignment.py：只读跨仓审计，支持 sparse 工作树。
- scripts/sync_replicas.py：默认 dry-run、基线冲突保护的副本同步。
- references/scenarios.md：回归情景。

## 使用和同步

优先通过 SkillDo 安装/同步，不手工复制到各工具目录。远端版本使用 skilldo install --url 指向本仓库；本地未发布修改使用 skilldo track-local 指向本 Skill 源，再 update、sync。中心位于 SkillDo 管理目录，工具用真实软链接，插件仓库内的携带副本保留普通文件。

本地审计和 Skill 同步不会自动发布插件、页面或官方 Issue。详见 SKILL.md。

License: MIT
