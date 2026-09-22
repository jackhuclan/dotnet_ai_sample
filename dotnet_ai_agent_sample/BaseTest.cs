using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

public class BaseTest
{
    static BaseTest()
    {
        DotNetEnv.Env.Load();
    }

    public string OPENAI_CHAT_MODEL_NAME => Env.GetString("OPENAI_CHAT_MODEL_NAME");
    public string DEEPSEEK_PRO_MODEL_NAME => Env.GetString("DEEPSEEK_PRO_MODEL_NAME");
    public string OPENAI_API_KEY => Env.GetString("OPENAI_API_KEY");

    public IChatClient GetChatClient()
    {
        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        //var model = Env.GetString("OPENAI_CHAT_MODEL_NAME");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(OPENAI_CHAT_MODEL_NAME).AsIChatClient();
    }

    public IChatClient GetProChatClient()
    {
        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        //var model = Env.GetString("DEEPSEEK_PRO_MODEL_NAME");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(DEEPSEEK_PRO_MODEL_NAME).AsIChatClient();
    }

    public OpenAIClient GetOpenAIClient()
    {
        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        });
    }
}
