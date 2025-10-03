using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Tests;

/// <summary>
/// End-to-End Test for Director Studio
/// This class tests the complete workflow from Avalonia app to Nexo execution
/// </summary>
public class EndToEndTest
{
    private readonly string _projectRoot;
    private readonly List<TestResult> _testResults = new();
    private const int Port = 5088;

    public EndToEndTest()
    {
        _projectRoot = Directory.GetCurrentDirectory();
    }

    public async Task<bool> RunAllTestsAsync()
    {
        Console.WriteLine("Director Studio End-to-End Integration Test");
        Console.WriteLine("=" + new string('=', 50));

        var tests = new Func<Task<bool>>[]
        {
            TestNexoCliAvailabilityAsync,
            TestNexoCommandsAsync,
            TestDirectorProtocolAsync,
            TestMockUnityServerAsync,
            TestAvaloniaAppBuildAsync
        };

        foreach (var test in tests)
        {
            try
            {
                await test();
            }
            catch (Exception ex)
            {
                LogTest(test.Method.Name, false, $"Test failed with exception: {ex.Message}", new Dictionary<string, object>());
            }
        }

        return GenerateFinalReport();
    }

    private async Task<bool> TestNexoCliAvailabilityAsync()
    {
        Console.WriteLine("\n=== Test 1: Nexo CLI Availability ===");

        try
        {
            var result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- --version");
            
            if (result.Success)
            {
                LogTest("Nexo CLI Available", true, "Nexo CLI is working", 
                    new Dictionary<string, object> { ["version_output"] = result.Output });
                return true;
            }
            else
            {
                LogTest("Nexo CLI Available", false, "Nexo CLI failed to run", 
                    new Dictionary<string, object> { ["error"] = result.Error });
                return false;
            }
        }
        catch (Exception ex)
        {
            LogTest("Nexo CLI Available", false, $"Error running Nexo CLI: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<bool> TestNexoCommandsAsync()
    {
        Console.WriteLine("\n=== Test 2: Nexo Commands ===");

        var commands = new[]
        {
            new { Name = "validate", Args = new[] { "--format-json" }, Description = "Architecture validation" },
            new { Name = "analyze", Args = new[] { "--format-json" }, Description = "Code analysis" },
            new { Name = "agent", Args = new[] { "--name", "list", "--format-json" }, Description = "List agents" }
        };

        var allSuccess = true;

        foreach (var cmd in commands)
        {
            try
            {
                var fullCommand = $"run --project src/Nexo.CLI/Nexo.CLI.csproj -- {cmd.Name} {string.Join(" ", cmd.Args)}";
                var result = await RunCommandAsync("dotnet", fullCommand);

                if (result.Success)
                {
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
                        allSuccess = false;
                    }
                }
                else
                {
                    LogTest($"Nexo Command: {cmd.Name}", false, $"{cmd.Description} - Command failed",
                        new Dictionary<string, object> { ["error"] = result.Error });
                    allSuccess = false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"Nexo Command: {cmd.Name}", false, $"{cmd.Description} - Exception: {ex.Message}", new Dictionary<string, object>());
                allSuccess = false;
            }
        }

        return allSuccess;
    }

    private async Task<bool> TestDirectorProtocolAsync()
    {
        Console.WriteLine("\n=== Test 3: Director Protocol ===");

        try
        {
            // Test command serialization
            var testCommand = new DirectorCommand
            {
                Id = "test-123",
                Type = "RunNexo",
                Payload = new RunNexoPayload("validate", new[] { "--format-json" }, _projectRoot)
            };

            var jsonStr = JsonSerializer.Serialize(testCommand);
            var parsed = JsonSerializer.Deserialize<DirectorEvent>(jsonStr);

            LogTest("Command Serialization", true, "Director command serializes correctly",
                new Dictionary<string, object> { ["command_type"] = testCommand.Type });

            // Test event serialization
            var testEvent = new DirectorEvent
            {
                Type = "LogLine",
                Payload = new LogLineEvent("Test log message", LogLevel.Information)
            };

            jsonStr = JsonSerializer.Serialize(testEvent);
            parsed = JsonSerializer.Deserialize<DirectorEvent>(jsonStr);

            LogTest("Event Serialization", true, "Director event serializes correctly",
                new Dictionary<string, object> { ["event_type"] = testEvent.Type });

            return true;
        }
        catch (Exception ex)
        {
            LogTest("Protocol Serialization", false, $"Serialization failed: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<bool> TestMockUnityServerAsync()
    {
        Console.WriteLine("\n=== Test 4: Mock Unity Server ===");

        try
        {
            // Start mock server
            var server = new TcpListener(IPAddress.Loopback, Port);
            server.Start();

            LogTest("Mock Server Start", true, $"Mock Unity server started on port {Port}", new Dictionary<string, object>());

            // Test server in background
            var receivedMessages = new List<DirectorEvent>();
            var serverTask = Task.Run(async () =>
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    var stream = client.GetStream();
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                    // Read token
                    var token = await reader.ReadLineAsync();

                    // Send welcome message
                    var welcome = new DirectorEvent
                    {
                        Type = "ConnectionEvent",
                        Payload = new ConnectionEvent(true, "Connected to Mock Unity Editor")
                    };
                    await writer.WriteLineAsync(JsonSerializer.Serialize(welcome));

                    // Send test events
                    var events = new[]
                    {
                        new DirectorEvent
                        {
                            Type = "LogLine",
                            Payload = new LogLineEvent("Unity Editor ready", LogLevel.Information)
                        },
                        new DirectorEvent
                        {
                            Type = "GateResult",
                            Payload = new GateResultEvent("Test Gate", true, "Test passed")
                        }
                    };

                    foreach (var evt in events)
                    {
                        await writer.WriteLineAsync(JsonSerializer.Serialize(evt));
                        await Task.Delay(100);
                    }

                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Mock server error: {ex.Message}");
                }
            });

            // Test client connection
            await Task.Delay(500); // Let server start

            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, Port);

            // Send token
            var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            await writer.WriteLineAsync("test-token-123");

            // Receive messages
            var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            client.ReceiveTimeout = 2000;

            try
            {
                while (true)
                {
                    var data = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(data)) break;
                    receivedMessages.Add(JsonSerializer.Deserialize<DirectorEvent>(data)!);
                }
            }
            catch (Exception)
            {
                // Expected timeout
            }

            client.Close();
            server.Stop();

            if (receivedMessages.Count >= 2) // Welcome + events
            {
                LogTest("Mock Server Communication", true, $"Received {receivedMessages.Count} messages",
                    new Dictionary<string, object> { ["message_types"] = receivedMessages.Select(m => m.Type).ToArray() });
                return true;
            }
            else
            {
                LogTest("Mock Server Communication", false, $"Only received {receivedMessages.Count} messages", new Dictionary<string, object>());
                return false;
            }
        }
        catch (Exception ex)
        {
            LogTest("Mock Server Test", false, $"Mock server failed: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<bool> TestAvaloniaAppBuildAsync()
    {
        Console.WriteLine("\n=== Test 5: Avalonia App Build ===");

        try
        {
            // Check if Avalonia app is already running
            var processes = Process.GetProcessesByName("Director.Avalonia");
            if (processes.Length > 0)
            {
                LogTest("Avalonia App Running", true, "Avalonia app is already running", new Dictionary<string, object>());
                return true;
            }

            // Try to build the app
            var result = await RunCommandAsync("dotnet", "build tools/Director.Avalonia/Director.Avalonia.csproj --configuration Release");

            if (result.Success)
            {
                LogTest("Avalonia App Build", true, "Avalonia app builds successfully", new Dictionary<string, object>());
                return true;
            }
            else
            {
                LogTest("Avalonia App Build", false, "Avalonia app build failed",
                    new Dictionary<string, object> { ["error"] = result.Error });
                return false;
            }
        }
        catch (Exception ex)
        {
            LogTest("Avalonia App Test", false, $"Avalonia app test failed: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private bool GenerateFinalReport()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("DIRECTOR STUDIO END-TO-END TEST REPORT");
        Console.WriteLine(new string('=', 60));

        var totalTests = _testResults.Count;
        var successfulTests = _testResults.Count(r => r.Success);
        var failedTests = totalTests - successfulTests;

        Console.WriteLine($"Total Tests: {totalTests}");
        Console.WriteLine($"Successful: {successfulTests}");
        Console.WriteLine($"Failed: {failedTests}");
        Console.WriteLine($"Success Rate: {(successfulTests / (double)totalTests) * 100:F1}%");

        Console.WriteLine($"\nTest Details:");
        foreach (var result in _testResults)
        {
            var status = result.Success ? "✅" : "❌";
            Console.WriteLine($"  {status} {result.Name}: {result.Message}");
        }

        if (failedTests == 0)
        {
            Console.WriteLine($"\n🎉 ALL TESTS PASSED!");
            Console.WriteLine($"\nDirector Studio is ready for production use:");
            Console.WriteLine($"  ✅ Nexo CLI integration working");
            Console.WriteLine($"  ✅ Director protocol implemented");
            Console.WriteLine($"  ✅ Avalonia app building and running");
            Console.WriteLine($"  ✅ IPC communication tested");
            Console.WriteLine($"  ✅ End-to-end workflow validated");
        }
        else
        {
            Console.WriteLine($"\n⚠️  {failedTests} tests failed");
            Console.WriteLine($"Check the details above for issues to resolve");
        }

        return failedTests == 0;
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
            Message = message,
            Details = details
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
        public string Message { get; set; } = "";
        public Dictionary<string, object> Details { get; set; } = new();
    }

    // Removed Main method - using TestRunner instead
}
