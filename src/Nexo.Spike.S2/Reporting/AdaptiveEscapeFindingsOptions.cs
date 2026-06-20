namespace Nexo.Spike.S2.Reporting;

public sealed record AdaptiveEscapeFindingsOptions(
    bool IncludeCorrectionNote = false,
    bool IncludeScriptedStandInBanner = false,
    string? RunStatus = null)
{
    public static AdaptiveEscapeFindingsOptions ForCanonical() =>
        new(IncludeCorrectionNote: true);

    public static AdaptiveEscapeFindingsOptions ForRun(string status) =>
        new(RunStatus: status);

    public static AdaptiveEscapeFindingsOptions ForScriptedStandInRun(string status) =>
        new(IncludeScriptedStandInBanner: true, RunStatus: status);
}
