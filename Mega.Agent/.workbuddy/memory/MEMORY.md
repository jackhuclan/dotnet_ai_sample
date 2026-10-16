# Mega.Agent 项目长期笔记

## 项目现状
- MegaAgent：WPF (.NET 10) 聊天应用。MAF（Microsoft.Agents.AI 1.25.0）ChatClientAgent + DeepSeek（OpenAI 兼容端点 https://api.deepseek.com/v1，模型 deepseek-chat / deepseek-reasoner）。
- 结构：MegaAgent.Core（模型+服务）/ MegaAgent.App（WPF+CommunityToolkit.Mvvm）/ MegaAgent.SmokeTest（console 冒烟测试）/ MegaAgent.UiSmoke（pywinauto UI 验证脚本）。
- UI 库：HandyControl 3.6.0-rc3（用户指定；SkinDefault+Theme 合并进 App.xaml，原生控件被隐式重写，按钮用 ButtonPrimary/ButtonDefault/ButtonIcon 键）。Markdown 渲染：MdXaml 2.0.0-pre（MarkdownScrollViewer + MarkdownStyle.GithubLike，助手消息流式 200ms 节流）。
- 交互：侧栏可折叠（420x820 手机视图）、头像 Popup 菜单（模型设置/技能选择）、输入框 `/` 斜杠选技能、模型下拉在输入区。
- 持久化：`%APPDATA%\MegaAgent\` 下 config.json（含用户 DeepSeek Key，勿外泄）、conversations.json（含 MAF 会话序列化 ThreadJson）、skills.json（技能=系统提示词预设）。

## 关键技术约定
- MAF 1.25：AgentSession（非 AgentThread）；会话序列化用 SerializeSessionAsync/DeserializeSessionAsync；Instructions 走 ChatClientAgentOptions.ChatOptions.Instructions。
- 根目录 nuget.config `<clear/>` 屏蔽内网镜像（192.168.3.116:8081 会 401），只走 nuget.org。
- 长时间运行的 GUI 用 run_in_background 启动，`&` 会被外壳回收。

## 界面形态（用户定稿）
- 手机式布局：窗口 9:16（450x800），侧栏为抽屉式（默认隐藏，展开覆盖聊天区 50%，遮罩点击关闭），抽屉内仅「新建对话+历史对话」；头像在聊天区顶栏右上角，点击弹菜单（模型设置/技能选择）。
- 样式：HandyControl 3.6.0-rc3 + MdXaml 2.0.0-pre（助手消息 GithubLike 渲染）；技能编辑窗口不要 Markdown 预览。
- **坑**：HC 的 ButtonDefault/模板带固定 Height=30，会裁掉超高的 Content（头像圆、多行技能项都被裁）——多行/自定义内容要么 Height=Auto 放开，要么不用 Button 直接用 Border。
- ShadUI NuGet 包是 Avalonia 库，WPF 项目不能用。
- 用户在用电脑时不要跑 UI 自动化（抢占鼠标）；pywinauto send_keys 经中文 IME 会吞空格。
