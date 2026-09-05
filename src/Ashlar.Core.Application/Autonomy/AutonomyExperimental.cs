namespace Ashlar.Core.Application.Autonomy;

/// <summary>
/// Diagnostic id and help link shared by every <c>[Experimental]</c> type of the autonomy
/// (self-extension) surface: this namespace, <c>Ashlar.Infrastructure.Certification.HotSwap</c>,
/// <c>Ashlar.Infrastructure.Autonomy</c> and <c>Ashlar.BackgroundAgents.Autonomy</c>. Consumers that
/// opt in suppress <c>ASHLAREXP001</c>; the certification gate itself is not part of this surface.
/// See docs/SdkCompatibilityPolicy.md ("Experimental tier").
/// </summary>
/// <remarks>
/// Deliberately NOT experimental itself: member-level <c>[Experimental]</c> attributes (e.g. on
/// <c>CertificationRequest.TouchSet</c>) bind their arguments in the containing type's scope, so an
/// experimental constant holder would trip the very diagnostic it names.
/// </remarks>
public static class AutonomyExperimental
{
    /// <summary>The compiler diagnostic raised when an experimental autonomy API is used.</summary>
    public const string DiagnosticId = "ASHLAREXP001";

    /// <summary>Help link attached to the diagnostic (the policy section that explains it).</summary>
    public const string UrlFormat = "https://github.com/IanFrelinger/Ashlar/blob/master/docs/SdkCompatibilityPolicy.md#ashlarexp001";
}
