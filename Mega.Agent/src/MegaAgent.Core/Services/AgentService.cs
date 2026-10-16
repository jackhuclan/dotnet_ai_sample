using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using MegaAgent.Core.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MegaAgent.Core.Services;

/// <summary>
/// 基于 Microsoft Agent Framework（MAF）的对话服务。
/// 底层使用 OpenAI SDK 指向 DeepSeek 的 OpenAI 兼容端点，
/// 通过 MAF 的 ChatClientAgent 获得会话（AgentSession）管理与后续扩展能力。
/// </summary>
public class AgentService
{
    private readonly SettingsStore _settings;

    public AgentService(SettingsStore settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// 发送一条用户消息并流式接收回复。
    /// 多轮上下文由 MAF AgentSession 维护（序列化保存在 Conversation.ThreadJson）。
    /// </summary>
    /// <param name="conversation">目标会话（携带历史与会话序列化数据）。</param>
    /// <param name="userInput">用户输入。</param>
    /// <param name="skillPrompt">技能 system prompt；null 表示不使用技能。</param>
    /// <param name="ct">取消令牌（用于停止生成）。</param>
    public async IAsyncEnumerable<string> StreamReplyAsync(
        Conversation conversation,
        string userInput,
        string? skillPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var cfg = _settings.Current;
        if (!cfg.IsConfigured)
            throw new InvalidOperationException("尚未配置 API Key，请点击左下角用户头像进行设置。");

        // OpenAI SDK + 自定义 Endpoint => DeepSeek OpenAI 兼容端点
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(cfg.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(cfg.Endpoint) });

        IChatClient chatClient = openAiClient.GetChatClient(conversation.ModelId).AsIChatClient();

        // MAF：ChatClientAgent 封装 IChatClient，技能提示词经 ChatOptions.Instructions 注入
        var agentOptions = new ChatClientAgentOptions { Name = "MegaAgent" };
        if (!string.IsNullOrWhiteSpace(skillPrompt))
            agentOptions.ChatOptions = new ChatOptions { Instructions = skillPrompt };

        var agent = new ChatClientAgent(chatClient, agentOptions);

        // 恢复或新建 MAF 会话（承载多轮上下文）
        AgentSession session;
        if (!string.IsNullOrWhiteSpace(conversation.ThreadJson))
        {
            var element = JsonDocument.Parse(conversation.ThreadJson).RootElement.Clone();
            session = await agent.DeserializeSessionAsync(element);
        }
        else
        {
            session = await agent.CreateSessionAsync();
        }

        // 流式运行，逐段产出文本
        await foreach (var update in agent.RunStreamingAsync(userInput, session, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }

        // 把会话序列化回存，供下轮恢复
        var serialized = await agent.SerializeSessionAsync(session);
        conversation.ThreadJson = serialized.GetRawText();
    }
}
