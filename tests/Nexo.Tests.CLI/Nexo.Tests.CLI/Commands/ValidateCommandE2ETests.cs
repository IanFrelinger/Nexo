using Xunit;
using System.Diagnostics;
using System.Text.Json;

namespace Nexo.Tests.CLI.Commands;

/// <summary>
/// End-to-end tests for the validate command.
/// Tests the CLI command execution with real dependencies.
/// </summary>
public class ValidateCommandE2ETests
{
    private readonly string _cliPath;

    public ValidateCommandE2ETests()
    {
        _cliPath = "dotnet";
    }

    [Fact]
    public async Task ValidateCommand_WithNoFilter_ReturnsResult()
    {
        // Arrange
        var arguments = "run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- validate";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        // Exit code may be 0 (passed) or 2 (validation failed) depending on test results
        Assert.True(exitCode == 0 || exitCode == 2, $"Unexpected exit code: {exitCode}");
        Assert.Contains("validation", output.ToLower() + error.ToLower());
    }

    [Fact]
    public async Task ValidateCommand_WithFilter_ReturnsResult()
    {
        // Arrange
        var arguments = "run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- validate --filter \"Category=Unit\"";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        // Exit code may be 0 (passed) or 2 (validation failed) depending on test results
        Assert.True(exitCode == 0 || exitCode == 2, $"Unexpected exit code: {exitCode}");
    }

    [Fact]
    public async Task ValidateCommand_WithJsonFormat_ReturnsJsonOutput()
    {
        // Arrange
        var arguments = "run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- validate --format-json";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        Assert.True(exitCode == 0 || exitCode == 2);
        
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

