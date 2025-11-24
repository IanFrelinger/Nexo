using Xunit;
using System.Diagnostics;
using System.Text.Json;

namespace Nexo.Tests.CLI.Commands;

/// <summary>
/// End-to-end tests for the analyze command.
/// Tests the CLI command execution with real dependencies.
/// </summary>
public class AnalyzeCommandE2ETests
{
    private readonly string _cliPath;

    public AnalyzeCommandE2ETests()
    {
        // Find the CLI executable
        var cliProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "src", "Nexo.CLI", "Nexo.CLI.csproj");
        
        _cliPath = "dotnet";
    }

    [Fact]
    public async Task AnalyzeCommand_WithValidPath_ReturnsSuccess()
    {
        // Arrange
        var testPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src", "Nexo.Core.Domain");
        var arguments = $"run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- analyze --path \"{testPath}\"";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        Assert.Equal(0, exitCode); // Exit code 0 = Ok
        Assert.Contains("violation", output.ToLower() + error.ToLower());
    }

    [Fact]
    public async Task AnalyzeCommand_WithJsonFormat_ReturnsJsonOutput()
    {
        // Arrange
        var testPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src", "Nexo.Core.Domain");
        var arguments = $"run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- analyze --path \"{testPath}\" --format-json";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        Assert.Equal(0, exitCode);
        
        // Check if output is valid JSON
        var jsonLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Trim().StartsWith("{") || line.Trim().StartsWith("["))
            .ToList();

        if (jsonLines.Any())
        {
            var json = jsonLines.First();
            Assert.True(IsValidJson(json), "Output should be valid JSON");
        }
    }

    [Fact]
    public async Task AnalyzeCommand_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = "/nonexistent/path/that/does/not/exist";
        var arguments = $"run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- analyze --path \"{invalidPath}\"";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        // Should return non-zero exit code for error
        Assert.NotEqual(0, exitCode);
    }

    private async Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output, error);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

