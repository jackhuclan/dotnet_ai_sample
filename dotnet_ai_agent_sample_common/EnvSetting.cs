using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace dotnet_ai_agent_sample;

public class EnvSetting
{
    public EnvSetting()
    {
        Env.Load();

        OPENAI_CHAT_MODEL_NAME = Env.GetString("OPENAI_CHAT_MODEL_NAME");
        DEEPSEEK_PRO_MODEL_NAME = Env.GetString("DEEPSEEK_PRO_MODEL_NAME");
        OPENAI_API_KEY = Env.GetString("OPENAI_API_KEY");
    }

    public string OPENAI_CHAT_MODEL_NAME { get; }
    public string DEEPSEEK_PRO_MODEL_NAME { get; }
    public string OPENAI_API_KEY { get; }

    public IChatClient GetChatClient()
    {
        return new OpenAIClient(new ApiKeyCredential(OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(OPENAI_CHAT_MODEL_NAME).AsIChatClient();
    }

    public IChatClient GetProChatClient()
    {
        return new OpenAIClient(new ApiKeyCredential(OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(DEEPSEEK_PRO_MODEL_NAME).AsIChatClient();
    }

    public OpenAIClient GetOpenAIClient()
    {
        return new OpenAIClient(new ApiKeyCredential(OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        });
    }
}
