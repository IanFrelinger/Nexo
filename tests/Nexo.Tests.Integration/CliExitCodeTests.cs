using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Tests.Integration;

[Trait("Category", "Integration")]
public class CliExitCodeTests
{
    [Fact(DisplayName = "nexo analyze returns non-zero on policy violation")]
    public async Task Analyze_PolicyViolation_NonZeroExit()
    {
        // Arrange: point to an invalid path or intentionally trigger a violation flag
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            // Example: run the CLI project directly; if installed as a tool, set FileName="nexo"
            Arguments = "run --project src/Nexo.CLI/Nexo.CLI.csproj -- analyze --path ./../../not-allowed",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Act
        using var proc = Process.Start(psi)!;
        string stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        // Assert
        Assert.NotEqual(0, proc.ExitCode);
        Assert.Contains("Policy", stderr, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "nexo analyze returns zero on successful analysis")]
    public async Task Analyze_ValidPath_ZeroExit()
    {
        // Arrange: point to a valid path (current directory)
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project src/Nexo.CLI/Nexo.CLI.csproj -- analyze --path .",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Act
        using var proc = Process.Start(psi)!;
        string stdout = await proc.StandardOutput.ReadToEndAsync();
        string stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        // Assert
        Assert.Equal(0, proc.ExitCode);
        // Should have some output indicating successful analysis
        Assert.True(!string.IsNullOrEmpty(stdout) || !string.IsNullOrEmpty(stderr));
    }
}
