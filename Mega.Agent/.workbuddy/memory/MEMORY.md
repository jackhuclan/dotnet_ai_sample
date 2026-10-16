# Mega.Agent 项目长期笔记

## 项目现状
- MegaAgent：WPF (.NET 10) 聊天应用，仿 WorkBuddy。MAF（Microsoft.Agents.AI 1.25.0）ChatClientAgent + DeepSeek（OpenAI 兼容端点 https://api.deepseek.com/v1，模型 deepseek-chat / deepseek-reasoner）。
- 结构：MegaAgent.Core（模型+服务）/ MegaAgent.App（WPF+CommunityToolkit.Mvvm）/ MegaAgent.SmokeTest（console 冒烟测试）。
- 持久化：`%APPDATA%\MegaAgent\` 下 config.json（含用户 DeepSeek Key，勿外泄）、conversations.json（含 MAF 会话序列化 ThreadJson）、skills.json（技能=系统提示词预设）。

## 关键技术约定
- MAF 1.25：AgentSession（非 AgentThread）；会话序列化用 SerializeSessionAsync/DeserializeSessionAsync；Instructions 走 ChatClientAgentOptions.ChatOptions.Instructions。
- 根目录 nuget.config `<clear/>` 屏蔽内网镜像（192.168.3.116:8081 会 401），只走 nuget.org。
- 长时间运行的 GUI 用 run_in_background 启动，`&` 会被外壳回收。
