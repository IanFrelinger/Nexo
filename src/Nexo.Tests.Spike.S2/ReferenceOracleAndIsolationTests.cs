using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Oracle;
using Xunit;

namespace Nexo.Tests.Spike.S2;

public sealed class ReferenceOracleSupersetTests
{
    [Fact]
    public async Task Reference_oracle_is_strict_superset_of_gate_pinned_acceptance()
    {
        var oracle = await ReferenceOracleLoader.LoadAsync(FindRepoFile("artifacts/s2/reference-oracle.json"));
        var intent = await BrickSpecLoader.LoadAsync(
            FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json")));

        var act = () => ReferenceOracleLoader.AssertStrictSuperset(oracle, intent);
        act.Should().NotThrow();

        oracle.GatePinnedLabels.Should().NotBeEmpty();
        oracle.HeldOutLabels.Should().NotBeEmpty();

        var gateKeys = ReferenceOracleLoader.ExtractGatePinnedInputKeys(intent);
        var heldOutOnly = oracle.HeldOutLabels
            .Where(l => !gateKeys.Contains(ReferenceOracleLoader.SerializeInputKey(l.Inputs)))
            .ToList();

        heldOutOnly.Should().NotBeEmpty(because: "true escapes must be structurally possible");
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

public sealed class AdversaryIsolationTests
{
    [Fact]
    public void Adversary_implementer_role_cannot_edit_tests_or_properties()
    {
        var spec = new BrickSpec
        {
            IntentId = "honest-csv-inferrer",
            Title = "title",
            Description = "desc",
            Inputs = [],
            Outputs = [],
            Invariants = [],
            AcceptanceCriteria = ["[\"1\",\"2\",\"3\"] => Integer"],
            Adversarial = false
        };

        var role = AdversaryAccessPolicy.CreateImplementerView(spec);
        role.CanEditImplementation.Should().BeTrue();
        role.CanEditTests.Should().BeFalse();
        role.VisibleImplementerPrompt.Should().BeTrue();
        role.VisibleTestAuthorPrompt.Should().BeFalse();
    }

    [Fact]
    public void Adversary_context_must_not_include_reference_oracle_path()
    {
        var spec = new BrickSpec
        {
            IntentId = "honest-csv-inferrer",
            Title = "t",
            Description = "d",
            Inputs = [],
            Outputs = [],
            Invariants = [],
            AcceptanceCriteria = [],
            Adversarial = false
        };
        var role = AdversaryAccessPolicy.CreateImplementerView(spec);
        var context = new AdaptiveAttemptContext(
            spec.IntentId,
            spec,
            role,
            [],
            0,
            3);

        AdversaryAccessPolicy.EnsureImplementerOnly(context);
        AdversaryAccessPolicy.AssertAdversaryCannotReadOracle();

        var oraclePath = AdversaryAccessPolicy.ResolveReferenceOraclePath();
        context.VisibleSpec.IntentId.Should().NotContain("reference-oracle");
        Path.GetFullPath(oraclePath).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Prior_verdicts_passed_to_adversary_strip_oracle_disagreements()
    {
        var spec = new BrickSpec
        {
            IntentId = "honest-csv-inferrer",
            Title = "t",
            Description = "d",
            Inputs = [],
            Outputs = [],
            Invariants = [],
            AcceptanceCriteria = [],
            Adversarial = false
        };
        var role = AdversaryAccessPolicy.CreateImplementerView(spec);

        var leaked = new GateVerdict(
            0,
            "candidate",
            AdaptiveOutcomeKind.TrueEscape,
            null,
            "passed",
            ["PropertyGate"],
            [new OracleDisagreement(["999"], "Integer", "String")]);

        var context = new AdaptiveAttemptContext(spec.IntentId, spec, role, [leaked with { OracleDisagreements = null }], 1, 3);
        AdversaryAccessPolicy.ContainsOracleLeak(context).Should().BeFalse();
    }
}

public sealed class LlmAdversaryGuardTests
{
    [Fact]
    public void Factory_defaults_to_mock_without_llm_env()
    {
        var prior = Environment.GetEnvironmentVariable("NEXO_S2_ADVERSARY");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_S2_ADVERSARY", null);
            var adversary = AdaptiveAdversaryFactory.Create();
            adversary.BackendName.Should().Be(AdaptiveAdversaryFactory.MockMode);
            adversary.Should().BeOfType<DeterministicMockAdversary>();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_S2_ADVERSARY", prior);
        }
    }

    [Fact]
    public void Factory_with_llm_mode_constructs_without_api_key()
    {
        var priorMode = Environment.GetEnvironmentVariable("NEXO_S2_ADVERSARY");
        var priorOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_S2_ADVERSARY", AdaptiveAdversaryFactory.LlmMode);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXO_LLM_API_KEY", null);

            var adversary = AdaptiveAdversaryFactory.Create();
            adversary.Should().BeOfType<LlmAdversary>();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_S2_ADVERSARY", priorMode);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", priorOpenAi);
        }
    }

    [Fact]
    public void Ci_environment_must_not_select_llm_adversary()
    {
        var ci = Environment.GetEnvironmentVariable("CI");
        var gh = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        var mode = Environment.GetEnvironmentVariable("NEXO_S2_ADVERSARY") ?? AdaptiveAdversaryFactory.MockMode;

        if (string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gh, "true", StringComparison.OrdinalIgnoreCase))
        {
            mode.Should().NotBe(AdaptiveAdversaryFactory.LlmMode);
        }
    }
}
