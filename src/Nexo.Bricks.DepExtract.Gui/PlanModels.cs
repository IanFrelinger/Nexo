using Nexo.Bricks.DepExtract;

namespace Nexo.Bricks.DepExtract.Gui;

public sealed record PlanRequest(
    string SrcDir,
    string[] Entries,
    string? ScanDir,
    string[]? IncludeDirs,
    string[]? Defines,
    bool IncludeUpper,
    string? Model,
    int? MaxRetries,
    bool? RequireCompile,
    bool? PreferScaffold,
    int? MaxCompileRetries = null);

public sealed record RevisePlanRequest(
    string PlanId,
    string? Feedback,
    string[]? Must,
    string[]? MustNot);

public sealed record ImplementPlanRequest(
    string PlanId,
    string? Model,
    int? MaxRetries,
    bool? RequireCompile,
    bool? PreferScaffold,
    int? MaxCompileRetries = null,
    /// <summary>Appended to operator guidance (build/extract failure feedback from adapt-cycle).</summary>
    string? ExtraGuidance = null);

public sealed record PlanClassDto(string Name, string[] Methods);

public sealed record PlanRiskDto(string Code, string Severity, string Message);
public sealed record PlanConstraintDto(string Kind, string Text);

public sealed record PlanResponse(
    string PlanId,
    string Status,
    string Title,
    string StructureSummary,
    string AdaptationSummary,
    string[] Steps,
    string Strategy,
    string DiagramMermaid,
    AdaptPlanBuilder.DiagramModel Diagram,
    PlanClassDto[] Classes,
    PlanRiskDto[] Risks,
    PlanConstraintDto[] Constraints,
    string[] Lower,
    string[] Upper,
    string[] External,
    string ManifestText,
    string? DuplicatePath,
    string[] Feedback,
    DateTimeOffset CreatedAt,
    string Hint,
    string? ExportPath);

public sealed record PlanSession(
    string PlanId,
    string SrcDir,
    string[] Entries,
    string? ScanDir,
    string[]? IncludeDirs,
    string[]? Defines,
    bool IncludeUpper,
    bool PreferScaffold,
    bool RequireCompile,
    string? Model,
    int? MaxRetries,
    string OutDir,
    string? DuplicatePath,
    string[] Lower,
    string[] Upper,
    string[] External,
    string ManifestText,
    AdaptPlanBuilder.PlanDraft Draft,
    List<string> Feedback,
    string Status,
    DateTimeOffset CreatedAt,
    int? MaxCompileRetries = null);
