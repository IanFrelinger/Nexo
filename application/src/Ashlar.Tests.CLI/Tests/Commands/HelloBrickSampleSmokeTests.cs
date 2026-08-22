using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for hello brick sample smoke.</summary>
[Trait("Category", "CLI")]
public sealed class HelloBrickSampleSmokeTests
{
    [Fact(Timeout = 120_000)]
    public async Task Hello_brick_sample_test_project_passes()
    {
        var repoRoot = FindRepoRoot();
        var sampleTestProject = Path.Combine(repoRoot, "samples", "hello-brick", "HelloBrick.Tests", "HelloBrick.Tests.csproj");
        File.Exists(sampleTestProject).Should().BeTrue();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.ArgumentList.Add("test");
        process.StartInfo.ArgumentList.Add(sampleTestProject);
        process.StartInfo.ArgumentList.Add("--blame-hang-timeout");
        process.StartInfo.ArgumentList.Add("120s");
        process.StartInfo.ArgumentList.Add("--blame-hang-dump-type");
        process.StartInfo.ArgumentList.Add("none");

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0, stdout + Environment.NewLine + stderr);
        stdout.Should().Contain("Passed!");
    }

    private static string FindRepoRoot()
    {
        // Anchored on the test assembly's location rather than the process-global
        // Environment.CurrentDirectory, which other suites in this assembly move.
        // See the note in BrickAuthoringPublicApiSnapshotTests.FindSnapshotRoot.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ashlar.sln")))
                return current.FullName;
            current = current.Parent;
        }

        /// <summary>Invalid operation exception.</summary>
        /// <param name="found."">Found.".</param>
        throw new InvalidOperationException("Repository root not found.");
    }
}
