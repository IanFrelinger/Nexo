namespace Nexo.Core.Application.Environments;

/// <summary>Severity strings for <see cref="MapVerificationIssue.Severity"/>.</summary>
public static class MapVerificationSeverity
{
    /// <summary>Informational finding that does not block verification.</summary>
    public const string Info = "info";

    /// <summary>Warning that may indicate a defect but does not block core checks.</summary>
    public const string Warning = "warn";

    /// <summary>Blocking defect that fails <see cref="MapVerificationReport.PassedCoreChecks"/>.</summary>
    public const string Error = "error";
}
