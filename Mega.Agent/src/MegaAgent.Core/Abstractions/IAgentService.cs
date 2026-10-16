using MegaAgent.Core.Models;

namespace MegaAgent.Core.Abstractions;

/// <summary>
/// 模型调用契约：所有对大模型的访问（流式对话、连通性测试）都收敛到这里，
/// 上层（ViewModel）只依赖本接口，不直接接触 OpenAI SDK / MAF 类型。
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// 发送一条用户消息并流式接收回复。
    /// 多轮上下文由 MAF AgentSession 维护（序列化保存在 <see cref="Conversation.ThreadJson"/>）。
    /// </summary>
    /// <param name="conversation">目标会话（携带历史与会话序列化数据）。</param>
    /// <param name="userInput">用户输入。</param>
    /// <param name="skillPrompt">技能 system prompt；null 表示不使用技能。</param>
    /// <param name="ct">取消令牌（用于停止生成）。</param>
    IAsyncEnumerable<string> StreamReplyAsync(
        Conversation conversation,
        string userInput,
        string? skillPrompt,
        CancellationToken ct = default);

    /// <summary>
    /// 用指定参数测试连通性，返回模型回复文本；失败时抛出异常。
    /// 供「模型设置」的「测试连接」使用（无需先落盘配置）。
    /// </summary>
    Task<string> TestConnectionAsync(
        string apiKey,
        string endpoint,
        string model,
        CancellationToken ct = default);
}
