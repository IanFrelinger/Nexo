using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using FluentAssertions;
using Nexo.CLI.Commands;
using Nexo.CLI.Runtime;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

[Trait("Category", "E2E")]
public sealed class ForgeRelatedCommandTests
{
    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_HasApplyTuneSubcommand()
    {
        var cmd = new RuntimeStudioCommand();
        var subcommands = cmd.Subcommands.Select(s => s.Name).ToList();
        subcommands.Should().Contain("apply-tune");
    }

    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_HasStatusSubcommand()
    {
        var cmd = new RuntimeStudioCommand();
        var subcommands = cmd.Subcommands.Select(s => s.Name).ToList();
        subcommands.Should().Contain("status");
    }

    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_ApplyTune_HasExpectedOptions()
    {
        var cmd = new RuntimeStudioCommand();
        var applyTune = cmd.Subcommands.Single(s => s.Name == "apply-tune");
        var optionNames = applyTune.Options.Select(o => o.Name).ToList();

        optionNames.Should().Contain("format-json");
        optionNames.Should().Contain("spec");
        optionNames.Should().Contain("agent-set");
        optionNames.Should().Contain("dry-run");
    }

    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_Status_HasExpectedOptions()
    {
        var cmd = new RuntimeStudioCommand();
        var status = cmd.Subcommands.Single(s => s.Name == "status");
        var optionNames = status.Options.Select(o => o.Name).ToList();

        optionNames.Should().Contain("format-json");
        optionNames.Should().Contain("agent-set");
    }

    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_ParsesApplyTuneWithDryRun()
    {
        var cmd = new RuntimeStudioCommand();
        var parseResult = cmd.Parse("apply-tune --dry-run --format-json");

        parseResult.Errors.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void RuntimeStudioCommand_ParsesStatusWithJson()
    {
        var cmd = new RuntimeStudioCommand();
        var parseResult = cmd.Parse("status --format-json");

        parseResult.Errors.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void TuneApplier_Apply_RejectsFailedOptimizePayload()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var payload = new WorkflowOptimizeLastPayload
            {
                WrittenAtUtc = DateTimeOffset.UtcNow,
                OptimizeRunId = "run-fail",
                Ok = false,
                OllamaModels = new[] { "llama3.1:latest" }
            };

            var result = RuntimeStudioTuneApplier.Apply(tempDir, "spec.json", "agents.json", payload, dryRun: false);

            result.Ok.Should().BeFalse();
            result.Summary.Should().Contain("Ok=false");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact(Timeout = 15000)]
    public void TuneApplier_Apply_RejectsMissingModelProfileId()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var payload = new WorkflowOptimizeLastPayload
            {
                WrittenAtUtc = DateTimeOffset.UtcNow,
                OptimizeRunId = "run-1",
                Ok = true,
                ModelProfileId = null,
                CompositionId = "hierarchy-squad",
                OllamaModels = new[] { "llama3.1:latest" }
            };

            var result = RuntimeStudioTuneApplier.Apply(tempDir, "spec.json", "agents.json", payload, dryRun: false);

            result.Ok.Should().BeFalse();
            result.Summary.Should().Contain("modelProfileId");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact(Timeout = 15000)]
    public void TuneApplier_ResolveOllamaModelForLabAgent_ReturnsMixedProfileModel()
    {
        var spec = WorkflowLabRuntimeSpec.Default();
        var composition = spec.Compositions.First(c => c.Id == "hierarchy-squad");
        var profile = spec.ModelProfiles.First(p => p.Id == "ollama-mixed");

        var plannerModel = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "planner-1");
        var builderModel = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "builder-1");
        var qaModel = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "qa-1");

        plannerModel.Should().Be("qwen2.5:7b");
        builderModel.Should().Be("codellama:13b");
        qaModel.Should().Be("mistral:7b");
    }

    [Fact(Timeout = 15000)]
    public void TuneApplier_ResolveOllamaModelForLabAgent_FallsBackToDefault()
    {
        var spec = WorkflowLabRuntimeSpec.Default();
        var composition = spec.Compositions.First(c => c.Id == "hierarchy-squad");
        var profile = spec.ModelProfiles.First(p => p.Id == "ollama-balanced");

        var model = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "planner-1");

        model.Should().Be("llama3.1:latest");
    }

    [Fact(Timeout = 15000)]
    public void AgentSetReader_ReturnsEmpty_ForMissingFile()
    {
        var rows = RuntimeStudioAgentSetReader.TryListOllamaAgents("/nonexistent/path/agents.json");
        rows.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void AgentSetReader_ParsesOllamaAgents_SortedById()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var agentSetPath = Path.Combine(tempDir, "agents.json");
            File.WriteAllText(agentSetPath, """
            {
              "BackgroundAgents": {
                "Agents": [
                  { "Id": "beta", "ModelProvider": "ollama", "ModelName": "llama3" },
                  { "Id": "alpha", "ModelProvider": "ollama", "ModelName": "qwen2" },
                  { "Id": "gamma", "ModelProvider": "deterministic" }
                ]
              }
            }
            """);

            var rows = RuntimeStudioAgentSetReader.TryListOllamaAgents(agentSetPath);

            rows.Should().HaveCount(2);
            rows[0].Id.Should().Be("alpha");
            rows[0].ModelName.Should().Be("qwen2");
            rows[1].Id.Should().Be("beta");
            rows[1].ModelName.Should().Be("llama3");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact(Timeout = 90000)]
    public void TuneApplier_Apply_DryRun_ReportsPlannedChanges()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var specPath = Path.Combine(tempDir, "lab.json");
            var specJson = JsonSerializer.Serialize(
                WorkflowLabRuntimeSpec.Default(),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
            File.WriteAllText(specPath, specJson);

            var agentSetPath = Path.Combine(tempDir, "agent_set.json");
            File.WriteAllText(agentSetPath, """
            {
              "BackgroundAgents": {
                "Agents": [
                  { "Id": "runtime-planner", "ModelProvider": "ollama", "ModelName": "old-model" },
                  { "Id": "runtime-worker-optimizer", "ModelProvider": "ollama", "ModelName": "old-model" }
                ]
              }
            }
            """);

            var payload = new WorkflowOptimizeLastPayload
            {
                WrittenAtUtc = DateTimeOffset.UtcNow,
                OptimizeRunId = "run-1",
                Ok = true,
                WinnerCandidateId = "c1",
                WinnerRunId = "r1",
                ModelProfileId = "ollama-balanced",
                CompositionId = "hierarchy-squad",
                RequestId = "fullstack-feature",
                OllamaModels = new[] { "llama3.1:latest" }
            };

            var result = RuntimeStudioTuneApplier.Apply(tempDir, specPath, agentSetPath, payload, dryRun: true);

            result.Ok.Should().BeTrue();
            result.Summary.Should().Contain("Dry run");
            result.UpdatedAgentIds.Should().NotBeNullOrEmpty();

            var fileContents = File.ReadAllText(agentSetPath);
            fileContents.Should().Contain("old-model", "dry run should not modify the file");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact(Timeout = 15000)]
    public void WorkflowLabRuntimeSpec_Default_HasExpectedCompositions()
    {
        var spec = WorkflowLabRuntimeSpec.Default();

        spec.Compositions.Should().HaveCount(2);
        spec.Compositions.Select(c => c.Id).Should().Contain(new[] { "single-specialist", "hierarchy-squad" });
        spec.ModelProfiles.Should().HaveCount(2);
        spec.Requests.Should().HaveCount(2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-forge-cli-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }
}
