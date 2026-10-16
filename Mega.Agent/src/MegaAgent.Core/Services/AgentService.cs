using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace MegaAgent.Core.Services;

/// <summary>
/// 基于 Microsoft Agent Framework（MAF）的对话服务。
/// 底层使用 OpenAI SDK 指向 DeepSeek 的 OpenAI 兼容端点，
/// 通过 MAF 的 ChatClientAgent 获得会话（AgentSession）管理与后续扩展能力。
/// </summary>
public class AgentService : IAgentService
{
    private readonly ISettingsStore _settings;
    private readonly ILogger<AgentService> _logger;

    public AgentService(ISettingsStore settings, ILogger<AgentService> logger)
    {
        _settings = settings;
        _logger = logger;
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
        {
            _logger.LogWarning("调用被拒绝：尚未配置 API Key，会话 {ConversationId}", conversation.Id);
            throw new InvalidOperationException("尚未配置 API Key，请点击左上角用户头像进行设置。");
        }

        _logger.LogInformation(
            "开始流式对话：会话 {ConversationId}，模型 {Model}，输入 {InputLength} 字，技能 {Skill}",
            conversation.Id, conversation.ModelId, userInput.Length,
            string.IsNullOrWhiteSpace(skillPrompt) ? "无" : "已启用");

        var chatClient = CreateChatClient(cfg.ApiKey, cfg.Endpoint, conversation.ModelId);

        // MAF：ChatClientAgent 封装 IChatClient，技能提示词经 ChatOptions.Instructions 注入
        var agentOptions = new ChatClientAgentOptions { Name = "MegaAgent" };
        if (!string.IsNullOrWhiteSpace(skillPrompt))
            agentOptions.ChatOptions = new ChatOptions { Instructions = skillPrompt };

        var agent = new ChatClientAgent(chatClient, agentOptions);

        // 恢复或新建 MAF 会话（承载多轮上下文）
        AgentSession session;
        if (!string.IsNullOrWhiteSpace(conversation.ThreadJson))
        {
            try
            {
                var element = JsonDocument.Parse(conversation.ThreadJson).RootElement.Clone();
                session = await agent.DeserializeSessionAsync(element);
                _logger.LogDebug("已从持久化数据恢复会话上下文：{ConversationId}", conversation.Id);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // 历史上下文损坏时退化为新会话，避免整条对话不可用
                _logger.LogWarning(ex, "会话上下文恢复失败，改为新建会话：{ConversationId}", conversation.Id);
                session = await agent.CreateSessionAsync();
            }
        }
        else
        {
            session = await agent.CreateSessionAsync();
            _logger.LogDebug("已创建新会话上下文：{ConversationId}", conversation.Id);
        }

        // 流式运行，逐段产出文本
        var sw = Stopwatch.StartNew();
        var chunkCount = 0;
        var charCount = 0;
        try
        {
            await foreach (var update in agent.RunStreamingAsync(userInput, session, cancellationToken: ct))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    chunkCount++;
                    charCount += update.Text.Length;
                    yield return update.Text;
                }
            }
        }
        finally
        {
            // 无论是正常结束还是用户点了「停止」，都记录一次调用结果
            _logger.LogInformation(
                "流式对话结束：会话 {ConversationId}，{ChunkCount} 段 / {CharCount} 字，耗时 {Elapsed} ms",
                conversation.Id, chunkCount, charCount, sw.ElapsedMilliseconds);
        }

        // 把会话序列化回存，供下轮恢复
        var serialized = await agent.SerializeSessionAsync(session);
        conversation.ThreadJson = serialized.GetRawText();
    }

    /// <summary>
    /// 用指定参数测试连通性，返回模型回复文本；失败时抛出异常。
    /// 供「模型设置」对话框在不落盘配置的前提下验证端点与 Key。
    /// </summary>
    public async Task<string> TestConnectionAsync(
        string apiKey,
        string endpoint,
        string model,
        CancellationToken ct = default)
    {
        _logger.LogInformation("测试模型连通性：模型 {Model}，端点 {Endpoint}，Key {MaskedKey}",
            model, endpoint, MaskApiKey(apiKey));

        var sw = Stopwatch.StartNew();
        try
        {
            var chatClient = CreateChatClient(apiKey, endpoint, model);
            var reply = await chatClient.GetResponseAsync("ping", cancellationToken: ct);
            _logger.LogInformation("连通性测试成功：模型 {Model}，耗时 {Elapsed} ms", model, sw.ElapsedMilliseconds);
            return reply.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连通性测试失败：模型 {Model}，端点 {Endpoint}", model, endpoint);
            throw;
        }
    }

    /// <summary>OpenAI SDK + 自定义 Endpoint =&gt; DeepSeek OpenAI 兼容端点。</summary>
    private static IChatClient CreateChatClient(string apiKey, string endpoint, string model)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return openAiClient.GetChatClient(model).AsIChatClient();
    }

    /// <summary>日志里不出现完整 Key：只保留头尾各 4 位。</summary>
    private static string MaskApiKey(string? apiKey)
        => string.IsNullOrEmpty(apiKey) || apiKey.Length <= 10
            ? "(未设置)"
            : $"{apiKey[..4]}****{apiKey[^4..]}";
}
