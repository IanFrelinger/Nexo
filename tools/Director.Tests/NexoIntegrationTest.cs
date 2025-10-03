using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Tests;

/// <summary>
/// Test class to validate Nexo integration with Director Studio
/// This class tests the complete workflow from Avalonia to Nexo execution
/// </summary>
public class NexoIntegrationTest
{
    private readonly string _projectRoot;
    private readonly List<TestResult> _testResults = new();

    public NexoIntegrationTest()
    {
        _projectRoot = Directory.GetCurrentDirectory();
    }

    public async Task<bool> RunFullTestAsync()
    {
        Console.WriteLine("Director Studio Nexo Integration Test");
        Console.WriteLine("====================================");

        try
        {
            // Check Nexo availability
            if (!await CheckNexoAvailabilityAsync())
            {
                Console.WriteLine("\n❌ Nexo CLI not available. Cannot run integration tests.");
                return false;
            }

            // Test Nexo commands
            var nexoResults = await TestNexoCommandsAsync();

            // Test Director command generation
            await TestDirectorCommandsAsync();

            // Generate report
            return GenerateTestReport(nexoResults);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nTest failed with error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CheckNexoAvailabilityAsync()
    {
        Console.WriteLine("=== Checking Nexo CLI Availability ===");

        try
        {
            var result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- --version");
            
            if (result.Success)
            {
                LogTest("Nexo CLI Available", true, "Nexo CLI is working", new Dictionary<string, object> { ["version_output"] = result.Output });
                return true;
            }
            else
            {
                LogTest("Nexo CLI Available", false, "Nexo CLI failed to run", new Dictionary<string, object> { ["error"] = result.Error });
                return false;
            }
        }
        catch (Exception ex)
        {
            LogTest("Nexo CLI Available", false, $"Error running Nexo CLI: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<List<TestResult>> TestNexoCommandsAsync()
    {
        Console.WriteLine("\n=== Testing Nexo Commands ===");

        var commands = new object[]
        {
            new { Name = "validate", Args = new[] { "--format-json" }, Description = "Architecture validation" },
            new { Name = "analyze", Args = new[] { "--format-json" }, Description = "Code analysis" },
            new { Name = "agent", Args = new[] { "--name", "list", "--format-json" }, Description = "List agents" }
        };

        var results = new List<TestResult>();

        foreach (var cmd in commands)
        {
            try
            {
                var fullCommand = $"run --project src/Nexo.CLI/Nexo.CLI.csproj -- {cmd.Name} {string.Join(" ", cmd.Args)}";
                var result = await RunCommandAsync("dotnet", fullCommand);

                if (result.Success)
                {
                    // Try to parse JSON output
                    try
                    {
                        var jsonOutput = JsonSerializer.Deserialize<JsonElement>(result.Output);
                        LogTest($"Nexo Command: {cmd.Name}", true, $"{cmd.Description} - JSON output received", 
                            new Dictionary<string, object> { ["json_output"] = jsonOutput });
                    }
                    catch (JsonException)
                    {
                        LogTest($"Nexo Command: {cmd.Name}", false, $"{cmd.Description} - Invalid JSON output", 
                            new Dictionary<string, object> { ["raw_output"] = result.Output });
                    }
                }
                else
                {
                    LogTest($"Nexo Command: {cmd.Name}", false, $"{cmd.Description} - Command failed", 
                        new Dictionary<string, object> { ["error"] = result.Error });
                }

                results.Add(new TestResult
                {
                    Name = cmd.Name,
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error
                });
            }
            catch (Exception ex)
            {
                LogTest($"Nexo Command: {cmd.Name}", false, $"{cmd.Description} - Exception: {ex.Message}", new Dictionary<string, object>());
                results.Add(new TestResult
                {
                    Name = cmd.Name,
                    Success = false,
                    Output = "",
                    Error = ex.Message
                });
            }
        }

        return results;
    }

    private async Task TestDirectorCommandsAsync()
    {
        Console.WriteLine("\n=== Testing Director Command Generation ===");

        var testCommands = new[]
        {
            new
            {
                Type = "RunNexo",
                Payload = new RunNexoPayload("validate", new[] { "--filter", "Category=Architecture", "--format-json" }, _projectRoot)
            },
            new
            {
                Type = "RunNexo",
                Payload = new RunNexoPayload("list", new[] { "agents" }, _projectRoot)
            },
            new
            {
                Type = "GetProjectInfo",
                Payload = new GetProjectInfoPayload()
            },
            new
            {
                Type = "ListScenes",
                Payload = new ListScenesPayload()
            }
        };

        for (int i = 0; i < testCommands.Length; i++)
        {
            var command = testCommands[i];
            Console.WriteLine($"\nTest Command {i + 1}: {command.Type}");
            Console.WriteLine($"Payload: {JsonSerializer.Serialize(command.Payload, new JsonSerializerOptions { WriteIndented = true })}");

            // Simulate what Director Studio would do
            if (command.Type == "RunNexo")
            {
                var payload = (RunNexoPayload)command.Payload;
                var nexoCmd = $"run --project src/Nexo.CLI/Nexo.CLI.csproj -- {payload.Command} {string.Join(" ", payload.Args)}";
                Console.WriteLine($"Would execute: dotnet {nexoCmd}");

                try
                {
                    var result = await RunCommandAsync("dotnet", nexoCmd);
                    Console.WriteLine($"Result: {result.Success} (success: {result.Success})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Unity Editor command (not executable in this test)");
            }
        }
    }

    private bool GenerateTestReport(List<TestResult> nexoResults)
    {
        Console.WriteLine("\n=== Test Report ===");

        var totalTests = nexoResults.Count;
        var successfulTests = nexoResults.Count(r => r.Success);
        var failedTests = totalTests - successfulTests;

        Console.WriteLine($"Total Tests: {totalTests}");
        Console.WriteLine($"Successful: {successfulTests}");
        Console.WriteLine($"Failed: {failedTests}");
        Console.WriteLine($"Success Rate: {(successfulTests / (double)totalTests) * 100:F1}%");

        if (failedTests > 0)
        {
            Console.WriteLine("\nFailed Tests:");
            foreach (var result in nexoResults.Where(r => !r.Success))
            {
                Console.WriteLine($"  - {result.Name}: {result.Error[..Math.Min(100, result.Error.Length)]}...");
            }
        }

        var allSuccess = successfulTests == totalTests;

        if (allSuccess)
        {
            Console.WriteLine("\n✅ All integration tests passed!");
            Console.WriteLine("\nDirector Studio is ready for:");
            Console.WriteLine("  - Nexo command execution");
            Console.WriteLine("  - Architecture validation");
            Console.WriteLine("  - Project analysis");
            Console.WriteLine("  - Agent listing");
        }
        else
        {
            Console.WriteLine("\n❌ Some integration tests failed!");
            Console.WriteLine("Check the output above for details.");
        }

        return allSuccess;
    }

    private async Task<CommandResult> RunCommandAsync(string command, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return new CommandResult { Success = false, Output = "", Error = "Failed to start process" };

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new CommandResult
            {
                Success = process.ExitCode == 0,
                Output = output.Trim(),
                Error = error.Trim()
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Output = "",
                Error = ex.Message
            };
        }
    }

    private void LogTest(string testName, bool success, string message, Dictionary<string, object> details)
    {
        var result = new TestResult
        {
            Name = testName,
            Success = success,
            Output = message,
            Error = ""
        };
        _testResults.Add(result);

        var status = success ? "✅" : "❌";
        Console.WriteLine($"{status} {testName}: {message}");
        foreach (var detail in details)
        {
            Console.WriteLine($"    {detail.Key}: {detail.Value}");
        }
    }

    private class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
    }

    private class TestResult
    {
        public string Name { get; set; } = "";
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
    }

    // Removed Main method - using TestRunner instead
}
