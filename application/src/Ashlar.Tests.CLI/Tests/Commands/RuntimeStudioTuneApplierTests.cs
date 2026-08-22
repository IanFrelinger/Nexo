using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for runtime studio tune applier.</summary>
public sealed class RuntimeStudioTuneApplierTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test apply tune_updates ollama model names_from default spec.</summary>
            TestApplyTune_UpdatesOllamaModelNames_FromDefaultSpec();
            /// <summary>Test resolve ollama model for lab agent_mixed profile.</summary>
            TestResolveOllamaModelForLabAgent_MixedProfile();
            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioTuneApplierTests),
                Category = "CLI",
                Passed = true,
                Message = "Runtime Studio tune applier tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioTuneApplierTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioTuneApplierTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestApplyTune_UpdatesOllamaModelNames_FromDefaultSpec()
    {
        var temp = Path.Combine(Path.GetTempPath(), "ashlar-tune-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var specPath = Path.Combine(temp, "lab.json");
            var specJson = JsonSerializer.Serialize(
                WorkflowLabRuntimeSpec.Default(),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
            File.WriteAllText(specPath, specJson);

            var agentPath = Path.Combine(temp, "agent_set.json");
            File.WriteAllText(
                agentPath,
                """
{
  "BackgroundAgents": {
    "Agents": [
      {
        "Id": "runtime-planner",
        "ModelProvider": "ollama",
        "ModelName": "legacy-model"
      },
      {
        "Id": "runtime-worker-optimizer",
        "ModelProvider": "ollama",
        "ModelName": "legacy-model"
      }
    ]
  }
}
""");

            var last = new WorkflowOptimizeLastPayload
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

            var result = RuntimeStudioTuneApplier.Apply(temp, specPath, agentPath, last, dryRun: false);
            /// <summary>Assert true.</summary>
            AssertTrue(result.Ok, result.Summary);
            /// <summary>Assert not null.</summary>
            AssertNotNull(result.UpdatedAgentIds);
            /// <summary>Assert true.</summary>
            /// <param name="1">1.</param>
            /// <param name="update."">Update.".</param>
            AssertTrue(result.UpdatedAgentIds!.Count >= 1, "Expected at least one agent update.");

            var updated = File.ReadAllText(agentPath);
            AssertTrue(updated.Contains("llama3.1:latest", StringComparison.Ordinal), "Expected winner default model in agent set JSON.");
            AssertFalse(updated.Contains("legacy-model", StringComparison.Ordinal), "Expected legacy model to be replaced.");
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch
            {
                // Temp cleanup best-effort.
            }
        }
    }

    private void TestResolveOllamaModelForLabAgent_MixedProfile()
    {
        var spec = WorkflowLabRuntimeSpec.Default();
        var composition = spec.Compositions.First(c => string.Equals(c.Id, "hierarchy-squad", StringComparison.OrdinalIgnoreCase));
        var profile = spec.ModelProfiles.First(p => string.Equals(p.Id, "ollama-mixed", StringComparison.OrdinalIgnoreCase));

        var planner = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "planner-1");
        var builder = RuntimeStudioTuneApplier.ResolveOllamaModelForLabAgent(composition, profile, "builder-1");
        /// <summary>Assert equal.</summary>
        AssertEqual("qwen2.5:7b", planner);
        /// <summary>Assert equal.</summary>
        AssertEqual("codellama:13b", builder);
    }
}
