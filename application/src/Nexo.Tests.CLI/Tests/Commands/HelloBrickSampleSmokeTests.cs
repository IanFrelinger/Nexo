using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

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
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Nexo.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
