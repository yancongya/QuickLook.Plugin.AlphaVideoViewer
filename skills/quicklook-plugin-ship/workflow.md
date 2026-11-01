# 工作流：核对 → 改造 → 分层验证 → 同步

1. 记录仓库/分支/dirty/sparse 与改动权限，先运行 scripts/audit_alignment.py。
2. 从主插件源码建立证据矩阵，选择 PAG / SVGA / Alpha / 原生 WPF 路线。
3. 修改用户授权的层；模板复制后改造，不重写无关逻辑或删除未追踪资产。
4. 根据 checklist.md 分别做静态、浏览器、Windows 宿主、CI/Release 与 Pages 验证。未具备环境的层写待验。
5. 复用经验先更新独立 Skill 源，保留旧基线；同步器 dry-run → 冲突检查 → --apply → 哈希比对。
6. 按 skilldo-cli 的本地来源/远端来源契约同步中心与工具链接，报告真实路径和内容哈希。
7. 用户授权发布时才 commit/push/tag/部署；之后检查目标远端 SHA、对应 workflow、资产内容及线上页面。没有线上验证就不称发布完成。

## 审计报告字段
对象 | 层 | 本地证据 | 远端证据/检查时间 | 差异 | 影响 | 已修复/待处理 | 验证层级

## 已知陷阱
- sparse-checkout 隐藏文件不是缺失；git show 读取少量 blob 比全量展开可靠。
- 历史 Release 错名资产可能仍存在，即使当前 CI 已修复；客户端必须按预期名称选择。
- README 的版本/CHANGELOG 高于 latest Release 时，先核对 tag，不能默认公开包含全部 HEAD 功能。
- Pages in-progress 冲突不能靠反复新建部署解决；查看失败日志和目标部署状态，再按授权处理。
- 网页实测不能证明 Windows Media Foundation、WPF、WebView2 宿主和官方安装/卸载行为。
