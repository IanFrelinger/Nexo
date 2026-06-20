using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;
using Nexo.Spike.S2;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Adversary.Llm;
using Nexo.Spike.S2.Oracle;
using Xunit;

namespace Nexo.Tests.Spike.S2;

public sealed class AdversaryPromptOracleIsolationTests
{
    [Fact]
    public async Task Assembled_adversary_prompt_never_contains_reference_oracle_content()
    {
        var oraclePath = FindRepoFile("artifacts/s2/reference-oracle.json");
        var oracle = await ReferenceOracleLoader.LoadAsync(oraclePath);
        var intent = await BrickSpecLoader.LoadAsync(
            FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json")));
        var role = AdversaryAccessPolicy.CreateImplementerView(intent);

        foreach (var context in SampleContexts(intent, role))
        {
            var combined = AdversaryPromptAssembler.SystemPrompt
                           + AdversaryPromptAssembler.BuildUserPrompt(context);

            combined.Should().NotContain("reference-oracle");
            combined.Should().NotContain("artifacts/s2/reference-oracle");
            combined.Should().NotContain(oracle.Provenance);

            foreach (var heldOut in oracle.HeldOutLabels)
            {
                var serialized = ReferenceOracleLoader.SerializeInputKey(heldOut.Inputs);
                combined.Should().NotContain(
                    serialized,
                    because: $"held-out oracle input {serialized} must not appear in adversary prompt");
            }
        }
    }

    private static IEnumerable<AdaptiveAttemptContext> SampleContexts(BrickSpec intent, RoleView role)
    {
        yield return new AdaptiveAttemptContext(intent.IntentId, intent, role, [], 0, 3);

        yield return new AdaptiveAttemptContext(
            intent.IntentId,
            intent,
            role,
            [
                new GateVerdict(
                    0,
                    "mock-rejected",
                    AdaptiveOutcomeKind.Rejected,
                    "PropertyGate",
                    "acceptance criterion failed for [\"1\",\"2\",\"3\"] => Integer",
                    null,
                    null)
            ],
            1,
            3);

        yield return new AdaptiveAttemptContext(
            intent.IntentId,
            intent,
            role,
            [
                new GateVerdict(
                    0,
                    "mock-rejected",
                    AdaptiveOutcomeKind.Rejected,
                    "MutationGate",
                    "mutation score below threshold",
                    ["Build", "RED", "PropertyGate"],
                    null),
                new GateVerdict(
                    1,
                    "mock-escape",
                    AdaptiveOutcomeKind.TrueEscape,
                    null,
                    "passed all gates",
                    ["Build", "RED", "PropertyGate", "MutationGate"],
                    null)
            ],
            2,
            3);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }
}

public sealed class LlmTransportGuardTests
{
    [Fact]
    public void Real_transport_fails_fast_without_api_key_and_never_leaks_key_value()
    {
        var priorOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var priorAnthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        var priorGeneric = Environment.GetEnvironmentVariable("NEXO_LLM_API_KEY");
        var priorModel = Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXO_LLM_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", "gpt-4o");

            var transport = new HttpLlmTransport();
            var request = new LlmCompletionRequest("gpt-4o", "system", "user", 0, 42);

            Action act = () => transport.CompleteAsync(request).GetAwaiter().GetResult();
            var ex = act.Should().Throw<InvalidOperationException>()
                .Which;
            ex.Message.Should().Contain("API key is not set");
            if (!string.IsNullOrEmpty(priorOpenAi))
                ex.Message.Should().NotContain(priorOpenAi);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", priorOpenAi);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", priorAnthropic);
            Environment.SetEnvironmentVariable("NEXO_LLM_API_KEY", priorGeneric);
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", priorModel);
        }
    }

    [Fact]
    public void Real_transport_fails_fast_without_model_id()
    {
        var priorOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var priorModel = Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key-not-used-no-network");
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", null);

            var transport = new HttpLlmTransport();
            var request = new LlmCompletionRequest("", "system", "user", null, null);

            var act = () => transport.CompleteAsync(request).GetAwaiter().GetResult();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*model id is not set*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", priorOpenAi);
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", priorModel);
        }
    }
}

public sealed class LlmAdversaryFakeTransportTests
{
    [Fact]
    public async Task Llm_adversary_with_fake_transport_runs_harness_offline()
    {
        var transport = new FakeLlmTransport(
        [
            FakeLlmTransport.WrapAsModelResponse(MockCandidates.RejectedConstantReturn),
            FakeLlmTransport.WrapAsModelResponse(MockCandidates.TrueEscapeHeldOut999),
            FakeLlmTransport.WrapAsModelResponse(MockCandidates.BenignPassHonest)
        ]);

        var adversary = new LlmAdversary(transport);
        var output = Path.Combine(Path.GetTempPath(), $"nexo-s2-llm-fake-{Guid.NewGuid():N}");
        var harness = new AdaptiveEscapeHarness();

        var report = await harness.RunAsync(new AdaptiveEscapeHarnessOptions
        {
            Intents = 1,
            AttemptsPerIntent = 3,
            OutputDirectory = output,
            IntentPath = FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json")),
            ReferenceOraclePath = FindRepoFile("artifacts/s2/reference-oracle.json"),
            RequireMutation = false,
            Adversary = adversary
        });

        report.AdversaryBackend.Should().Be(AdaptiveAdversaryFactory.LlmMode);
        report.NonVacuityProven.Should().BeTrue();
        report.TrueEscapeCount.Should().BeGreaterThan(0);
        report.BenignPassCount.Should().BeGreaterThan(0);
        transport.CallCount.Should().Be(3);
    }

    [Fact]
    public void GenerateNext_parses_fake_transport_script_into_candidate()
    {
        var transport = new FakeLlmTransport([FakeLlmTransport.WrapAsModelResponse(MockCandidates.BenignPassHonest)]);
        var adversary = new LlmAdversary(transport);
        var intent = new BrickSpec
        {
            IntentId = "honest-csv-inferrer",
            Title = "t",
            Description = "d",
            Inputs = [],
            Outputs = [],
            Invariants = [],
            AcceptanceCriteria = ["[\"1\",\"2\",\"3\"] => Integer"],
            Adversarial = false
        };

        var candidate = adversary.GenerateNext(new AdaptiveAttemptContext(
            intent.IntentId,
            intent,
            AdversaryAccessPolicy.CreateImplementerView(intent),
            [],
            0,
            1));

        candidate.ImplementationSource.Should().Contain("ColumnTypeInferrer");
        candidate.CandidateId.Should().StartWith("llm-attempt-");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }
}
