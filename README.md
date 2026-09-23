# OpenAI .NET SDK：`GetChatClient()` 与 `GetResponsesClient()` 的区别

基于 [openai/openai-dotnet](https://github.com/openai/openai-dotnet) 当前 `main` 分支源码（提交 `5d1e3b0`）的对照分析。

> 名称纠正：SDK **没有** `OpenAIClient.GetResposeClient()`。正确方法名是 **`GetResponsesClient()`**（Responses 是复数）。

二者都是顶层工厂方法：用同一个 `OpenAIClient` 复用认证、Endpoint 和 HTTP pipeline，再分别拿到 **Chat Completions** 与 **Responses** 两条 REST 产品线的场景客户端。它们不是同一 API 的两种写法。

## 一句话结论

| | `GetChatClient(model)` | `GetResponsesClient()` |
|---|---|---|
| 对应 REST | `POST /v1/chat/completions` | `POST /v1/responses` |
| 产品定位 | 经典、稳定、无状态对话补全 | OpenAI 推荐的新原语，面向 agent / 内置工具 / 服务端状态 |
| SDK 成熟度 | **稳定** | **实验性**（`[Experimental("OPENAI001")]`） |
| 模型怎么传 | **构造客户端时绑定** | **每次调用时指定** |
| 该用谁 | 简单聊天、已有 Chat Completions 代码、跨厂商兼容 | 新项目、推理模型、内置工具、会话链式调用 |

OpenAI 官方当前建议：**新项目用 Responses；Chat Completions 仍会长期支持。** Assistants API 会在 2026-08-26 下线，迁移目标是 Responses，不是 Chat Completions。

---

## 1. 工厂方法本身

源码：`OpenAI/src/Custom/OpenAIClient.cs`

```csharp
// 稳定 API。model 必填，之后每次 CompleteChat 都用这个模型。
public virtual ChatClient GetChatClient(string model)
    => new(Pipeline, model, _options);

// 实验性 API。不接收 model；ResponsesClient 也不再持有 Model 属性。
[Experimental("OPENAI001")]
public virtual ResponsesClient GetResponsesClient()
    => new TopLevelResponsesClient(Pipeline, new ResponsesClientOptions
    {
        Endpoint = _options.Endpoint,
    });
```

共同点：

- 都挂在 `OpenAIClient` 上，用来“派生场景客户端”，而不是自己发请求。
- 每次调用都会 `new` 一个新实例（缓存字段被 `[CodeGenSuppress]` 掉了）。
- 复用同一条 `ClientPipeline`（API Key、重试、组织/项目头、自定义 Endpoint）。

关键差异：

1. **模型绑定时机不同**  
   `ChatClient` 把 `model` 存成字段，请求体里自动填。  
   `ResponsesClient` 在 2025 年之后刻意去掉了 `Model` 属性；必须在 `CreateResponseOptions.Model` 或便利重载参数里传。不需要模型的操作（`GetResponse` / `CancelResponse` / `DeleteResponse`）因此不必先选模型。

2. **稳定性不同**  
   `GetChatClient` 是正式公开 API。  
   `GetResponsesClient` 以及整个 `OpenAI.Responses` 命名空间都标了 `OPENAI001`。不抑制该诊断就编不过：

   ```xml
   <NoWarn>$(NoWarn);OPENAI001</NoWarn>
   ```

3. **构造桥接不同**  
   `ResponsesClient` 的共享 pipeline 构造函数是 `protected internal`。`OpenAIClient` 通过私有嵌套类 `TopLevelResponsesClient : ResponsesClient` 去调它。`ChatClient` 没有这层桥。

4. **Options 类型不同**  
   Chat 走 `OpenAIClientOptions`。Responses 走独立的 `ResponsesClientOptions`（字段几乎一样：Endpoint / OrganizationId / ProjectId / UserAgentApplicationId）。从顶层工厂创建时，只有 Endpoint 被拷过去——其余配置已经烘焙进共享 pipeline。

也可以不经过 `OpenAIClient`，直接 `new ChatClient(...)` / `new ResponsesClient(...)`。工厂方法只是“共享配置”的便捷入口。

### 最小用法对照

```csharp
OpenAIClient openai = new(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

// Chat Completions：模型钉在客户端上
ChatClient chat = openai.GetChatClient("gpt-4o-mini");
ChatCompletion completion = chat.CompleteChat("Say 'this is a test.'");
Console.WriteLine(completion.Content[0].Text);

// Responses：客户端无模型；每次调用带模型
#pragma warning disable OPENAI001
ResponsesClient responses = openai.GetResponsesClient();
ResponseResult response = responses.CreateResponse("gpt-5", "Say 'this is a test.'");
Console.WriteLine(response.GetOutputText());
#pragma warning restore OPENAI001
```

---

## 2. 两条 REST 产品线，不是两个包装器

| | Chat Completions | Responses |
|---|---|---|
| 命名空间 | `OpenAI.Chat` | `OpenAI.Responses` |
| 客户端 | `ChatClient` | `ResponsesClient` |
| 创建 | `CompleteChat` / `CompleteChatStreaming` | `CreateResponse` / `CreateResponseStreaming` |
| 请求体 | `ChatCompletionOptions` + `messages` | `CreateResponseOptions` + `input`（`InputItems`） |
| 输入单位 | `ChatMessage`（role + content 粘在一起） | `ResponseItem`（message / reasoning / function_call / tool output 分开） |
| 返回 | `ChatCompletion`（choices[0].message） | `ResponseResult`（`output` 数组 + 自身 `id`） |
| 读文本 | `completion.Content[0].Text` | `response.GetOutputText()` |
| HTTP | `POST /chat/completions` | `POST /responses` |
| Accept | `application/json` | `application/json, text/event-stream` |

OpenAI 的迁移指南把这个差别说得很明确：Chat Completions 的输入输出都是 **Messages**；Responses 的输入输出是 **Items**。一条 assistant message 不再把文本、工具调用、拒绝、音频全塞进同一个对象。模型做了什么，就产出什么类型的 Item。

---

## 3. 公开操作面

### `ChatClient`

稳定核心：

- `CompleteChat` / `CompleteChatAsync`
- `CompleteChatStreaming` / `CompleteChatStreamingAsync`

实验性（`OPENAI001`）存储管理：

- `GetChatCompletion` / `GetChatCompletions` / `GetChatCompletionMessages`
- `UpdateChatCompletion`（只改 metadata）
- `DeleteChatCompletion`

没有 cancel、没有 compact、没有按 token 预估、没有“用 previous id 接着聊”。多轮对话必须由调用方自己维护 `List<ChatMessage>`，每轮把完整历史再发一遍。

### `ResponsesClient`

创建：

- `CreateResponse(...)` — 非流式；若 `StreamingEnabled == true` 会抛错，必须改用 Streaming 方法
- `CreateResponseStreaming(...)` — 反过来要求 `StreamingEnabled == true`
- 便利重载：`(model, userInputText, previousResponseId)` 和 `(model, inputItems, previousResponseId)`

对象生命周期（这是 Chat 没有的）：

- `GetResponse` / `GetResponseStreaming` — 按 `responseId` 取回，支持从 `startingAfter` 断点续传流
- `CancelResponse` — 取消后台任务
- `DeleteResponse`
- `GetResponseInputItems` — 分页列出当初的 input items
- `GetInputTokenCount` — 估算这次请求会花多少 input token
- `CompactResponse` — 压缩长对话

Chat 把“流还是不流”藏在方法名里（内部设 `Stream`）。Responses 把 `StreamingEnabled` 做成请求字段，并在便利方法和流式方法之间做硬校验。

---

## 4. 状态：谁记住对话

这是选型时影响最大的差别。

**Chat Completions 是无状态的。** 每一轮都要带上完整 `messages`。SDK 示例的典型循环是：

```csharp
messages.Add(new UserChatMessage(userText));
ChatCompletion completion = chat.CompleteChat(messages, options);
messages.Add(new AssistantChatMessage(completion));
```

**Responses 可以让服务端记住上下文：**

1. **`PreviousResponseId`**  
   下一轮只传增量 input，服务端把上一轮 response 接上。便利方法第三个参数就是它。
2. **`ConversationOptions.ConversationId`**  
   接到 Conversations API 的持久会话。`OpenAIClient.GetConversationClient()` 是另一条线，和 Responses 配套，和 Chat 无关。
3. **`StoredOutputEnabled`（`store`）**  
   Responses 默认会存储（官方文档：约 30 天后 TTL，除非挂到 Conversation 上）。Chat Completions 对新账号也会存，但 SDK 里要显式设 `StoredOutputEnabled`，而且没有 `previous_response_id` 这种链式语义。
4. **`BackgroundModeEnabled`**  
   `background=true` 立刻返回 id，之后 `GetResponse` / `CancelResponse`。Chat 没有这个作业模型。

推理模型在 Responses 里还能延续 reasoning item（或在 `store=false` 时带回 `encrypted_content` 再喂回去）。Chat Completions 上，较新的 reasoning 模型 + tool calling 限制更严。

---

## 5. 工具：函数调用 vs 内置 Agent 工具

`ChatToolKind` 目前只有一个值：`Function`。`ChatTool` 的公开工厂也只有：

- `ChatTool.CreateFunctionTool(...)`

网页搜索在 Chat 里是请求选项 `ChatWebSearchOptions`，不是一等工具类型。文件搜索、代码解释器、MCP、Computer Use 都不在 `ChatClient` 的工具模型里。

`ResponseTool` 的工厂方法覆盖一整套 agent 原语：

| 工厂 | 用途 |
|---|---|
| `CreateFunctionTool` | 和 Chat 类似的应用侧函数 |
| `CreateFileSearchTool` | 向量库检索（hosted） |
| `CreateWebSearchTool` / `CreateWebSearchPreviewTool` | 联网搜索（hosted） |
| `CreateCodeInterpreterTool` | 代码执行（hosted） |
| `CreateComputerTool` | Computer Use |
| `CreateMcpTool` | 远程 MCP 服务器 / connector |
| `CreateImageGenerationTool` | 生成过程中出图 |
| `CreateApplyPatchTool` | 打补丁 |
| `CreateCustomTool` | 自定义工具 |

函数调用的回传形状也不一样：

- **Chat**：assistant message 上挂 `ToolCalls`；下一轮追加 `ToolChatMessage(toolCall.Id, result)`。
- **Responses**：输出里出现独立的 `FunctionCallResponseItem`；下一轮追加 `FunctionCallOutputResponseItem(callId, output)`。用 `previous_response_id` 时，通常只需要回传 tool output，不必重放整段历史。

SDK 示例：`examples/Chat/Example03_FunctionCalling.cs` vs `examples/Responses/Example03_FunctionCalling.cs`。

---

## 6. 请求选项对照

两边都有：`Temperature`、`TopP`、`MaxOutputTokenCount`、`Metadata`、`Tools` / `ToolChoice`、并行 tool calls、`EndUserId`、`SafetyIdentifier`、`ServiceTier`、logprobs、store。

Chat 有、Responses 没有（或形态不同）：

- `FrequencyPenalty` / `PresencePenalty` / `Seed` / `StopSequences` / `LogitBiases`
- `ResponseFormat`（JSON / JSON Schema）
- `ResponseModalities` + `AudioOptions`（`gpt-4o-audio-preview` 那套语音 IO）
- `OutputPrediction`（预测加速）
- 已过时的 `Functions` / `FunctionChoice`

Responses 有、Chat 没有：

- `PreviousResponseId`、`ConversationOptions`、`Instructions`
- `ReasoningOptions`（effort 等；Chat 只有一个扁平的 `ReasoningEffortLevel`）
- `TextOptions`（结构化输出走 `text.format`，不是 `response_format`）
- `BackgroundModeEnabled`、`MaxToolCallCount`、`TruncationMode`
- `IncludedProperties`、`PromptCacheKey` / `PromptCacheRetentionPolicy`
- `StreamingEnabled` 作为请求字段

输出形状：

- `ChatCompletion`：`Content`、`FinishReason`、`ToolCalls`、`OutputAudio`、`Usage`……都挂在**一条 completion** 上。
- `ResponseResult`：`OutputItems` 是异构列表（`MessageResponseItem`、`ReasoningResponseItem`、`FileSearchCallResponseItem`、`WebSearchCallResponseItem`、`FunctionCallResponseItem`……），再用 `GetOutputText()` 把 message 里的 `output_text` 拼起来。

流式事件同样更碎：Chat 枚举 `StreamingChatCompletionUpdate`（文本增量、音频增量）；Responses 枚举一棵 `StreamingResponseUpdate` 子类树（item added/done、text delta、reasoning、各类 tool 事件）。

---

## 7. 包结构与依赖注入

源码上 `OpenAI.Responses/` 是单独目录，但 `OpenAI.csproj` 把它编进 **同一个 `OpenAI` NuGet 包**，不是第二个 package：

```xml
<Compile Include="..\..\OpenAI.Responses\src\Custom\**\*.cs" LinkBase="Responses\Custom" />
<Compile Include="..\..\OpenAI.Responses\src\Generated\**\*.cs" LinkBase="Responses\Generated" />
```

DI：

- `builder.AddChatClient("Clients:ChatClient")` — 配置里需要 model（`ChatClientSettings.Model`）
- `builder.AddResponsesClient(...)` — `ResponsesClientSettings` 没有 Model

`ChatClient` 额外接了 OpenTelemetry（`OpenTelemetrySource`）。`ResponsesClient` 自定义层没有对等的 chat-scope telemetry。

---

## 8. 怎么选

用 **`GetChatClient`** 当：

- 只要无状态文本 / 多轮聊天，自己管 history
- 代码已经围绕 `ChatMessage` / `CompleteChat` 写好
- 需要 SDK 稳定面（不想抑制 `OPENAI001`）
- 对接 Azure OpenAI 或其它 Chat Completions 兼容网关
- 需要 Chat 独有的音频模态、logit bias、n-best 这类补全旋钮

用 **`GetResponsesClient`** 当：

- 新项目，尤其是 agent、工具循环、推理模型
- 需要 file search / web search / code interpreter / MCP / computer use
- 想用 `previous_response_id` 或 Conversations，避免每轮重放全文
- 需要后台执行、取消、压缩、按 id 取回、断点续流
- 从即将下线的 Assistants API 迁出来

不要把它们当成可以互换的两个 getter。返回类型、请求体、工具模型和状态语义都不兼容。要从 Chat 迁到 Responses，改的是整条调用链，不是换一个工厂方法。

官方迁移说明：<https://developers.openai.com/api/docs/guides/migrate-to-responses>

---

## 源码索引

| 主题 | 路径 |
|---|---|
| 工厂方法 | `OpenAI/src/Custom/OpenAIClient.cs` |
| Chat 客户端 | `OpenAI/src/Custom/Chat/ChatClient.cs` |
| Chat REST | `OpenAI/src/Generated/ChatClient.RestClient.cs` |
| Responses 客户端 | `OpenAI.Responses/src/Custom/ResponsesClient.cs` |
| Responses REST | `OpenAI.Responses/src/Generated/ResponsesClient.RestClient.cs` |
| 公开 API 清单 | `api/released/net8.0/OpenAI.Chat.net8.0.cs`、`OpenAI.Responses.net8.0.cs` |
| README 示例 | 仓库根 `README.md` 的 Chat / Responses 章节 |
| Chat 示例 | `examples/Chat/` |
| Responses 示例 | `examples/Responses/` |
