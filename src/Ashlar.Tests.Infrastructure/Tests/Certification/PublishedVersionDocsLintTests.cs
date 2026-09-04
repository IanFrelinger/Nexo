using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>C6: docs published-version claims key off ci/published-version, never VERSION.</summary>
[Trait("Category", "Certification")]
public sealed class PublishedVersionDocsLintTests
{
    [Fact]
    public void PublishedVersionFile_IsNotTheRepoVersionBumpSource()
    {
        var root = FindRepoRoot();
        var published = File.ReadAllText(Path.Combine(root, "ci", "published-version")).Trim();
        published.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
        File.Exists(Path.Combine(root, "scripts", "verify-docs-published-version.sh")).Should().BeTrue();
    }

    [Fact]
    public void DocsClaimLint_PassesAgainstPublishedVersion()
    {
        var root = FindRepoRoot();
        var script = Path.Combine(root, "scripts", "verify-docs-published-version.sh");
        var psi = new ProcessStartInfo("bash", script)
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("failed to start C6 lint");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, stderr + stdout);
        stdout.Should().Contain("published-version lint ok");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Ashlar.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
