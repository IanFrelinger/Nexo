using Nexo.CLI.Commands;
using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class WorkflowCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestScaffoldWritesSpecFileAsync().ConfigureAwait(false);
            await TestHistoryReturnsExpectedSummaryAsync().ConfigureAwait(false);
            await TestStressRunsWithInjectedRequestAndPersistsHistoryAsync(cancellationToken).ConfigureAwait(false);
            await TestStressReturnsFailureWhenExecutorFailsAsync(cancellationToken).ConfigureAwait(false);
            await TestReportGeneratesMarkdownBenchmarkOutputAsync(cancellationToken).ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(WorkflowCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "Workflow command tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(WorkflowCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(WorkflowCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private static WorkflowCommand CreateCommand(
        Func<string, string, string?, bool, bool, CancellationToken, Task<WorkflowCommand.ScenarioExecutionResult>>? scenarioExecutor = null)
    {
        WorkflowCommand.ScenarioExecutor executor = scenarioExecutor is null
            ? StubScenarioExecutorAsync
            : new WorkflowCommand.ScenarioExecutor(scenarioExecutor);
        return new WorkflowCommand(executor);
    }

    private async Task TestScaffoldWritesSpecFileAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var outputPath = Path.Combine(repoRoot, ".nexo", "workflow", "workflow_lab.runtime.json");
            var command = CreateCommand();
            var exitCode = await command.ExecuteScaffoldAsync(outputPath, force: false, json: true).ConfigureAwait(false);
            AssertEqual(0, exitCode);
            AssertTrue(File.Exists(outputPath), "Expected workflow scaffold file to be created.");

            var content = await File.ReadAllTextAsync(outputPath).ConfigureAwait(false);
            AssertTrue(content.Contains("\"compositions\"", StringComparison.OrdinalIgnoreCase), "Scaffold should include compositions.");
            AssertTrue(content.Contains("\"modelProfiles\"", StringComparison.OrdinalIgnoreCase), "Scaffold should include model profiles.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestHistoryReturnsExpectedSummaryAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                ScenarioId = "request-a::composition-a::profile-a::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-a",
                Iteration = 1,
                Success = true,
                Score = 103.5,
                ElapsedMs = 128,
                BenchmarkSet = "workflow-lab"
            });
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                ScenarioId = "request-a::composition-a::profile-b::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-b",
                Iteration = 1,
                Success = false,
                Score = 62.4,
                ElapsedMs = 172,
                BenchmarkSet = "workflow-lab"
            });

            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteHistoryAsync(repoRoot, limit: 10, benchmarkSet: "workflow-lab", json: true)).ConfigureAwait(false);
            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("\"summaryStats\"", StringComparison.OrdinalIgnoreCase), "History output should include summary stats.");
            AssertTrue(output.Contains("\"bestScenarioId\"", StringComparison.OrdinalIgnoreCase), "History output should include best scenario.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestStressRunsWithInjectedRequestAndPersistsHistoryAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            var runtimeSpecJson = """
{
  "execution": {
    "iterations": 1,
    "persistHistory": true,
    "benchmarkSet": "workflow-lab"
  },
  "requests": [
    { "id": "incident", "prompt": "Investigate issue and deliver mitigation plan." }
  ],
  "compositions": [
    {
      "id": "triage-squad",
      "roles": [
        { "agentId": "planner-1", "role": "planner", "domain": "coordination", "goal": "Plan execution", "clusterId": "core" },
        { "agentId": "builder-1", "role": "builder", "domain": "engineering", "goal": "Implement fix", "reportsToAgentId": "planner-1", "commandChain": ["planner-1"] }
      ]
    }
  ],
  "modelProfiles": [
    {
      "id": "profile-a",
      "default": { "prefer": "agentic", "provider": "ollama", "model": "llama3.1" },
      "agents": {
        "planner-1": { "prefer": "agentic", "provider": "ollama", "model": "qwen2.5:7b" },
        "builder-1": { "prefer": "agentic", "provider": "ollama", "model": "codellama:13b" }
      }
    }
  ]
}
""";

            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteStressAsync(
                    requestOverride: "Execute this objective using mandatory hierarchy.",
                    specPath: null,
                    specJson: runtimeSpecJson,
                    providerOverride: null,
                    preferOverride: null,
                    iterationsOverride: null,
                    benchmarkSetOverride: "hardware-lab",
                    persistHistoryOverride: true,
                    json: true,
                    verbose: false,
                    cancellationToken)).ConfigureAwait(false);
            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("\"ok\": true", StringComparison.OrdinalIgnoreCase), "Stress output should report success.");
            AssertTrue(output.Contains("\"aggregates\"", StringComparison.OrdinalIgnoreCase), "Stress output should include aggregate rankings.");

            var historyRows = WorkflowLabHistoryStore.ReadRecent(repoRoot, 10);
            AssertTrue(historyRows.Count >= 1, "Expected stress run to persist at least one history row.");
            AssertEqual("hardware-lab", historyRows[0].BenchmarkSet);
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private static string CreateTempRepoRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-workflow-command-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<(int ExitCode, string Output)> CaptureConsoleAsync(Func<Task<int>> action)
    {
        var previousOut = Console.Out;
        var previousErr = Console.Error;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            Console.SetError(writer);
            var exitCode = await action().ConfigureAwait(false);
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousErr);
        }
    }

    private static Task<WorkflowCommand.ScenarioExecutionResult> StubScenarioExecutorAsync(
        string request,
        string runtimeSpecJson,
        string? provider,
        bool json,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var isValid = !string.IsNullOrWhiteSpace(request) &&
                      !string.IsNullOrWhiteSpace(runtimeSpecJson) &&
                      json;
        return Task.FromResult(
            new WorkflowCommand.ScenarioExecutionResult(
                isValid,
                isValid ? "stub orchestrate success" : "stub orchestrate failure",
                ConflictCount: 1,
                EscalationCount: 1));
    }

    private async Task TestStressReturnsFailureWhenExecutorFailsAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            const string runtimeSpecJson = """
{
  "execution": { "iterations": 1, "persistHistory": false, "benchmarkSet": "workflow-lab" },
  "requests": [ { "id": "request", "prompt": "Do thing" } ],
  "compositions": [ { "id": "comp", "roles": [ { "agentId": "a1", "role": "builder", "goal": "do thing" } ] } ],
  "modelProfiles": [ { "id": "profile", "default": { "prefer": "agentic", "provider": "ollama", "model": "llama3.1" } } ]
}
""";
            var command = CreateCommand((_, _, _, _, _, _) =>
                Task.FromResult(new WorkflowCommand.ScenarioExecutionResult(false, "forced failure", 0, 0)));
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteStressAsync(
                    requestOverride: null,
                    specPath: null,
                    specJson: runtimeSpecJson,
                    providerOverride: null,
                    preferOverride: null,
                    iterationsOverride: null,
                    benchmarkSetOverride: null,
                    persistHistoryOverride: false,
                    json: true,
                    verbose: false,
                    cancellationToken)).ConfigureAwait(false);
            AssertEqual(1, exitCode);
            AssertTrue(output.Contains("\"ok\": false", StringComparison.OrdinalIgnoreCase), "Stress should fail when executor fails.");
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestReportGeneratesMarkdownBenchmarkOutputAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                ScenarioId = "request-a::composition-a::profile-a::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-a",
                Iteration = 1,
                Success = true,
                Score = 91.1,
                ElapsedMs = 120,
                AgentCount = 2,
                ConflictCount = 1,
                EscalationCount = 0,
                BenchmarkSet = "workflow-lab"
            });
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                ScenarioId = "request-a::composition-a::profile-b::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-b",
                Iteration = 1,
                Success = false,
                Score = 40.0,
                ElapsedMs = 320,
                AgentCount = 2,
                ConflictCount = 2,
                EscalationCount = 1,
                BenchmarkSet = "workflow-lab"
            });

            var reportPath = Path.Combine(repoRoot, "workflow_report.md");
            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteReportAsync(
                    repoRoot,
                    limit: 20,
                    benchmarkSet: "workflow-lab",
                    outputPath: reportPath,
                    json: false)).ConfigureAwait(false);

            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("workflow report: ok", StringComparison.OrdinalIgnoreCase), "Report output should indicate success.");
            AssertTrue(File.Exists(reportPath), "Markdown report file should be written.");
            var markdown = await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(false);
            AssertTrue(markdown.Contains("# Workflow Stress Benchmark Report", StringComparison.Ordinal));
            AssertTrue(markdown.Contains("## Top Scenarios", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }
}
