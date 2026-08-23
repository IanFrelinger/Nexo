using Xunit;

namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// A <see cref="FactAttribute"/> for host-heavy tests that must not run on CI runners
/// (<c>CI=true</c> or <c>GITHUB_ACTIONS=true</c>). On CI the test is reported as <b>Skipped</b> with the
/// reason — instead of returning early and counting as <b>Passed</b>. Locally it runs as a normal fact.
/// </summary>
/// <remarks>Evaluated at discovery time (xunit 2.x has no dynamic skip); see <see cref="OptInFactAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class NotOnCiFactAttribute : FactAttribute
{
    /// <summary>Creates a fact that is skipped when running under CI.</summary>
    /// <param name="reason">Why the test is local-only (for example "spawns nested dotnet test hosts").</param>
    public NotOnCiFactAttribute(string reason)
    {
        Reason = reason;
        if (IsCi())
        {
            Skip = $"Not run on CI (CI/GITHUB_ACTIONS=true): {reason}";
        }
    }

    /// <summary>Why the test is local-only.</summary>
    public string Reason { get; }

    /// <summary>True when <c>GITHUB_ACTIONS</c> or <c>CI</c> is <c>true</c>.</summary>
    /// <returns>Whether the current process runs on a CI runner.</returns>
    public static bool IsCi() =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}
