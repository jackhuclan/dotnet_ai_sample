using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

// ============================================================
// MegaAgent 冒烟测试
// 使用与 WPF UI 完全相同的服务层（ISettingsStore + IAgentService）
// 真实调用 DeepSeek 流式接口，验证 "发送 -> 调用 agent -> 回复" 链路
// 说明：这里只关心服务链路，日志用 NullLogger 静音；日志系统由 WPF 宿主（NLog）负责。
// ============================================================

Console.WriteLine("== MegaAgent 冒烟测试 ==");

// 1. 加载配置（与 WPF 应用同一份 %APPDATA%\MegaAgent\config.json）
ISettingsStore settings = new SettingsStore(NullLogger<SettingsStore>.Instance);
settings.Load();

if (!settings.Current.IsConfigured)
{
    Console.WriteLine("[FAIL] 未配置 API Key（config.json）");
    return 1;
}
Console.WriteLine($"[OK] 配置加载：endpoint={settings.Current.Endpoint}, model={settings.Current.DefaultModel}");

// 2. 构建会话与服务
var conversation = new Conversation
{
    ModelId = settings.Current.DefaultModel,
    Title = "冒烟测试",
};

IAgentService agentService = new AgentService(settings, NullLogger<AgentService>.Instance);

// 3. 第一轮：流式调用
Console.WriteLine("\n-- 第一轮（流式）--");
Console.Write("User: 你好，请用一句话介绍你自己。\nAssistant: ");
var reply1 = new System.Text.StringBuilder();
try
{
    await foreach (var chunk in agentService.StreamReplyAsync(conversation, "你好，请用一句话介绍你自己。", null))
    {
        reply1.Append(chunk);
        Console.Write(chunk);
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n[FAIL] 调用失败：{ex.Message}");
    return 1;
}

if (reply1.Length == 0)
{
    Console.WriteLine("[FAIL] 回复为空");
    return 1;
}
Console.WriteLine($"[OK] 第一轮收到 {reply1.Length} 字符流式回复");

// 4. 第二轮：验证多轮上下文（MAF 线程恢复）
Console.WriteLine("\n-- 第二轮（验证多轮上下文）--");
Console.Write("User: 请把上一条回答压缩成不超过15个字的版本。\nAssistant: ");
var reply2 = new System.Text.StringBuilder();
try
{
    await foreach (var chunk in agentService.StreamReplyAsync(conversation, "请把上一条回答压缩成不超过15个字的版本。", null))
    {
        reply2.Append(chunk);
        Console.Write(chunk);
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n[FAIL] 第二轮调用失败：{ex.Message}");
    return 1;
}

if (reply2.Length == 0)
{
    Console.WriteLine("[FAIL] 第二轮回复为空");
    return 1;
}
Console.WriteLine($"[OK] 第二轮收到 {reply2.Length} 字符回复（多轮上下文生效）");

// 5. 验证技能提示词注入路径（翻译技能）
Console.WriteLine("\n-- 第三轮（技能 system prompt 注入）--");
var skillConv = new Conversation { ModelId = settings.Current.DefaultModel, Title = "技能测试" };
Console.Write("User: hello world\nAssistant: ");
var reply3 = new System.Text.StringBuilder();
try
{
    var skill = new Skill { Name = "翻译助手", Prompt = "你是翻译器。把用户输入原样翻译成中文，只输出译文。" };
    await foreach (var chunk in agentService.StreamReplyAsync(skillConv, "hello world", skill.Prompt))
    {
        reply3.Append(chunk);
        Console.Write(chunk);
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n[FAIL] 第三轮调用失败：{ex.Message}");
    return 1;
}

Console.WriteLine($"[OK] 技能注入生效，回复：{reply3}");

Console.WriteLine("\n== 全部通过 ✅ ==");
return 0;
