using FluentAssertions;
using Nexo.Analyzers;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Analyzer fence gate (extension spec A1): verdict rule, fail-closed behavior, the
/// upward-only severity floor, and A3-conformant proposer feedback.
/// </summary>
public sealed class AnalyzerFenceGateTests
{
    private static readonly string[] ContractReferences =
    {
        typeof(Nexo.Core.Domain.Bricks.Brick).Assembly.Location,
    };

    private static AnalyzerFenceGate Gate() => new();

    private const string DirtyBrickSource = """
        using Nexo.Core.Domain.Bricks;
        using Nexo.Core.Domain.Execution;

        namespace Nexo.Certified.AnalyzerProbe;

        public sealed class AnalyzerProbeBrick : DomainBrick
        {
            public AnalyzerProbeBrick()
            {
                Id = "analyzer-probe-brick";
                Name = "Analyzer Probe";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "Deliberately reads the wall clock.";
                Interface = new BrickInterface
                {
                    Inputs = [],
                    Outputs = [new BrickOutputDefinition("stamp", "string", "stamp")]
                };
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input, ImplementationType implementation,
                IExecutionContext context, CancellationToken cancellationToken = default)
            {
                var output = new BrickOutput();
                output.Set("stamp", DateTime.Now.ToString());
                return Task.FromResult(output);
            }
        }
        """;

    [Fact]
    public async Task Clean_candidate_passes_and_records_evaluation_metadata()
    {
        var outcome = await Gate().EvaluateAsync(MutationProbeBrickSource.Code, ContractReferences);

        outcome.Passed.Should().BeTrue(outcome.FormatProposerFeedback());
        outcome.SeverityFloor.Should().Be("Warning");
        outcome.AnalyzerAssemblyVersion.Should().NotBeNullOrWhiteSpace();
        outcome.GatePassConfiguration.Should()
            .Contain("analyzerAssemblyVersion=").And.Contain("severityFloor=Warning").And.Contain("diagnosticsEvaluated=");
    }

    [Fact]
    public async Task Wall_clock_candidate_fails_with_a_located_verbatim_finding()
    {
        var outcome = await Gate().EvaluateAsync(DirtyBrickSource, ContractReferences);

        outcome.Passed.Should().BeFalse();
        var finding = outcome.Findings.Should().ContainSingle().Subject;
        finding.Id.Should().Be("NEXO0006");
        finding.Message.Should().Contain("DateTime.Now").And.Contain("DateTime.UtcNow");
        finding.Line.Should().BeGreaterThan(0, "the location must map back to the candidate's own coordinates");

        // A3.2: feedback carries the message verbatim plus the source location.
        var feedback = outcome.FormatProposerFeedback();
        feedback.Should().Contain("NEXO0006").And.Contain(finding.Message).And.Contain($"line {finding.Line}");
    }

    [Fact]
    public async Task Non_compiling_candidate_fails_closed()
    {
        var outcome = await Gate().EvaluateAsync("public sealed class Broken {", ContractReferences);

        outcome.Passed.Should().BeFalse();
        outcome.FailureReason.Should().Contain("does not compile");
    }

    [Fact]
    public async Task Missing_brick_anchor_references_fail_closed()
    {
        // No contract references: every brick-scoped rule would silently no-op, which in a
        // certification context is fail-open. The gate must refuse instead (A1.4).
        var outcome = await Gate().EvaluateAsync(
            "public sealed class NotABrick { }",
            compilationReferences: null);

        outcome.Passed.Should().BeFalse();
        outcome.FailureReason.Should().Contain("Nexo.Core.Domain.Bricks.Brick");
    }

    [Fact]
    public async Task Severity_floor_cannot_be_relaxed_below_warning()
    {
        Environment.SetEnvironmentVariable(AnalyzerFenceGate.SeverityFloorEnvVar, "error");
        try
        {
            var outcome = await Gate().EvaluateAsync(DirtyBrickSource, ContractReferences);

            outcome.SeverityFloor.Should().Be("Warning", "the floor is configurable only upward in strictness (A1.3)");
            outcome.Passed.Should().BeFalse("the warning-severity finding must still fail the gate");
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnalyzerFenceGate.SeverityFloorEnvVar, null);
        }
    }

    [Fact]
    public async Task Nondeterministic_fixture_stays_invisible_to_the_static_catalog()
    {
        // The determinism-teeth fixture must be nondeterministic in a way the honest analyzer
        // deliberately does not flag (Guid.NewGuid), so the runtime determinism gate — not the
        // analyzer fence — is what rejects it. If this fails, the fixture regressed into a
        // statically-nameable pattern and the teeth test silently stopped testing its gate.
        var outcome = await Gate().EvaluateAsync(NondeterministicBrickSource.Code, ContractReferences);

        outcome.Passed.Should().BeTrue(outcome.FormatProposerFeedback());
    }

    [Fact]
    public void Catalog_is_never_empty()
    {
        AnalyzerFenceGate.CatalogAnalyzers().Should().NotBeEmpty(
            "an empty analyzer set where the catalog declares rules must be impossible by construction");
    }

    // --- manifest-derived rules (extension spec A2) --------------------------------------

    [Fact]
    public async Task Manifest_rules_run_alongside_the_catalog_and_restate_the_instruction_verbatim()
    {
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "DateTime.Now" } };

        var outcome = await Gate().EvaluateAsync(DirtyBrickSource, ContractReferences, manifest);

        outcome.Passed.Should().BeFalse();
        outcome.GatePassConfiguration.Should().Contain("manifestRules=attached");
        outcome.Findings.Should().Contain(f => f.Id == "NEXO0006", "the static catalog still runs");
        var manifestFinding = outcome.Findings.Should()
            .ContainSingle(f => f.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId).Subject;
        manifestFinding.Message.Should().Contain(manifest.ForbiddenApiInstruction("DateTime.Now"),
            "the finding must restate the instruction verbatim from the same manifest instance (A2.3/A3.2)");
        manifestFinding.Line.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Candidate_text_inside_a_forbidden_namespace_fails_the_gate()
    {
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.Collections" } };

        var outcome = await Gate().EvaluateAsync(MutationProbeBrickSource.Code, ContractReferences, manifest);

        outcome.Passed.Should().BeFalse("the candidate creates and uses a List<string>, which lives under System.Collections");
        var findings = outcome.Findings
            .Where(f => f.Id == BrickConstraintManifestAnalyzer.ForbiddenNamespaceId).ToArray();
        findings.Should().NotBeEmpty();
        findings.Should().OnlyContain(f => f.Line > 0,
            "every finding must point at the candidate's own collection operations, never at wrapper text");
    }

    [Fact]
    public async Task Wrapper_injected_text_is_exempt_from_manifest_rules()
    {
        // The certification wrap prepends `using System.Collections.Generic;` and injects an
        // audit context that creates a Dictionary — text the proposer did not write and cannot
        // repair. With System.Collections forbidden, a candidate whose OWN text never touches
        // collections must fail only for its own defect (the wall clock), never for the wrapper.
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.Collections" } };

        var outcome = await Gate().EvaluateAsync(DirtyBrickSource, ContractReferences, manifest);

        outcome.Passed.Should().BeFalse();
        outcome.Findings.Should().OnlyContain(f => f.Id == "NEXO0006",
            "wrapper-owned preamble/audit text maps to candidate line 0 and is structurally exempt from manifest rules");
    }

    [Fact]
    public async Task Without_a_manifest_the_configuration_records_none()
    {
        var outcome = await Gate().EvaluateAsync(MutationProbeBrickSource.Code, ContractReferences);

        outcome.Passed.Should().BeTrue(outcome.FormatProposerFeedback());
        outcome.GatePassConfiguration.Should().Contain("manifestRules=none",
            "A1.5: whether manifest rules governed the verdict is part of the recorded gate configuration");
    }
}
