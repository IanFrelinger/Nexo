using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Tests;

/// <summary>
/// Simple test runner for Director Studio tests
/// </summary>
public class SimpleTestRunner
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Director Studio Test Suite (C#)");
        Console.WriteLine("===============================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Available tests:");
            Console.WriteLine("  nexo         - Test Nexo integration");
            Console.WriteLine("  protocol     - Test Director protocol");
            Console.WriteLine("  unity        - Test Unity project setup");
            Console.WriteLine("  all          - Run all tests");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <test_name>");
            return 0;
        }

        var testName = args[0].ToLower();
        var success = false;

        try
        {
            switch (testName)
            {
                case "nexo":
                    Console.WriteLine("Running Nexo Integration Test...");
                    success = await TestNexoIntegrationAsync();
                    break;

                case "protocol":
                    Console.WriteLine("Running Director Protocol Test...");
                    success = TestDirectorProtocol();
                    break;

                case "unity":
                    Console.WriteLine("Running Unity Project Setup Test...");
                    success = TestUnityProjectSetup();
                    break;

                case "all":
                    Console.WriteLine("Running All Tests...");
                    success = await RunAllTestsAsync();
                    break;

                default:
                    Console.WriteLine($"Unknown test: {testName}");
                    Console.WriteLine("Use 'dotnet run --' to see available tests");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            return 1;
        }

        if (success)
        {
            Console.WriteLine("\n🎉 Test completed successfully!");
            return 0;
        }
        else
        {
            Console.WriteLine("\n❌ Test failed!");
            return 1;
        }
    }

    private static async Task<bool> TestNexoIntegrationAsync()
    {
        Console.WriteLine("\n=== Testing Nexo CLI Integration ===");

        try
        {
            // Test Nexo CLI availability
            var result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- --version");
            
            if (!result.Success)
            {
                Console.WriteLine($"❌ Nexo CLI not available: {result.Error}");
                return false;
            }

            Console.WriteLine($"✅ Nexo CLI available: {result.Output}");

            // Test validation command
            result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- validate --format-json");
            
            if (!result.Success)
            {
                Console.WriteLine($"❌ Validation command failed: {result.Error}");
                return false;
            }

            Console.WriteLine($"✅ Validation command working: {result.Output}");

            // Test analysis command
            result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- analyze --format-json");
            
            if (!result.Success)
            {
                Console.WriteLine($"❌ Analysis command failed: {result.Error}");
                return false;
            }

            Console.WriteLine($"✅ Analysis command working: {result.Output}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Nexo integration test failed: {ex.Message}");
            return false;
        }
    }

    private static bool TestDirectorProtocol()
    {
        Console.WriteLine("\n=== Testing Director Protocol ===");

        try
        {
            // Test command serialization
            var testCommand = new DirectorCommand
            {
                Id = "test-123",
                Type = "RunNexo",
                Payload = new RunNexoPayload("validate", new[] { "--format-json" }, Directory.GetCurrentDirectory())
            };

            var jsonStr = JsonSerializer.Serialize(testCommand);
            var parsed = JsonSerializer.Deserialize<DirectorEvent>(jsonStr);

            if (parsed?.Type == testCommand.Type)
            {
                Console.WriteLine("✅ Command serialization working");
            }
            else
            {
                Console.WriteLine("❌ Command serialization failed");
                return false;
            }

            // Test event serialization
            var testEvent = new DirectorEvent
            {
                Type = "LogLine",
                Payload = new LogLineEvent("Test log message", LogLevel.Information)
            };

            jsonStr = JsonSerializer.Serialize(testEvent);
            parsed = JsonSerializer.Deserialize<DirectorEvent>(jsonStr);

            if (parsed?.Type == testEvent.Type)
            {
                Console.WriteLine("✅ Event serialization working");
            }
            else
            {
                Console.WriteLine("❌ Event serialization failed");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Director protocol test failed: {ex.Message}");
            return false;
        }
    }

    private static bool TestUnityProjectSetup()
    {
        Console.WriteLine("\n=== Testing Unity Project Setup ===");

        var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "UnityProjects", "NexoDirectorDemo");
        var requiredFiles = new[]
        {
            "ProjectSettings/ProjectVersion.txt",
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scripts/NexoValidationDemo.cs",
            "Assets/Scripts/DirectorStudioController.cs",
            "Assets/Editor/DirectorStudioWindow.cs",
            "Packages/manifest.json"
        };

        var allExist = true;
        foreach (var filePath in requiredFiles)
        {
            var fullPath = Path.Combine(projectPath, filePath);
            if (File.Exists(fullPath))
            {
                Console.WriteLine($"✅ {filePath}");
            }
            else
            {
                Console.WriteLine($"❌ {filePath} - Missing");
                allExist = false;
            }
        }

        // Check Director package
        var directorPackagePath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "com.nexo.director", "package.json");
        if (File.Exists(directorPackagePath))
        {
            Console.WriteLine("✅ Director Studio package available");
        }
        else
        {
            Console.WriteLine("❌ Director Studio package missing");
            allExist = false;
        }

        return allExist;
    }

    private static async Task<bool> RunAllTestsAsync()
    {
        Console.WriteLine("\n=== Running All Tests ===");

        var tests = new (string Name, Func<Task<bool>> Test)[]
        {
            ("Nexo Integration", async () => await TestNexoIntegrationAsync()),
            ("Director Protocol", () => Task.FromResult(TestDirectorProtocol())),
            ("Unity Project Setup", () => Task.FromResult(TestUnityProjectSetup()))
        };

        var allSuccess = true;
        var results = new List<(string Name, bool Success)>();

        foreach (var testInfo in tests)
        {
            Console.WriteLine($"\n{'='*50}");
            Console.WriteLine($"Running {testInfo.Name} Test");
            Console.WriteLine($"{'='*50}");

            try
            {
                var success = await testInfo.Test();
                results.Add((testInfo.Name, success));
                allSuccess = allSuccess && success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {testInfo.Name} test failed with exception: {ex.Message}");
                results.Add((testInfo.Name, false));
                allSuccess = false;
            }
        }

        // Summary
        Console.WriteLine($"\n{'='*50}");
        Console.WriteLine("TEST SUITE SUMMARY");
        Console.WriteLine($"{'='*50}");

        foreach (var (name, success) in results)
        {
            var status = success ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"{status} {name}");
        }

        var passedCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        Console.WriteLine($"\nTotal: {passedCount}/{totalCount} tests passed");

        if (allSuccess)
        {
            Console.WriteLine("\n🎉 ALL TESTS PASSED!");
            Console.WriteLine("Director Studio is ready for production use!");
        }
        else
        {
            Console.WriteLine("\n⚠️  Some tests failed. Check the output above for details.");
        }

        return allSuccess;
    }

    private static async Task<CommandResult> RunCommandAsync(string command, string arguments)
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

    private class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
    }
}
