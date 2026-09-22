using Microsoft.Agents.AI.Workflows;
using System.Text.Json.Serialization;
using Xunit;
using Console = System.Diagnostics.Debug;
using Run = Microsoft.Agents.AI.Workflows.Run;

namespace dotnet_ai_agent_sample.workflow;

public class WorkflowTest
{
    [Fact]
    public async Task TestWorkflow()
    {
        Console.WriteLine("\n=== Sub-Workflow Demonstration ===\n");

        FirstExecutor first = new FirstExecutor();
        SecondExecutor second = new();

        var workflow = new WorkflowBuilder(first)
            .AddEdge(first, second)
            .WithOutputFrom(second)
            .Build();

        // Step 4: Execute the main workflow
        await using Run run = await InProcessExecution.RunAsync(workflow, "zhangsan");

        // Display results
        foreach (WorkflowEvent evt in run.NewEvents)
        {
            if (evt is ExecutorCompletedEvent executorComplete && executorComplete.Data is not null)
            {
                Console.WriteLine($"[{executorComplete.ExecutorId}] {executorComplete.Data}");
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine("\n=== Main Workflow Completed ===");
                Console.WriteLine($"Final Output: {output.Data}");
            }
            else if (evt is WorkflowErrorEvent workflowError)
            {
                Console.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");

            }
            else if (evt is ExecutorFailedEvent executorFailed)
            {
                Console.WriteLine($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data == null ? "unknown error" : $"exception {executorFailed.Data}")}.");
            }
        }
    }
}

// ====================================
// Text Processing Executors
// ====================================

/// <summary>
/// Adds a prefix to the input text.
/// </summary>
internal sealed class FirstExecutor() : Executor<string, PersonInfo>("FirstExecutor")
{
    public override ValueTask<PersonInfo> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new PersonInfo { Name = message, Age = 100 });
    }
}

/// <summary>
/// Converts input text to uppercase.
/// </summary>
internal sealed class SecondExecutor() : Executor<PersonInfo, string>("SecondExecutor")
{
    public override ValueTask<string> HandleAsync(PersonInfo personInfo, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string result = personInfo.Name.ToUpperInvariant();
        return ValueTask.FromResult(result);
    }
}

internal class PersonInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; } = 100;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
}