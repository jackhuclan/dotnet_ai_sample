using dotnet_ai_agent_sample_common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;

namespace dotnet_ai_agent_sample;

public class Agent_OpenAI_Step02_Reasoning
{
    public async Task Run()
    {
        var envSetting = new EnvSetting();
        var client = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetResponsesClient()
         .AsIChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
         .AsBuilder()
         .ConfigureOptions(o =>
         {
             o.Reasoning = new()
             {
                 Effort = ReasoningEffort.Medium,
                 Output = ReasoningOutput.Full,
             };
         })
         .Build();

        AIAgent agent = new ChatClientAgent(client);

        Console.WriteLine("1. Non-streaming:");
        var response = await agent.RunAsync("解决下面的问题：如果一辆火车以每小时60英里的速度行驶，需要行驶180英里，那么行程需要多长时间？请展示你的推理过程。");

        Console.WriteLine(response.Text);

        Console.WriteLine("Token usage:");
        Console.WriteLine($"Input: {response.Usage?.InputTokenCount}, Output: {response.Usage?.OutputTokenCount}, {string.Join(", ", response.Usage?.AdditionalCounts ?? [])}");
        Console.WriteLine();

        Console.WriteLine("2. Streaming");
        await foreach (var update in agent.RunStreamingAsync("用简单的话解释相对论。"))
        {
            foreach (var item in update.Contents)
            {
                if (item is TextReasoningContent reasoningContent)
                {
                    Console.Write($"\e[38;2;255;165;0m{reasoningContent.Text}\e[0m");
                    //Console.Write($"\e[97m{reasoningContent.Text}\e[0m");
                }
                else if (item is TextContent textContent)
                {
                    Console.Write(textContent.Text);
                }
            }
        }
    }

    public async Task Run2()
    {
        var envSetting = new EnvSetting();

        AIAgent agent = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
        .AsAIAgent(new ChatClientAgentOptions
        {
            //Name = "",
            //Description = "",
            //ChatOptions = new ChatOptions
            //{
            //    ModelId = envSetting.DEEPSEEK_PRO_MODEL_NAME,
            //    Instructions = "",
            //    Reasoning = new ReasoningOptions { Effort = ReasoningEffort.High },
            //    Tools = []
            //},
            //EnableMessageInjection = true,
            AIContextProviders = []
        });

        //var aa = agent.RunStreamingAsync(options: new ChatClientAgentRunOptions
        //{
        //    ChatOptions = new ChatOptions
        //    {
        //        Tools = []
        //    }
        //});

        Console.WriteLine("1. Non-streaming:");
        var response = await agent.RunAsync("解决下面的问题：如果一辆火车以每小时60英里的速度行驶，需要行驶180英里，那么行程需要多长时间？请展示你的推理过程。");

        Console.WriteLine(response.Text);

        Console.WriteLine("Token usage:");
        Console.WriteLine($"Input: {response.Usage?.InputTokenCount}, Output: {response.Usage?.OutputTokenCount}, {string.Join(", ", response.Usage?.AdditionalCounts ?? [])}");
        //Console.WriteLine();

        Console.WriteLine("2. Streaming");
        await foreach (var update in agent.RunStreamingAsync("用简单的话解释相对论。"))
        {
            foreach (var item in update.Contents)
            {
                if (item is TextReasoningContent reasoningContent)
                {
                    Console.Write($"\e[38;2;255;165;0m{reasoningContent.Text}\e[0m");
                    //Console.Write($"\e[97m{reasoningContent.Text}\e[0m");
                }
                else if (item is TextContent textContent)
                {
                    Console.Write(textContent.Text);
                }
            }
        }
    }
    // Function invocation middleware that logs before and after function calls.
    async ValueTask<object?> FunctionCallMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Function Name: {context!.Function.Name} - Middleware 1 Pre-Invoke");
        //string skillName = context.CallContent.Arguments["skillName"]?.ToString() ?? string.Empty;
        //context.Options.Tools
        var result = await next(context, cancellationToken);
        Console.WriteLine($"Function Name: {context!.Function.Name} - Middleware 1 Post-Invoke");

        return result;
    }

    public async Task Run3()
    {
        var envSetting = new EnvSetting();

        var myAgentSkillsProvider = new MyAgentSkillsProvider();
        var skillsProvider = new AgentSkillsProvider(Path.Combine(AppContext.BaseDirectory, "skills"));

        AIAgent agent = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
        .AsAIAgent(new ChatClientAgentOptions
        {
            //DisableApprovalNotRequiredFunctionBypassing = true,
            Name = "aoi_assistant",
            Description = "you are assistant for aoi device",
            ChatOptions = new ChatOptions
            {
                Instructions = "you are assistant for aoi device, please calling tools to complete jobs",
                Tools =
                [
                    AIFunctionFactory.Create(this.ToolFirst, name:"tool_first"),
                    AIFunctionFactory.Create(this.ToolSecond, name:"tool_second"),
                ],
                Reasoning = new()
                {
                    Effort = ReasoningEffort.None,
                    //Output = ReasoningOutput.Full,
                }
            },
            AIContextProviders = [skillsProvider, myAgentSkillsProvider]
        }).AsBuilder()
        .Use(FunctionCallMiddleware)
        .UseToolApproval(new ToolApprovalAgentOptions
        {
            // Auto-approve read-only skill tools (load_skill, read_skill_resource).
            // run_skill_script will still require explicit user approval.
            AutoApprovalRules = [AgentSkillsProvider.ReadOnlyToolsAutoApprovalRule],
        })
        .Build();


        //var client = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
        //{
        //    Endpoint = new Uri("https://api.deepseek.com"),
        //}).GetChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
        //.AsIChatClient()
        ////.GetResponsesClient()
        ////.AsIChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
        //.AsBuilder()
        //.ConfigureOptions(o =>
        //{
        //    o.Instructions = "you are assistant for aoi device, please calling tools to complete jobs";
        //    o.Tools =
        //        [
        //            AIFunctionFactory.Create(this.ToolFirst, name:"tool_first"),
        //            AIFunctionFactory.Create(this.ToolSecond, name:"tool_second"),
        //        ];
        //    o.Reasoning = new()
        //    {
        //        Effort = ReasoningEffort.Medium,
        //        Output = ReasoningOutput.Full,
        //    };
        //}).Build();

        //AIAgent agent = new ChatClientAgent(client);

        Console.WriteLine("开始调用技能...");
        var chatClientAgentRunOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools =
                [
                    AIFunctionFactory.Create(this.ToolThird, name:"tool_third"),
                    AIFunctionFactory.Create(this.ToolFourth, name:"tool_fourth"),
                ]
            }
        };
        //var agentSession = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("你有哪些技能，你有哪些可用的工具？", options: chatClientAgentRunOptions);

        Console.WriteLine(response.Text);

        //await Task.Delay(5000);

        var response2 = await agent.RunAsync("第一步：给我拍照，第二步：帮我创建配方", options: chatClientAgentRunOptions);
        Console.WriteLine(response2.Text);

        //var response2 = agent.RunStreamingAsync("给我拍照", session: agentSession);
        //await foreach (var update in response2)
        //{
        //    foreach (var item in update.Contents)
        //    {
        //        if (item is TextReasoningContent reasoningContent)
        //        {
        //            Console.Write($"\e[97m{reasoningContent.Text}\e[0m");
        //        }
        //        else if (item is TextContent textContent)
        //        {
        //            Console.Write(textContent.Text);
        //        }
        //    }
        //}

        //await Task.Delay(10 * 1000);

    }

    public async Task Run4()
    {
        var envSetting = new EnvSetting();

        var skillsProvider = new AgentSkillsProvider(Path.Combine(AppContext.BaseDirectory, "skills"));
        AIAgent agent = new OpenAIClient(new ApiKeyCredential(envSetting.OPENAI_API_KEY), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(envSetting.OPENAI_CHAT_MODEL_NAME)
        .AsAIAgent(new ChatClientAgentOptions
        {
            //DisableApprovalNotRequiredFunctionBypassing = true,
            Name = "aoi_assistant",
            Description = "you are assistant for aoi device",
            ChatOptions = new ChatOptions
            {
                Instructions = "you are assistant for aoi device, please calling tools to complete jobs",
                Tools =
                [
                    AIFunctionFactory.Create(this.ToolFirst, name:"tool_first"),
                    AIFunctionFactory.Create(this.ToolSecond, name:"tool_second"),
                ],
                Reasoning = new()
                {
                    Effort = ReasoningEffort.None,
                    //Output = ReasoningOutput.Full,
                }
            },
            AIContextProviders = [skillsProvider]
        }).AsBuilder()
        .UseToolApproval(new ToolApprovalAgentOptions
        {
            // Auto-approve read-only skill tools (load_skill, read_skill_resource).
            // run_skill_script will still require explicit user approval.
            AutoApprovalRules = [AgentSkillsProvider.ReadOnlyToolsAutoApprovalRule],
        })
        .Build();
    }

    //[Description("load tools")]
    //public string LoadTools([Description("the name of skill")] string skillName)
    //{
    //    Console.WriteLine("the first tool is called");
    //    return aa;
    //}

    [Description("first tool")]
    public string ToolFirst([Description("the input parameter")] string aa)
    {
        Console.WriteLine("the first tool is called");
        return aa;
    }

    [Description("second tool")]
    public string ToolSecond([Description("the input parameter")] string aa)
    {
        Console.WriteLine("the second tool is called");
        return aa;
    }

    [Description("third tool")]
    public string ToolThird([Description("the input parameter")] string aa)
    {
        Console.WriteLine("the third tool is called");
        return aa;
    }

    [Description("the fourth tool")]
    public string ToolFourth([Description("the input parameter")] string aa)
    {
        Console.WriteLine("the fourth tool is called");
        return aa;
    }
}
