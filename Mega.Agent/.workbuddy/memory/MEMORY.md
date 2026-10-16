# Mega.Agent 项目长期笔记

## 项目现状
- MegaAgent：WPF (.NET 10) 聊天应用。MAF（Microsoft.Agents.AI 1.25.0）ChatClientAgent + DeepSeek（OpenAI 兼容端点 https://api.deepseek.com/v1，可选模型 deepseek-flash / deepseek-v4-pro）。
- 结构：MegaAgent.Core（模型+服务+Abstractions 接口）/ MegaAgent.App（WPF+CommunityToolkit.Mvvm+DI 宿主）/ MegaAgent.SmokeTest（console 冒烟测试）/ MegaAgent.UiProbe（离屏 UI 探针，主力验证手段）/ MegaAgent.UiSmoke（pywinauto 脚本，已被 UiProbe 取代）。
- UI 库：HandyControl 3.6.0-rc3（用户指定；SkinDefault+Theme 合并进 App.xaml，原生控件被隐式重写，按钮用 ButtonPrimary/ButtonDefault/ButtonIcon 键）。Markdown 渲染：MdXaml 2.0.0-pre（MarkdownScrollViewer + MarkdownStyle.GithubLike，助手消息流式 200ms 节流）。
- **宿主与 DI（用户指定）**：`Host.CreateApplicationBuilder` 启动，`ServiceRegistrationExtensions.AddMegaAgentServices` 集中注册（App 与 UiProbe 共用），Core 服务全部按接口注册为单例、ViewModel/Window 注入。
- **日志（用户指定 NLog 6.2.2）**：`NLog` + `NLog.Extensions.Hosting` 6.2.2（主包不含 M.E.Logging 桥接），`src/MegaAgent.App/NLog.config`（CopyToOutputDirectory），输出 `<exe目录>\MegaAgent\logs\megaagent-<date>.log` 与 `megaagent-error-<date>.log`（config 里 logDir 必须 = LogPaths.Directory = `${basedir}/MegaAgent/logs`，NLog 相对路径不可靠：File target 相对 BaseDirectory、internalLogFile 相对 CWD）。接线在 `LoggingRegistrationExtensions.AddMegaAgentLogging`，退出前必须 `Shutdown()` 刷 AsyncWrapper。
- 交互：侧栏可折叠（450x800 手机视图）、头像 Popup 菜单（模型设置/技能管理）、输入框 `/` 斜杠选技能、模型下拉在输入区（MainViewModel.SelectedModel 代理：有会话→会话模型，无会话→默认模型且可写回）；冷启动无会话时 MainViewModel ctor 自动 CreateConversation 预置一个空会话（不立即落盘，首次使用才写 conversations.json）、Enter 发送 / Shift+Enter 换行、会话项「…」菜单（重命名/删除）、会话项副标题显示日期（ConversationViewModel.DisplayDate，非模型名）、空状态提示互斥（ShowEmptyChatHint）。
- 持久化：**应用目录下的 `MegaAgent\` 文件夹（与 exe 同级，用户 2026-10-16 自己从 %APPDATA% 改的，勿改回）**，含 config.json（含用户 DeepSeek Key，勿外泄）、conversations.json（含 MAF 会话序列化 ThreadJson）、skills.json。

## 关键技术约定
- MAF 1.25：AgentSession（非 AgentThread）；会话序列化用 SerializeSessionAsync/DeserializeSessionAsync；Instructions 走 ChatClientAgentOptions.ChatOptions.Instructions。
- 根目录 nuget.config `<clear/>` 屏蔽内网镜像（192.168.3.116:8081 会 401），只走 nuget.org。
- 长时间运行的 GUI 用 run_in_background 启动，`&` 会被外壳回收；构建前先 `taskkill //F //IM MegaAgent.exe`（否则 MSB3021 占用）。
- 验证优先级：UiProbe 离屏探针（每个用例自建 CreateHost + 备份/还原应用数据目录 JSON；现有 9 用例，含 EMPTY-STATE 冷启动用例）> pywinauto。合成鼠标点击（PostMessage）在新实例上已整体失效。
- 日志埋点约定：服务/VM 构造注入 `ILogger<T>`；API Key 一律脱敏；持久化 Load 对 JsonException/IOException 容错回落；落盘失败不让交互中断（记 Error 后吞掉）。


## 界面形态（用户定稿）
- 手机式布局：窗口 9:16（450x800），侧栏为抽屉式（默认隐藏，展开覆盖聊天区 50%，遮罩点击关闭），抽屉内仅「新建对话+历史对话」；头像在聊天区顶栏右上角，点击弹菜单（模型设置/技能选择）。
- 样式：HandyControl 3.6.0-rc3 + MdXaml 2.0.0-pre（助手消息 GithubLike 渲染）；技能编辑窗口不要 Markdown 预览。
- **坑**：HC 的 ButtonDefault/模板带固定 Height=30，会裁掉超高的 Content（头像圆、多行技能项都被裁）——多行/自定义内容要么 Height=Auto 放开，要么不用 Button 直接用 Border。
- ShadUI NuGet 包是 Avalonia 库，WPF 项目不能用。
- 用户在用电脑时不要跑 UI 自动化（抢占鼠标）；pywinauto send_keys 经中文 IME 会吞空格。
