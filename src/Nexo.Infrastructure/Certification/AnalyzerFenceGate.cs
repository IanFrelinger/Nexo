#pragma warning disable NEXOEXP001 // The gate enforces the experimental autonomy contract (touch-set, kernel prefixes) by design; see docs/SdkCompatibilityPolicy.md.
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Analyzers;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// The analyzer fence gate (extension spec Part A §1): runs the trust-loop analyzer catalog
/// over the pristine candidate compilation and converts <c>NEXO*</c> diagnostics at or above
/// the severity floor into a gate verdict.
///
/// Fail-closed per A1.4: a candidate that does not compile, a compilation that cannot resolve
/// the brick anchor types (which would make every brick-scoped rule silently no-op), or an
/// analyzer crash (surfaced by Roslyn as AD0001) is a FAIL, never a skip. The floor is Warning
/// and configurable only upward in strictness (Info/Hidden); attempts to relax it to Error are
/// clamped back to Warning.
/// </summary>
public sealed class AnalyzerFenceGate
{
    /// <summary>Environment variable overriding the severity floor (<c>warning</c> default; <c>info</c>/<c>hidden</c> are stricter).</summary>
    public const string SeverityFloorEnvVar = "NEXO_ANALYZER_SEVERITY_FLOOR";

    private const string DiagnosticIdPrefix = "NEXO";
    private const string AnalyzerCrashDiagnosticId = "AD0001";

    /// <summary>Feedback is deterministically truncated beyond this many findings (A3.3 permits
    /// deterministic truncation; the truncation itself is recorded in the feedback).</summary>
    public const int MaxFindingsInFeedback = 25;

    private readonly ILogger<AnalyzerFenceGate>? _logger;

    /// <summary>Creates the gate.</summary>
    public AnalyzerFenceGate(ILogger<AnalyzerFenceGate>? logger = null) => _logger = logger;

    /// <summary>
    /// The catalog attached to candidate compilations: every analyzer shipped in
    /// <c>Nexo.Analyzers</c>. Explicit construction (not reflection over the assembly) keeps
    /// the attached set deterministic and reviewable; additions here are Tier-1-shaped changes.
    /// </summary>
    public static ImmutableArray<DiagnosticAnalyzer> CatalogAnalyzers() => ImmutableArray.Create<DiagnosticAnalyzer>(
        new BrickInterfaceDriftAnalyzer(),
        new BrickConstructorPurityAnalyzer(),
        new SelfRecursiveRegistrationAnalyzer(),
        new BrickDeterminismAnalyzer(),
        new BrickEmptyCatchAnalyzer());

    /// <summary>
    /// Runs the analyzer catalog — plus, when the request carries a constraint manifest, the
    /// manifest-derived rules (A2), and, when it carries a declared touch-set, the touch-set
    /// reference rules (autonomy spec R3.2 analyzer leg) — over the candidate and produces
    /// the gate verdict.
    /// </summary>
    public async Task<AnalyzerGateOutcome> EvaluateAsync(
        string candidateSource,
        IEnumerable<string>? compilationReferences,
        BrickConstraintManifest? constraintManifest = null,
        Nexo.Core.Application.Autonomy.TouchSet? touchSet = null,
        CancellationToken cancellationToken = default)
    {
        var floor = ResolveSeverityFloor();
        var analyzers = CatalogAnalyzers();
        // A2.3: the analyzer is constructed with the manifest INSTANCE — the same object the
        // proposer's instructions were rendered from — so rules and prompt cannot drift.
        if (constraintManifest is not null)
            analyzers = analyzers.Add(new BrickConstraintManifestAnalyzer(constraintManifest));
        // R3.2/R1.3: the same declaration that classified the tier and derived the mounts is
        // enforced against the candidate's actual reference graph. The kernel enumeration
        // comes from TrustKernel — the analyzer never trusts a caller-supplied kernel list.
        if (touchSet is not null)
        {
            analyzers = analyzers.Add(new TouchSetReferenceAnalyzer(
                touchSet.Normalize().Namespaces,
                Nexo.Core.Application.Autonomy.TrustKernel.KernelNamespacePrefixes));
        }
        var analyzerVersion = typeof(BrickInterfaceDriftAnalyzer).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

        if (analyzers.IsEmpty)
        {
            // Cannot happen by construction, but an empty set where the catalog declares rules
            // is a FAIL, not a silently-green gate (A1.4).
            return AnalyzerGateOutcome.Failed(
                "analyzer set is empty while the catalog declares rules", floor, analyzerVersion);
        }

        try
        {
            var wrapped = CandidateSourceWrapper.Wrap(candidateSource);
            var syntaxTree = CSharpSyntaxTree.ParseText(wrapped, cancellationToken: cancellationToken);

            var references = RoslynCodeAnalysisService.BuildReferenceSet(compilationReferences);
            // The autonomy surface is [Experimental]; a candidate that reaches into it would
            // otherwise fail the compile-error guard below with NEXOEXP001 and never reach the
            // analyzers - which is precisely the kernel-smuggling case the fence exists to NAME
            // (NEXO0014). Suppress the opt-in diagnostic here so the fence judges the candidate on
            // its rules; the certification build outside the fence still enforces it.
            var fenceOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    [Nexo.Core.Application.Autonomy.AutonomyExperimental.DiagnosticId] = ReportDiagnostic.Suppress,
                });
            var compilation = CSharpCompilation.Create(
                "AnalyzerGateCandidate",
                new[] { syntaxTree },
                references,
                fenceOptions);

            // Every brick-scoped rule anchors on the canonical Brick type and silently no-ops
            // without it. In a certification context that silence is fail-open; convert it to an
            // explicit refusal (A1.4). Checked before compile errors because a missing contract
            // reference also breaks the wrap's DomainBrick alias — the reference set is the root
            // cause and the causal message to surface.
            if (compilation.GetTypeByMetadataName(BrickTypeResolution.BrickMetadataName) is null)
            {
                return AnalyzerGateOutcome.Failed(
                    $"analyzer anchor type '{BrickTypeResolution.BrickMetadataName}' is not resolvable from the "
                    + "candidate compilation references; the brick rules would silently no-op",
                    floor,
                    analyzerVersion);
            }

            // A candidate that does not compile yields zero analyzer diagnostics and would read
            // as a pass; fail loudly instead (same argument as the analyzer test harness).
            var compileErrors = compilation.GetDiagnostics(cancellationToken)
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Take(10)
                .ToArray();
            if (compileErrors.Length > 0)
            {
                return AnalyzerGateOutcome.Failed(
                    "candidate does not compile, so analyzer silence would be meaningless: "
                    + string.Join(" | ", compileErrors.Select(e => e.ToString())),
                    floor,
                    analyzerVersion);
            }

            var withAnalyzers = compilation.WithAnalyzers(analyzers);
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

            // Roslyn converts analyzer crashes into AD0001 diagnostics rather than throwing;
            // a crashed analyzer is a FAIL, not a skip (A1.4).
            var crash = diagnostics.FirstOrDefault(d => d.Id == AnalyzerCrashDiagnosticId);
            if (crash is not null)
            {
                return AnalyzerGateOutcome.Failed(
                    $"analyzer crashed during evaluation: {crash.GetMessage()}", floor, analyzerVersion);
            }

            var findings = diagnostics
                .Where(d => d.Id.StartsWith(DiagnosticIdPrefix, StringComparison.Ordinal) && d.Severity >= floor)
                .OrderBy(d => d.Location.SourceSpan.Start)
                .Select(d => ToFinding(d, candidateSource))
                // Manifest and touch-set rules apply to every line of the compilation (unlike
                // the brick-scoped catalog), so they also fire on the wrapper-injected
                // preamble/audit text — code the proposer did not write and cannot repair.
                // Findings there map to candidate line 0 and are structurally exempt.
                // Catalog findings are never dropped.
                .Where(f => f.Line != 0 || !IsWholeCompilationRule(f.Id))
                .ToArray();

            return new AnalyzerGateOutcome
            {
                Passed = findings.Length == 0,
                FailureReason = findings.Length == 0 ? null : "analyzer diagnostics at or above the severity floor",
                Findings = findings,
                DiagnosticsEvaluated = diagnostics.Length,
                AnalyzerAssemblyVersion = analyzerVersion,
                SeverityFloor = floor.ToString(),
                ManifestRulesAttached = constraintManifest is not null,
                TouchSetRulesAttached = touchSet is not null,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Analyzer gate crashed while evaluating a candidate");
            return AnalyzerGateOutcome.Failed($"analyzer gate crashed: {ex.Message}", floor, analyzerVersion);
        }
    }

    private static bool IsWholeCompilationRule(string id) =>
        BrickConstraintManifestAnalyzer.ManifestRuleIds.Contains(id)
        || TouchSetReferenceAnalyzer.TouchSetRuleIds.Contains(id);

    private DiagnosticSeverity ResolveSeverityFloor()
    {
        var configured = Environment.GetEnvironmentVariable(SeverityFloorEnvVar);
        switch (configured?.Trim().ToLowerInvariant())
        {
            case null or "" or "warning":
                return DiagnosticSeverity.Warning;
            case "info":
                return DiagnosticSeverity.Info;
            case "hidden":
                return DiagnosticSeverity.Hidden;
            default:
                // "error" (or garbage) would relax the gate below the spec's Warning floor;
                // the floor is configurable only upward in strictness (A1.3).
                _logger?.LogWarning(
                    "Ignoring {EnvVar}={Value}: the analyzer severity floor is Warning and only configurable upward (info, hidden)",
                    SeverityFloorEnvVar, configured);
                return DiagnosticSeverity.Warning;
        }
    }

    private static AnalyzerFinding ToFinding(Diagnostic diagnostic, string candidateSource)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        return new AnalyzerFinding(
            diagnostic.Id,
            diagnostic.Severity.ToString(),
            diagnostic.GetMessage(),
            CandidateSourceWrapper.MapToCandidateLine(candidateSource, position.Line + 1),
            position.Character + 1);
    }
}

/// <summary>One analyzer diagnostic, located in the candidate's own coordinates.</summary>
/// <param name="Id">Diagnostic id (<c>NEXO*</c>).</param>
/// <param name="Severity">Diagnostic severity name.</param>
/// <param name="Message">The analyzer message, verbatim.</param>
/// <param name="Line">1-based candidate line (0 when the location falls in injected wrapper text).</param>
/// <param name="Column">1-based column.</param>
public sealed record AnalyzerFinding(string Id, string Severity, string Message, int Line, int Column);

/// <summary>Verdict of one analyzer-gate evaluation.</summary>
public sealed record AnalyzerGateOutcome
{
    /// <summary>Whether the candidate passed the gate.</summary>
    public required bool Passed { get; init; }

    /// <summary>Failure category when not passed.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Findings at or above the floor, in source order.</summary>
    public IReadOnlyList<AnalyzerFinding> Findings { get; init; } = Array.Empty<AnalyzerFinding>();

    /// <summary>Total analyzer diagnostics evaluated, including zero (recorded per A1.5).</summary>
    public required int DiagnosticsEvaluated { get; init; }

    /// <summary>Version of the analyzer assembly that produced the verdict (A1.5).</summary>
    public required string AnalyzerAssemblyVersion { get; init; }

    /// <summary>The severity floor the verdict was computed at (A1.5).</summary>
    public required string SeverityFloor { get; init; }

    /// <summary>Whether manifest-derived rules (NEXO0010–0012) were attached for this run (A2).</summary>
    public bool ManifestRulesAttached { get; init; }

    /// <summary>Whether touch-set rules (NEXO0013–0014) were attached for this run (autonomy R3.2).</summary>
    public bool TouchSetRulesAttached { get; init; }

    /// <summary>The certificate's gates_passed configuration string (A1.5).</summary>
    public string GatePassConfiguration =>
        $"analyzerAssemblyVersion={AnalyzerAssemblyVersion};severityFloor={SeverityFloor};diagnosticsEvaluated={DiagnosticsEvaluated}"
        + $";manifestRules={(ManifestRulesAttached ? "attached" : "none")}"
        + $";touchSet={(TouchSetRulesAttached ? "attached" : "none")}";

    /// <summary>Creates a fail-closed outcome for gate-infrastructure failures (A1.4).</summary>
    public static AnalyzerGateOutcome Failed(string reason, DiagnosticSeverity floor, string analyzerVersion) => new()
    {
        Passed = false,
        FailureReason = reason,
        DiagnosticsEvaluated = 0,
        AnalyzerAssemblyVersion = analyzerVersion,
        SeverityFloor = floor.ToString(),
    };

    /// <summary>
    /// Proposer-facing repair feedback (A3.1–A3.3): each finding's message verbatim with its
    /// candidate-relative location; never summarized or re-ranked, deterministically truncated
    /// with the truncation recorded.
    /// </summary>
    public string FormatProposerFeedback()
    {
        if (Passed)
            return "analyzer-gate: PASS";

        if (Findings.Count == 0)
            return $"analyzer-gate: FAIL ({FailureReason})";

        var builder = new StringBuilder();
        builder.Append("analyzer-gate: FAIL — ").Append(Findings.Count).Append(" diagnostic(s):");
        foreach (var finding in Findings.Take(AnalyzerFenceGate.MaxFindingsInFeedback))
        {
            builder.AppendLine();
            builder
                .Append("  [").Append(finding.Id).Append("] line ").Append(finding.Line)
                .Append(", col ").Append(finding.Column).Append(": ").Append(finding.Message);
        }

        if (Findings.Count > AnalyzerFenceGate.MaxFindingsInFeedback)
        {
            builder.AppendLine();
            builder
                .Append("  (deterministically truncated: ")
                .Append(Findings.Count - AnalyzerFenceGate.MaxFindingsInFeedback)
                .Append(" further diagnostic(s) omitted, in source order)");
        }

        return builder.ToString();
    }
}
