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
            await TestReportFiltersByRunIdAsync(cancellationToken).ConfigureAwait(false);
            await TestStressClassifiesRuntimeContextFailureFromErrorCodeAsync(cancellationToken).ConfigureAwait(false);
            await TestStressHonorsWarmupShuffleAndCooldownExecutionControlsAsync(cancellationToken).ConfigureAwait(false);
            await TestReportIncludesComparisonSectionAsync(cancellationToken).ConfigureAwait(false);
            await TestGatePassesAndFailsWithThresholdsAsync().ConfigureAwait(false);
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
                    warmupRunsOverride: null,
                    shuffleScenariosOverride: null,
                    randomSeedOverride: null,
                    cooldownMsOverride: null,
                    json: true,
                    verbose: false,
                    ct: cancellationToken)).ConfigureAwait(false);
            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("\"ok\": true", StringComparison.OrdinalIgnoreCase), "Stress output should report success.");
            AssertTrue(output.Contains("\"aggregates\"", StringComparison.OrdinalIgnoreCase), "Stress output should include aggregate rankings.");

            var historyRows = WorkflowLabHistoryStore.ReadRecent(repoRoot, 10);
            AssertTrue(historyRows.Count >= 1, "Expected stress run to persist at least one history row.");
            AssertEqual("hardware-lab", historyRows[0].BenchmarkSet);
            AssertEqual("none", historyRows[0].FailureCategory);
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
                Task.FromResult(new WorkflowCommand.ScenarioExecutionResult(false, "forced failure", 0, 0, false, "executor_failure")));
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
                    warmupRunsOverride: null,
                    shuffleScenariosOverride: null,
                    randomSeedOverride: null,
                    cooldownMsOverride: null,
                    json: true,
                    verbose: false,
                    ct: cancellationToken)).ConfigureAwait(false);
            AssertEqual(1, exitCode);
            AssertTrue(output.Contains("\"ok\": false", StringComparison.OrdinalIgnoreCase), "Stress should fail when executor fails.");
            AssertTrue(output.Contains("forced failure", StringComparison.OrdinalIgnoreCase), "Stress output should include executor failure summary.");
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
                FailureCategory = "orchestration_failure",
                BenchmarkSet = "workflow-lab"
            });

            var reportPath = Path.Combine(repoRoot, "workflow_report.md");
            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteReportAsync(
                    repoRoot,
                    limit: 20,
                    benchmarkSet: "workflow-lab",
                    runId: null,
                    baselineRunId: null,
                    since: null,
                    outputPath: reportPath,
                    json: false)).ConfigureAwait(false);

            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("workflow report: ok", StringComparison.OrdinalIgnoreCase), "Report output should indicate success.");
            AssertTrue(File.Exists(reportPath), "Markdown report file should be written.");
            var markdown = await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(false);
            AssertTrue(markdown.Contains("# Workflow Stress Benchmark Report", StringComparison.Ordinal));
            AssertTrue(markdown.Contains("## Top Scenarios", StringComparison.OrdinalIgnoreCase));
            AssertTrue(markdown.Contains("## Failure Categories", StringComparison.OrdinalIgnoreCase));
            AssertTrue(markdown.Contains("orchestration_failure", StringComparison.OrdinalIgnoreCase));
            AssertTrue(markdown.Contains("## Recommendations", StringComparison.OrdinalIgnoreCase));
            AssertTrue(markdown.Contains("global_baseline", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestReportFiltersByRunIdAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "run-a",
                GitSha = "abc123",
                SpecHash = "spec-a",
                ProviderSnapshot = "ollama",
                ScenarioId = "request-a::composition-a::profile-a::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-a",
                Iteration = 1,
                Success = true,
                Score = 90.0,
                ElapsedMs = 100,
                BenchmarkSet = "workflow-lab"
            });
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "run-b",
                GitSha = "def456",
                SpecHash = "spec-b",
                ProviderSnapshot = "ollama",
                ScenarioId = "request-b::composition-b::profile-b::iter-1",
                RequestId = "request-b",
                CompositionId = "composition-b",
                ModelProfileId = "profile-b",
                Iteration = 1,
                Success = false,
                Score = 10.0,
                ElapsedMs = 300,
                FailureCategory = "orchestration_failure",
                BenchmarkSet = "workflow-lab"
            });

            var reportPath = Path.Combine(repoRoot, "workflow_report_run_a.md");
            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteReportAsync(
                    repoRoot,
                    limit: 20,
                    benchmarkSet: "workflow-lab",
                    runId: "run-a",
                    baselineRunId: null,
                    since: null,
                    outputPath: reportPath,
                    json: false)).ConfigureAwait(false);

            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("workflow report: ok", StringComparison.OrdinalIgnoreCase));
            var markdown = await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(false);
            AssertTrue(markdown.Contains("Run ID: run-a", StringComparison.Ordinal));
            AssertTrue(!markdown.Contains("run-b", StringComparison.OrdinalIgnoreCase), "Report should filter out run-b data.");
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestStressClassifiesRuntimeContextFailureFromErrorCodeAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            const string runtimeSpecJson = """
{
  "execution": { "iterations": 1, "persistHistory": true, "benchmarkSet": "workflow-lab" },
  "requests": [ { "id": "request", "prompt": "Do thing" } ],
  "compositions": [ { "id": "comp", "roles": [ { "agentId": "a1", "role": "builder", "goal": "do thing" } ] } ],
  "modelProfiles": [ { "id": "profile", "default": { "prefer": "agentic", "provider": "ollama", "model": "llama3.1" } } ]
}
""";
            var command = CreateCommand((_, _, _, _, _, _) =>
            {
                return Task.FromResult(new WorkflowCommand.ScenarioExecutionResult(false, "forced failure", 0, 0, false, "runtime_context_failure"));
            });
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteStressAsync(
                    requestOverride: null,
                    specPath: null,
                    specJson: runtimeSpecJson,
                    providerOverride: null,
                    preferOverride: null,
                    iterationsOverride: null,
                    benchmarkSetOverride: null,
                    persistHistoryOverride: true,
                    warmupRunsOverride: null,
                    shuffleScenariosOverride: null,
                    randomSeedOverride: null,
                    cooldownMsOverride: null,
                    json: true,
                    verbose: false,
                    ct: cancellationToken)).ConfigureAwait(false);
            AssertEqual(1, exitCode);
            var historyRows = WorkflowLabHistoryStore.ReadRecent(repoRoot, 5);
            AssertTrue(historyRows.Count >= 1, "Expected failure row to be persisted.");
            AssertEqual("runtime_context_failure", historyRows[0].FailureCategory);
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestStressHonorsWarmupShuffleAndCooldownExecutionControlsAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        var calls = new List<string>();
        var command = CreateCommand((request, _, _, _, _, _) =>
        {
            calls.Add(request);
            return Task.FromResult(new WorkflowCommand.ScenarioExecutionResult(true, "ok", 0, 0));
        });

        try
        {
            Environment.CurrentDirectory = repoRoot;
            const string runtimeSpecJson = """
{
  "execution": {
    "iterations": 1,
    "persistHistory": false,
    "benchmarkSet": "workflow-lab",
    "warmupRuns": 1,
    "cooldownMs": 2,
    "shuffleScenarioOrder": true,
    "randomSeed": 1337
  },
  "requests": [
    { "id": "r1", "prompt": "Do one" },
    { "id": "r2", "prompt": "Do two" }
  ],
  "compositions": [
    { "id": "c1", "roles": [ { "agentId": "a1", "role": "builder", "goal": "do thing" } ] },
    { "id": "c2", "roles": [ { "agentId": "a2", "role": "builder", "goal": "do thing" } ] }
  ],
  "modelProfiles": [
    { "id": "p1", "default": { "prefer": "agentic", "provider": "ollama", "model": "llama3.1" } }
  ]
}
""";

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
                    warmupRunsOverride: null,
                    shuffleScenariosOverride: null,
                    randomSeedOverride: null,
                    cooldownMsOverride: null,
                    json: true,
                    verbose: false,
                    ct: cancellationToken)).ConfigureAwait(false);

            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("\"ok\": true", StringComparison.OrdinalIgnoreCase));
            AssertEqual(8, calls.Count); // 4 scenarios x (1 warmup + 1 measured)
            AssertTrue(calls[0].Contains("Do one", StringComparison.OrdinalIgnoreCase) ||
                       calls[0].Contains("Do two", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestReportIncludesComparisonSectionAsync(CancellationToken cancellationToken)
    {
        var repoRoot = CreateTempRepoRoot();
        var previousCurrent = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = repoRoot;
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "baseline",
                ScenarioId = "request-a::composition-a::profile-a::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-a",
                Iteration = 1,
                Success = true,
                Score = 100,
                ElapsedMs = 100,
                BenchmarkSet = "workflow-lab"
            });
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "candidate",
                ScenarioId = "request-a::composition-a::profile-a::iter-1",
                RequestId = "request-a",
                CompositionId = "composition-a",
                ModelProfileId = "profile-a",
                Iteration = 1,
                Success = true,
                Score = 97,
                ElapsedMs = 130,
                BenchmarkSet = "workflow-lab"
            });

            var reportPath = Path.Combine(repoRoot, "workflow_compare.md");
            var command = CreateCommand();
            var (exitCode, output) = await CaptureConsoleAsync(
                () => command.ExecuteReportAsync(
                    repoRoot,
                    limit: 50,
                    benchmarkSet: "workflow-lab",
                    runId: "candidate",
                    baselineRunId: "baseline",
                    since: null,
                    outputPath: reportPath,
                    json: false)).ConfigureAwait(false);

            AssertEqual(0, exitCode);
            AssertTrue(output.Contains("comparison=candidate vs baseline", StringComparison.OrdinalIgnoreCase));
            var markdown = await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(false);
            AssertTrue(markdown.Contains("## Comparison", StringComparison.OrdinalIgnoreCase));
            AssertTrue(markdown.Contains("Candidate run: `candidate`", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrent;
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestGatePassesAndFailsWithThresholdsAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "baseline",
                ScenarioId = "req::comp::profile::iter-1",
                RequestId = "req",
                CompositionId = "comp",
                ModelProfileId = "profile",
                Iteration = 1,
                Success = true,
                Score = 100,
                ElapsedMs = 100,
                BenchmarkSet = "workflow-lab"
            });
            WorkflowLabHistoryStore.Append(repoRoot, new WorkflowLabStressHistoryRow
            {
                RunId = "candidate",
                ScenarioId = "req::comp::profile::iter-1",
                RequestId = "req",
                CompositionId = "comp",
                ModelProfileId = "profile",
                Iteration = 1,
                Success = false,
                Score = 70,
                ElapsedMs = 500,
                BenchmarkSet = "workflow-lab"
            });

            var command = CreateCommand();
            var (failingExit, failingOutput) = await CaptureConsoleAsync(
                () => command.ExecuteGateAsync(
                    repoRoot,
                    benchmarkSet: "workflow-lab",
                    runId: "candidate",
                    baselineRunId: "baseline",
                    minSuccessRateDelta: -0.05,
                    maxP95LatencyRegressionMs: 100,
                    maxAverageLatencyRegressionMs: 100,
                    minAverageScoreDelta: -10,
                    maxRegressedScenarios: 0,
                    json: false)).ConfigureAwait(false);
            AssertEqual(1, failingExit);
            AssertTrue(failingOutput.Contains("workflow gate: failed", StringComparison.OrdinalIgnoreCase));

            var (passingExit, passingOutput) = await CaptureConsoleAsync(
                () => command.ExecuteGateAsync(
                    repoRoot,
                    benchmarkSet: "workflow-lab",
                    runId: "candidate",
                    baselineRunId: "baseline",
                    minSuccessRateDelta: -1.0,
                    maxP95LatencyRegressionMs: 1000,
                    maxAverageLatencyRegressionMs: 1000,
                    minAverageScoreDelta: -100,
                    maxRegressedScenarios: 10,
                    json: false)).ConfigureAwait(false);
            AssertEqual(0, passingExit);
            AssertTrue(passingOutput.Contains("workflow gate: passed", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }
}
