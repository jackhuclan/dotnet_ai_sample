using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using Xunit;

namespace dotnet_ai_agent_sample;

public sealed class Test1
{
    /// <summary>
    /// 测试证明：通过函数来定义一个tool，方法和参数都需要有Description特性，才能被AIFunctionFactory.Create识别为一个有效的tool
    /// 
    /// {"type":"object","properties":{"location":{"description":"The location to get the weather for.","type":"string"}},"required":["location"]}
    /// </summary>
    [Fact]
    public void TestMethod1()
    {
        var aIFunction = AIFunctionFactory.Create(Test1.GetWeather);
        Assert.NotEmpty(aIFunction.Name);
        Assert.NotEmpty(aIFunction.Description);

        var jsonSchema = aIFunction.JsonSchema.ToString();
        Assert.NotEmpty(jsonSchema);

        var outputSchema = aIFunction.ReturnJsonSchema?.ToString();
        Assert.NotNull(outputSchema);
    }

    [Fact]
    public void TestMethod2()
    {
        var aIFunction = AIFunctionFactory.Create(Test1.GetCity);
        Assert.NotEmpty(aIFunction.Name);
        Assert.Empty(aIFunction.Description);
    }

    [Fact]
    public void TestMethod3()
    {
        var seviceCollection = new ServiceCollection();
        seviceCollection.AddSingleton<IRecipeService, RecipeServiceImpl>();
        var serviceProvider = seviceCollection.BuildServiceProvider();

        var aa = ActivatorUtilities.CreateInstance<RecipeServiceCatalog>(serviceProvider, serviceProvider.GetRequiredService<IRecipeService>());
        var aIFunction = AIFunctionFactory.Create(aa.GetRecipeAsync);
        Assert.NotEmpty(aIFunction.Name);
        Assert.NotEmpty(aIFunction.Description);
        var jsonSchema = aIFunction.JsonSchema.ToString();
        Assert.NotEmpty(jsonSchema);
        var outputSchema = aIFunction.ReturnJsonSchema?.ToString();
        Assert.NotNull(outputSchema);
    }

    [Fact]
    public async Task CreateAgent()
    {
        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        var model = Env.GetString("OPENAI_CHAT_MODEL_NAME");
        var chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(model)
        .AsIChatClient();

        AIAgent agent = chatClient
          .AsAIAgent();

        var response = await agent.RunAsync("你是谁？");

        Assert.NotEmpty(response.Text);
    }


    const int MaxContextWindowTokens = 1_050_000;
    const int MaxOutputTokens = 128_000;

    [Fact]
    public void TestMethod4()
    {
        var instructions =
    """
    ## Technical Assistant Instructions

    You are a code-powered technical assistant. You can execute Python code in a sandboxed environment
    to solve problems precisely rather than guessing. You also have access to skills that provide
    structured workflows for specific technical tasks.

    ### Code Execution

    When a problem requires computation, validation, or testing:
    - Write Python code and use `execute_code` to run it in the sandbox.
    - Always verify results by running the code rather than reasoning about what would happen.
    - If code fails, read the error message carefully, fix the issue, and retry.

    ### Skills

    You have access to discoverable skills. When a task matches a skill's description:
    - Follow the skill's instructions carefully.
    - Use the skill's reference materials for context.
    - Combine the skill's workflow with code execution when appropriate.

    ### Planning and Research

    For complex tasks:
    - Break the problem into steps using your todo list.
    - Research background information using web search when needed.
    - Save important findings to file memory for later reference.

    ### Presenting Results

    - Show your work: include the code you ran and its output.
    - Explain what each part of your solution does.
    - If applicable, save final results to file memory.
    """;


        var seviceCollection = new ServiceCollection();
        seviceCollection.AddSingleton<IRecipeService, RecipeServiceImpl>();
        var serviceProvider = seviceCollection.BuildServiceProvider();

        var aa = new RecipeServiceCatalog(serviceProvider.GetRequiredService<IRecipeService>());
        List<AITool> tools = [.. aa.AsAITools()];

        // --- Skills Provider ---
        // Discovers skills from the 'skills' directory containing SKILL.md files.
        // The script runner runs file-based scripts (e.g. Python) as local subprocesses.
        var skillsProvider = new AgentSkillsProvider(
            Path.Combine(AppContext.BaseDirectory, "skills"),
            SubprocessScriptRunner.RunAsync);


        Env.Load();
        var apiKey = DotNetEnv.Env.GetString("OPENAI_API_KEY");
        var model = DotNetEnv.Env.GetString("OPENAI_CHAT_MODEL_NAME");
        AIAgent agent = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(model)
        .AsIChatClient()
        .AsHarnessAgent(new HarnessAgentOptions
        {
            MaxContextWindowTokens = MaxContextWindowTokens,
            MaxOutputTokens = MaxOutputTokens,
            Name = "aoi-agent",
            Description = "An agent for executing code and providing technical assistance.",
            AIContextProviders = [skillsProvider],
            //FileMemoryStore = new FileSystemAgentFileStore(Path.Combine(AppContext.BaseDirectory, "agent-files")),
            //FileAccessStore = new FileSystemAgentFileStore(Path.Combine(AppContext.BaseDirectory, "working")),
            //DisableAgentSkillsProvider = true,
            //BackgroundAgents = [researchAgent],
            //OpenTelemetrySourceName = OpenTelemetrySourceName,
            //ToolApprovalAgentOptions = new ToolApprovalAgentOptions
            //{
            //    AutoApprovalRules = autoApprovalRules,
            //},
            AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
            //AIContextProviders = contextProviders,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = tools,
                Reasoning = new() { Effort = ReasoningEffort.Medium },
            },
        });
    }

    [Fact]
    public async Task TestMethod5()
    {
        var skillsProvider = new AgentSkillsProvider(Path.Combine(AppContext.BaseDirectory, "skills"));

        DotNetEnv.Env.Load();
        var apiKey = Env.GetString("OPENAI_API_KEY");
        var model = Env.GetString("OPENAI_CHAT_MODEL_NAME");
        var aIAgent = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
        }).GetChatClient(model)
        .AsIChatClient()
        .AsAIAgent(new ChatClientAgentOptions
        {
            //Description = "You are a helpful assistant that can convert units.",
            Name = "UnitConverterAgent",
            ChatOptions = new ChatOptions
            {
                ModelId = model,
                Instructions = "You are a helpful assistant that can convert units.",
                Reasoning = new() { Effort = ReasoningEffort.None },
            },
            AIContextProviders = [skillsProvider],
        }).AsBuilder()
        .UseToolApproval(new ToolApprovalAgentOptions
        {
            AutoApprovalRules = [AgentSkillsProvider.AllToolsAutoApprovalRule],
        })
        .Build();

        AgentResponse response = await aIAgent.RunAsync("How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(response.Text);

    }

    [Fact]
    public void YamlTest()
    {
        var aa = new ConfigurationBuilder()
            .AddYamlFile(Path.Combine(AppContext.BaseDirectory, "./skills/unit-converter/references/recipe-skill-tools.yml"), optional: false, reloadOnChange: true)
            .Build();

        var name = aa["name"];
        var classpath = aa["classpath"];
        var tools = aa.GetSection("tools").GetChildren().ToList();
        Assert.NotEmpty(name);
        Assert.NotEmpty(classpath);

        foreach (var tool in tools)
        {
            var parameters = tool.GetValue<string>("parameters");
            var aIFunction = AIFunctionFactory.CreateDeclaration(
                     name: tool.GetValue<string>("name") ?? string.Empty,
                     description: tool.GetValue<string>("description") ?? string.Empty,
                     jsonSchema: JsonElement.Parse("{\n" + parameters + "}\n"),
                     returnJsonSchema: JsonElement.Parse("{}"));
        }
    }

    [Description("Get the weather for a given location.")]
    static string GetWeather([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 15°C.";


    static string GetCity() => $"suzhou";
}
