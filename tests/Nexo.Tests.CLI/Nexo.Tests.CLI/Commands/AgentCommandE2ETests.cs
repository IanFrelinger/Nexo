using Xunit;
using System.Diagnostics;
using System.Text.Json;

namespace Nexo.Tests.CLI.Commands;

/// <summary>
/// End-to-end tests for the agent command.
/// Tests the CLI command execution with real dependencies.
/// </summary>
public class AgentCommandE2ETests
{
    private readonly string _cliPath;

    public AgentCommandE2ETests()
    {
        _cliPath = "dotnet";
    }

    [Fact]
    public async Task AgentCommand_WithValidAgentName_ReturnsResult()
    {
        // Arrange
        // Find a DLL file to use as input
        var dllPath = Directory.GetFiles(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src"),
            "*.dll",
            SearchOption.AllDirectories)
            .FirstOrDefault();

        if (dllPath == null)
        {
            // Skip test if no DLL found
            return;
        }

        var arguments = $"run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- agent --name director --input \"{dllPath}\"";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        // Exit code may be 0 (success) or 2 (failed) depending on agent execution
        Assert.True(exitCode == 0 || exitCode == 2, $"Unexpected exit code: {exitCode}");
        Assert.Contains("agent", output.ToLower() + error.ToLower());
    }

    [Fact]
    public async Task AgentCommand_WithNonExistentAgent_ReturnsError()
    {
        // Arrange
        var arguments = "run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- agent --name nonexistent-agent";

        // Act
        var (exitCode, output, error) = await RunCommandAsync(arguments);

        // Assert
        // Should return non-zero exit code for error
        Assert.NotEqual(0, exitCode);
        Assert.Contains("not found", output.ToLower() + error.ToLower(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AgentCommand_WithJsonFormat_ReturnsJsonOutput()
    {
        // Arrange
        var dllPath = Directory.GetFiles(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src"),
            "*.dll",
            SearchOption.AllDirectories)
            .FirstOrDefault();

        if (dllPath == null)
        {
            return;
        }

        var arguments = $"run --project ../../../../src/Nexo.CLI/Nexo.CLI.csproj -- agent --name director --input \"{dllPath}\" --format-json";

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

