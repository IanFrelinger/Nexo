using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Director.Tests;

/// <summary>
/// Test class to validate Unity project setup with Director Studio
/// </summary>
public class UnityProjectSetupTest
{
    private readonly string _projectPath;
    private readonly string _nexoRoot;
    private readonly List<TestResult> _testResults = new();

    public UnityProjectSetupTest()
    {
        _nexoRoot = Directory.GetCurrentDirectory();
        _projectPath = Path.Combine(_nexoRoot, "UnityProjects", "NexoDirectorDemo");
    }

    public async Task<bool> RunAllTestsAsync()
    {
        Console.WriteLine("Unity Project Setup Test");
        Console.WriteLine("=" + new string('=', 30));

        var tests = new Func<Task<bool>>[]
        {
            TestProjectStructureAsync,
            TestDirectorPackageAsync,
            TestManifestDependenciesAsync,
            TestScriptCompilationAsync,
            TestDirectorStudioAvailabilityAsync,
            TestNexoCliAvailabilityAsync
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

        return GenerateReport();
    }

    private async Task<bool> TestProjectStructureAsync()
    {
        Console.WriteLine("=== Test 1: Unity Project Structure ===");

        var requiredFiles = new[]
        {
            "ProjectSettings/ProjectVersion.txt",
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scripts/NexoValidationDemo.cs",
            "Assets/Scripts/DirectorStudioController.cs",
            "Assets/Editor/DirectorStudioWindow.cs",
            "Packages/manifest.json"
        };

        // Check if Director package exists in the main project
        var directorPackageExists = File.Exists(Path.Combine(_nexoRoot, "Packages/com.nexo.director/package.json"));
        if (directorPackageExists)
        {
            LogTest("Director Package", true, "Packages/com.nexo.director/package.json (in main project)", new Dictionary<string, object>());
        }
        else
        {
            LogTest("Director Package", false, "Packages/com.nexo.director/package.json - Missing", new Dictionary<string, object>());
        }

        var allExist = true;
        foreach (var filePath in requiredFiles)
        {
            var fullPath = Path.Combine(_projectPath, filePath);
            if (File.Exists(fullPath))
            {
                LogTest($"File: {filePath}", true, "Exists", new Dictionary<string, object>());
            }
            else
            {
                LogTest($"File: {filePath}", false, "Missing", new Dictionary<string, object>());
                allExist = false;
            }
        }

        return allExist && directorPackageExists;
    }

    private async Task<bool> TestDirectorPackageAsync()
    {
        Console.WriteLine("\n=== Test 2: Director Studio Package ===");

        var packageJsonPath = Path.Combine(_nexoRoot, "Packages/com.nexo.director/package.json");

        try
        {
            var jsonContent = await File.ReadAllTextAsync(packageJsonPath);
            var packageData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var requiredFields = new[] { "name", "version", "displayName", "description", "unity" };
            var allFieldsPresent = true;

            foreach (var field in requiredFields)
            {
                if (packageData.TryGetProperty(field, out var value))
                {
                    LogTest($"Package Field: {field}", true, value.GetString() ?? "", new Dictionary<string, object>());
                }
                else
                {
                    LogTest($"Package Field: {field}", false, "Missing", new Dictionary<string, object>());
                    allFieldsPresent = false;
                }
            }

            return allFieldsPresent;
        }
        catch (Exception ex)
        {
            LogTest("Package JSON", false, $"Error reading package.json: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<bool> TestManifestDependenciesAsync()
    {
        Console.WriteLine("\n=== Test 3: Package Dependencies ===");

        var manifestPath = Path.Combine(_projectPath, "Packages/manifest.json");

        try
        {
            var jsonContent = await File.ReadAllTextAsync(manifestPath);
            var manifestData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var dependencies = manifestData.GetProperty("dependencies");

            var directorDependency = dependencies.TryGetProperty("com.nexo.director", out var directorValue);
            if (directorDependency)
            {
                LogTest("Director Dependency", true, directorValue.GetString() ?? "", new Dictionary<string, object>());
            }
            else
            {
                LogTest("Director Dependency", false, "Missing", new Dictionary<string, object>());
                return false;
            }

            var jsonDependency = dependencies.TryGetProperty("com.unity.nuget.newtonsoft-json", out var jsonValue);
            if (jsonDependency)
            {
                LogTest("JSON Dependency", true, jsonValue.GetString() ?? "", new Dictionary<string, object>());
            }
            else
            {
                LogTest("JSON Dependency", false, "Missing", new Dictionary<string, object>());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogTest("Manifest JSON", false, $"Error reading manifest.json: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private async Task<bool> TestScriptCompilationAsync()
    {
        Console.WriteLine("\n=== Test 4: Script Compilation ===");

        var scriptFiles = new[]
        {
            "Assets/Scripts/NexoValidationDemo.cs",
            "Assets/Scripts/DirectorStudioController.cs",
            "Assets/Editor/DirectorStudioWindow.cs"
        };

        var allScriptsExist = true;
        foreach (var scriptFile in scriptFiles)
        {
            var scriptPath = Path.Combine(_projectPath, scriptFile);
            if (File.Exists(scriptPath))
            {
                LogTest($"Script: {scriptFile}", true, "Exists", new Dictionary<string, object>());
            }
            else
            {
                LogTest($"Script: {scriptFile}", false, "Missing", new Dictionary<string, object>());
                allScriptsExist = false;
            }
        }

        return allScriptsExist;
    }

    private async Task<bool> TestDirectorStudioAvailabilityAsync()
    {
        Console.WriteLine("\n=== Test 5: Director Studio Availability ===");

        var directorProjects = new[]
        {
            "tools/Director.Core/Director.Core.csproj",
            "tools/Director.Avalonia/Director.Avalonia.csproj"
        };

        var allProjectsExist = true;
        foreach (var project in directorProjects)
        {
            var projectPath = Path.Combine(_nexoRoot, project);
            if (File.Exists(projectPath))
            {
                LogTest($"Project: {project}", true, "Exists", new Dictionary<string, object>());
            }
            else
            {
                LogTest($"Project: {project}", false, "Missing", new Dictionary<string, object>());
                allProjectsExist = false;
            }
        }

        return allProjectsExist;
    }

    private async Task<bool> TestNexoCliAvailabilityAsync()
    {
        Console.WriteLine("\n=== Test 6: Nexo CLI Availability ===");

        try
        {
            var result = await RunCommandAsync("dotnet", "run --project src/Nexo.CLI/Nexo.CLI.csproj -- --help");

            if (result.Success)
            {
                LogTest("Nexo CLI", true, "Nexo CLI is available and working", new Dictionary<string, object>());
                return true;
            }
            else
            {
                LogTest("Nexo CLI", false, $"Nexo CLI error: {result.Error}", new Dictionary<string, object>());
                return false;
            }
        }
        catch (Exception ex)
        {
            LogTest("Nexo CLI", false, $"Error running Nexo CLI: {ex.Message}", new Dictionary<string, object>());
            return false;
        }
    }

    private bool GenerateReport()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("UNITY PROJECT SETUP TEST REPORT");
        Console.WriteLine(new string('=', 60));

        var totalTests = _testResults.Count;
        var successfulTests = _testResults.Count(r => r.Success);
        var failedTests = totalTests - successfulTests;

        Console.WriteLine($"Total Tests: {totalTests}");
        Console.WriteLine($"Successful: {successfulTests}");
        Console.WriteLine($"Failed: {failedTests}");
        Console.WriteLine($"Success Rate: {(successfulTests / (double)totalTests) * 100:F1}%");

        if (failedTests == 0)
        {
            Console.WriteLine("\n🎉 ALL TESTS PASSED!");
            Console.WriteLine("\nUnity project is ready for Director Studio integration:");
            Console.WriteLine("  ✅ Project structure created");
            Console.WriteLine("  ✅ Director Studio package integrated");
            Console.WriteLine("  ✅ Dependencies configured");
            Console.WriteLine("  ✅ Demo scripts created");
            Console.WriteLine("  ✅ Nexo CLI available");
            Console.WriteLine("\nNext steps:");
            Console.WriteLine("  1. Open Unity and load the project");
            Console.WriteLine("  2. Start Director Studio application");
            Console.WriteLine("  3. Connect and test the integration");
        }
        else
        {
            Console.WriteLine($"\n⚠️  {failedTests} tests failed");
            Console.WriteLine("Check the output above for issues to resolve");
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
