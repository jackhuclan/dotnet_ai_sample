using dotnet_ai_agent_sample;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Xunit;

namespace dotnet_ai_sample;

public class Test3 : BaseTest
{
    [Fact]
    public async Task Test()
    {
        var model = "qwen3:1.7b";

        var chatClient = new OpenAIClient(new ApiKeyCredential("sk-token"), new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434/v1"),
        }).GetChatClient(model)
        .AsIChatClient();

        var agent = chatClient.AsAIAgent();
        var response = await agent.RunAsync("hello");

        Assert.NotEmpty(response.Text);

    }

    [Fact]
    public async Task CallTool()
    {
        var model = "qwen3:1.7b";

        var chatClient = new OpenAIClient(new ApiKeyCredential("sk-token"), new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434/v1"),
        }).GetChatClient(model)
        .AsIChatClient();

        string str = string.Empty;
        Action test = () =>
        {
            str = "helloworld";
        };

        var agent = chatClient.AsAIAgent("你是一个ai助手，能调用各种工具", tools: [
            AIFunctionFactory.Create(test, "test", "测试")
            ]);
        var response = await agent.RunAsync("调用下'test'工具");

        Assert.Equal("helloworld", str);
    }
}
