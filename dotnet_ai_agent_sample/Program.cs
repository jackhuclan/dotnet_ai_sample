namespace dotnet_ai_agent_sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        //await new Agent_OpenAI_Step01_Running().Run();
        //await new Agent_OpenAI_Step02_Reasoning().Run();
        //await new Agent_OpenAI_Step03_CreateFromChatClient().Run();
        //await new Agent_OpenAI_Step04_CreateFromOpenAIResponseClient().Run();
        //await new Agent_OpenAI_Step05_Conversation().Run();
        //await new Agent_OpenAI_Step06_CodeInterpreterFileDownload().Run();

        await new Agent_Step21_WebSearch().Run();
    }
}
