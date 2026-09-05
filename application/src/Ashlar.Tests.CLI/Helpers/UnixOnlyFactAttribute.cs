using Xunit;

namespace Ashlar.Tests.CLI.Helpers;

/// <summary>
/// A <see cref="FactAttribute"/> for claims that can only be STATED on a Unix host, because the
/// fixture itself is Unix-only: a FIFO, a symlink to a character device. Windows has neither on
/// NTFS without a privilege the runner may not hold.
///
/// <para>On Windows the test is reported as <b>Skipped</b> with the reason — instead of returning
/// early and counting as <b>Passed</b>, which is the difference between "not applicable on this
/// host" and "verified on this host". Same shape as Ashlar.Tests.Infrastructure's
/// <c>NotOnCiFactAttribute</c>; evaluated at discovery time, since xunit 2.x has no dynamic skip.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UnixOnlyFactAttribute : FactAttribute
{
    /// <summary>Creates a fact that is skipped on Windows.</summary>
    /// <param name="reason">The Unix-only fixture the test needs (for example "mkfifo").</param>
    public UnixOnlyFactAttribute(string reason)
    {
        Reason = reason;
        if (OperatingSystem.IsWindows())
        {
            Skip = $"Unix-only fixture ({reason}): Windows has no FIFO on NTFS and no unprivileged symlink.";
        }
    }

    /// <summary>The Unix-only fixture this test needs.</summary>
    public string Reason { get; }
}
