using Xunit;

namespace Nexo.Bricks.DepExtract.Gui.Tests;

/// <summary>
/// A theory that needs a live local stack (container engine + parser service) to be
/// meaningful. Opt in explicitly with <c>NEXO_E2E_LIVE_STACK=1</c>; otherwise it
/// skips.
///
/// Deliberately an explicit opt-in rather than a probe: a probe that guesses wrong
/// turns an environment gap into a red test, which is exactly the false signal this
/// suite already suffered from while it was untracked.
/// </summary>
public sealed class LiveStackTheoryAttribute : TheoryAttribute
{
    /// <summary>Environment variable that enables live-stack tests.</summary>
    public const string EnableVariable = "NEXO_E2E_LIVE_STACK";

    /// <summary>Creates the attribute, skipping unless the stack is opted in.</summary>
    public LiveStackTheoryAttribute()
    {
        if (!Enabled)
            Skip = $"requires a live local stack; set {EnableVariable}=1 to run";
    }

    /// <summary>True when live-stack tests are enabled.</summary>
    public static bool Enabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnableVariable),
            "1",
            StringComparison.Ordinal);
}
