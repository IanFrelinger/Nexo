using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexo.Tools.TestRunner;

class Program
{
    private static readonly string ProjectRoot = GetProjectRoot();
    private static readonly string TestResultsDir = Path.Combine(ProjectRoot, "test-results");
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🧪 Nexo CLI Unit Test Runner (Cross-Platform)");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Parse arguments
        var platforms = new List<string>();
        var includeMobile = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--all":
                    platforms.Add("ubuntu");
                    break;
                case "--mobile":
                    includeMobile = true;
                    break;
                case "--quick":
                    // TODO: Implement quick mode (skip build cache)
                    break;
                case "--platform":
                    if (i + 1 < args.Length)
                    {
                        platforms.Add(args[++i]);
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    PrintUsage();
                    return 1;
            }
        }

        // Default to ubuntu if no platform specified
        if (platforms.Count == 0)
        {
            platforms.Add("ubuntu");
        }

        // Add mobile platforms if requested
        if (includeMobile)
        {
            if (IsMacOS)
            {
                platforms.Add("ios");
            }
            platforms.Add("android");
        }

        // Ensure test results directory exists
        Directory.CreateDirectory(TestResultsDir);

        // Run tests
        var failedPlatforms = new List<string>();
        var passedPlatforms = new List<string>();

        foreach (var platform in platforms)
        {
            Console.WriteLine($"Testing on {platform}...");
            Console.WriteLine();

            bool success = false;
            try
            {
                switch (platform.ToLower())
                {
                    case "ios":
                        success = await RunIOSTests();
                        break;
                    case "android":
                        success = await RunAndroidTests();
                        break;
                    case "ubuntu":
                    case "linux":
                        success = await RunDockerTests("ubuntu");
                        break;
                    default:
                        Console.WriteLine($"❌ Unknown platform: {platform}");
                        failedPlatforms.Add(platform);
                        continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running tests on {platform}: {ex.Message}");
                failedPlatforms.Add(platform);
                continue;
            }

            if (success)
            {
                passedPlatforms.Add(platform);
            }
            else
            {
                failedPlatforms.Add(platform);
            }

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("==============================================");
        Console.WriteLine("📊 Test Summary");
        Console.WriteLine("==============================================");

        if (passedPlatforms.Count > 0)
        {
            Console.WriteLine("✅ Passed platforms:");
            foreach (var platform in passedPlatforms)
            {
                Console.WriteLine($"   ✓ {platform}");
            }
        }

        if (failedPlatforms.Count > 0)
        {
            Console.WriteLine("❌ Failed platforms:");
            foreach (var platform in failedPlatforms)
            {
                Console.WriteLine($"   ✗ {platform}");
                var logFile = Path.Combine(TestResultsDir, $"{platform}-logs.txt");
                if (File.Exists(logFile))
                {
                    Console.WriteLine($"   Check {logFile} for details");
                }
            }
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("✅ All tests passed on all platforms!");
        Console.WriteLine();
        Console.WriteLine($"Test results are available in: {TestResultsDir}");

        return 0;
    }

    private static async Task<bool> RunDockerTests(string platform)
    {
        Console.WriteLine($"🐳 Running tests in Docker ({platform})...");

        var dockerfile = Path.Combine(ProjectRoot, ".docker", "Dockerfile.test");
        if (!File.Exists(dockerfile))
        {
            Console.WriteLine($"❌ Dockerfile not found: {dockerfile}");
            return false;
        }

        // Build Docker image
        var buildArgs = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"build -f {dockerfile} -t nexo-test:{platform} .",
            WorkingDirectory = ProjectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        Console.WriteLine($"Building Docker image: nexo-test:{platform}...");
        using var buildProcess = Process.Start(buildArgs);
        if (buildProcess == null)
        {
            Console.WriteLine("❌ Failed to start Docker build process");
            return false;
        }

        await buildProcess.WaitForExitAsync();
        if (buildProcess.ExitCode != 0)
        {
            Console.WriteLine("❌ Docker build failed");
            var error = await buildProcess.StandardError.ReadToEndAsync();
            Console.WriteLine(error);
            return false;
        }

        // Run tests in Docker
        var resultsFile = Path.Combine(TestResultsDir, $"{platform}-results.json");
        var logsFile = Path.Combine(TestResultsDir, $"{platform}-logs.txt");
        var resultsFileInContainer = "/workspace/test-results/results.json";
        var logsFileInContainer = "/workspace/test-results/logs.txt";

        // Create Python script for JSON extraction
        var pythonScript = $@"import json
import sys
import re
try:
    with open('{resultsFileInContainer}', 'r') as f:
        content = f.read()
    matches = re.findall(r'\\{{[^{{}}]*(?:\\{{[^{{}}]*\\}}[^{{}}]*)*\\}}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open('{resultsFileInContainer}', 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'✅ {platform}: {{total}} total, {{passed}} passed, {{failed}} failed')
                sys.exit(0 if failed == 0 else 1)
        except:
            continue
    print('❌ No valid test results found')
    sys.exit(1)
except Exception as e:
    print(f'Error: {{e}}')
    sys.exit(1)
";

        var pythonScriptFile = Path.Combine(Path.GetTempPath(), $"nexo-extract-{platform}-{Guid.NewGuid()}.py");
        await File.WriteAllTextAsync(pythonScriptFile, pythonScript);

        // Use a simpler approach: write script to temp file and execute it
        var scriptFile = Path.Combine(Path.GetTempPath(), $"nexo-test-{platform}-{Guid.NewGuid()}.sh");
        var runScript = $@"cd src/Nexo.CLI &&
dotnet run --project Nexo.CLI.csproj -- test --format-json > {resultsFileInContainer} 2>{logsFileInContainer} || true &&
python3 /scripts/{Path.GetFileName(pythonScriptFile)}
";
        var scriptContent = "#!/bin/bash\n" + runScript;
        await File.WriteAllTextAsync(scriptFile, scriptContent);
        
        if (!IsWindows)
        {
            // Make executable on Unix
            var chmodProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x {scriptFile}",
                UseShellExecute = false
            });
            chmodProcess?.WaitForExit();
        }
        
        try
        {
            var runArgs = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --rm -v \"{ProjectRoot}/test-results:/workspace/test-results\" -v \"{Path.GetDirectoryName(scriptFile)}:/scripts\" -v \"{Path.GetDirectoryName(pythonScriptFile)}:/scripts\" nexo-test:{platform} bash /scripts/{Path.GetFileName(scriptFile)}",
                WorkingDirectory = ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var runProcess = Process.Start(runArgs);
            if (runProcess == null)
            {
                Console.WriteLine("❌ Failed to start Docker run process");
                return false;
            }

            var output = await runProcess.StandardOutput.ReadToEndAsync();
            var error = await runProcess.StandardError.ReadToEndAsync();

            await runProcess.WaitForExitAsync();

            Console.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }

            // Copy results from container
            var tempResults = Path.Combine(TestResultsDir, "results.json");
            var tempLogs = Path.Combine(TestResultsDir, "logs.txt");
            if (File.Exists(tempResults))
            {
                File.Move(tempResults, resultsFile, true);
            }
            if (File.Exists(tempLogs))
            {
                File.Move(tempLogs, logsFile, true);
            }

            return runProcess.ExitCode == 0;
        }
        finally
        {
            if (File.Exists(scriptFile))
            {
                File.Delete(scriptFile);
            }
            if (File.Exists(pythonScriptFile))
            {
                File.Delete(pythonScriptFile);
            }
        }
    }

    private static async Task<bool> RunAndroidTests()
    {
        Console.WriteLine("🤖 Running tests on Android (Docker)...");

        var dockerfile = Path.Combine(ProjectRoot, ".docker", "Dockerfile.test-android");
        if (!File.Exists(dockerfile))
        {
            Console.WriteLine($"❌ Android Dockerfile not found: {dockerfile}");
            return false;
        }

        // Build Docker image
        var buildArgs = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"build -f {dockerfile} -t nexo-test:android .",
            WorkingDirectory = ProjectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        Console.WriteLine("Building Android Docker image...");
        using var buildProcess = Process.Start(buildArgs);
        if (buildProcess == null)
        {
            Console.WriteLine("❌ Failed to start Docker build process");
            return false;
        }

        await buildProcess.WaitForExitAsync();
        if (buildProcess.ExitCode != 0)
        {
            Console.WriteLine("❌ Docker build failed");
            var error = await buildProcess.StandardError.ReadToEndAsync();
            Console.WriteLine(error);
            return false;
        }

        // Run tests
        var resultsFile = Path.Combine(TestResultsDir, "android-results.json");
        var logsFile = Path.Combine(TestResultsDir, "android-logs.txt");

        // Create Python script for JSON extraction
        var androidPythonScript = @"import json
import sys
import re
try:
    with open('/workspace/test-results/android-output.txt', 'r') as f:
        content = f.read()
    matches = re.findall(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open('/workspace/test-results/android-results.json', 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'✅ Android: {total} total, {passed} passed, {failed} failed')
                sys.exit(0 if failed == 0 else 1)
        except:
            continue
    print('❌ No valid test results found')
    sys.exit(1)
except Exception as e:
    print(f'Error: {e}')
    sys.exit(1)
";

        var androidPythonScriptFile = Path.Combine(Path.GetTempPath(), $"nexo-extract-android-{Guid.NewGuid()}.py");
        await File.WriteAllTextAsync(androidPythonScriptFile, androidPythonScript);

        var runScript = $@"cd src/Nexo.CLI &&
dotnet run --project Nexo.CLI.csproj -- test --format-json > /workspace/test-results/android-output.txt 2>/workspace/test-results/android-logs.txt || true &&
python3 /scripts/{Path.GetFileName(androidPythonScriptFile)}
";

        // Use a simpler approach: write script to temp file and execute it
        var scriptFile = Path.Combine(Path.GetTempPath(), $"nexo-test-android-{Guid.NewGuid()}.sh");
        // Make script executable on Unix systems
        var scriptContent = "#!/bin/bash\n" + runScript;
        await File.WriteAllTextAsync(scriptFile, scriptContent);
        
        if (!IsWindows)
        {
            // Make executable on Unix
            var chmodProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x {scriptFile}",
                UseShellExecute = false
            });
            chmodProcess?.WaitForExit();
        }
        
        try
        {
            var runArgs = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --rm -v \"{ProjectRoot}/test-results:/workspace/test-results\" -v \"{Path.GetDirectoryName(scriptFile)}:/scripts\" -e ANDROID_HOME=/opt/android-sdk nexo-test:android bash /scripts/{Path.GetFileName(scriptFile)}",
                WorkingDirectory = ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

        using var runProcess = Process.Start(runArgs);
        if (runProcess == null)
        {
            Console.WriteLine("❌ Failed to start Docker run process");
            return false;
        }

        var output = await runProcess.StandardOutput.ReadToEndAsync();
        var error = await runProcess.StandardError.ReadToEndAsync();

        await runProcess.WaitForExitAsync();

        Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine(error);
        }

        return runProcess.ExitCode == 0;
        }
        finally
        {
            if (File.Exists(scriptFile))
            {
                File.Delete(scriptFile);
            }
            if (File.Exists(androidPythonScriptFile))
            {
                File.Delete(androidPythonScriptFile);
            }
        }
    }

    private static async Task<bool> RunIOSTests()
    {
        if (!IsMacOS)
        {
            Console.WriteLine("❌ iOS testing requires macOS");
            return false;
        }

        Console.WriteLine("🍎 Running tests on iOS (macOS native)...");

        // Check for Xcode
        var xcodeCheck = new ProcessStartInfo
        {
            FileName = "xcodebuild",
            Arguments = "-version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var xcodeProcess = Process.Start(xcodeCheck);
        if (xcodeProcess == null)
        {
            Console.WriteLine("❌ Xcode is not installed");
            Console.WriteLine("   Please install Xcode from the App Store");
            return false;
        }

        await xcodeProcess.WaitForExitAsync();
        if (xcodeProcess.ExitCode != 0)
        {
            Console.WriteLine("❌ Xcode is not installed");
            Console.WriteLine("   Please install Xcode from the App Store");
            return false;
        }

        // Run tests natively
        var cliProject = Path.Combine(ProjectRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        if (!File.Exists(cliProject))
        {
            Console.WriteLine($"❌ CLI project not found: {cliProject}");
            return false;
        }

        var resultsFile = Path.Combine(TestResultsDir, "ios-results.json");
        var logsFile = Path.Combine(TestResultsDir, "ios-logs.txt");

        var testArgs = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project Nexo.CLI.csproj -- test --format-json",
            WorkingDirectory = Path.Combine(ProjectRoot, "src", "Nexo.CLI"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var testProcess = Process.Start(testArgs);
        if (testProcess == null)
        {
            Console.WriteLine("❌ Failed to start test process");
            return false;
        }

        var output = await testProcess.StandardOutput.ReadToEndAsync();
        var error = await testProcess.StandardError.ReadToEndAsync();

        await testProcess.WaitForExitAsync();

        // Write logs
        await File.WriteAllTextAsync(logsFile, error);

        // Parse JSON from output
        var result = ParseTestResults(output);
        if (result != null)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(resultsFile, json);

            Console.WriteLine($"✅ iOS: {result.TotalTests} total, {result.PassedTests} passed, {result.FailedTests} failed");
            return result.FailedTests == 0;
        }

        Console.WriteLine("❌ No valid test results found");
        Console.WriteLine($"Output preview: {output.Substring(0, Math.Min(200, output.Length))}...");
        return false;
    }

    private static TestResults? ParseTestResults(string output)
    {
        // Try to find JSON in the output
        var jsonMatch = Regex.Match(output, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", RegexOptions.Singleline);
        if (jsonMatch.Success)
        {
            try
            {
                var json = jsonMatch.Value;
                var result = JsonSerializer.Deserialize<TestResults>(json);
                if (result != null && result.TotalTests > 0)
                {
                    return result;
                }
            }
            catch
            {
                // Try next match
            }
        }

        // Fallback: try to extract from text
        var passedMatch = Regex.Match(output, @"(\d+)\s+passed", RegexOptions.IgnoreCase);
        var failedMatch = Regex.Match(output, @"(\d+)\s+failed", RegexOptions.IgnoreCase);
        var totalMatch = Regex.Match(output, @"(\d+)\s+total", RegexOptions.IgnoreCase);

        if (passedMatch.Success && failedMatch.Success)
        {
            var passed = int.Parse(passedMatch.Groups[1].Value);
            var failed = int.Parse(failedMatch.Groups[1].Value);
            var total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : passed + failed;

            return new TestResults
            {
                TotalTests = total,
                PassedTests = passed,
                FailedTests = failed
            };
        }

        return null;
    }


    private static string GetProjectRoot()
    {
        var current = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(current);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "Nexo.CLI", "Nexo.CLI.csproj")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return current;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --all              Run on all desktop platforms");
        Console.WriteLine("  --mobile           Include mobile platforms (iOS, Android)");
        Console.WriteLine("  --quick            Skip build cache");
        Console.WriteLine("  --platform <name>   Run on specific platform (ubuntu, ios, android)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --                    # Linux only");
        Console.WriteLine("  dotnet run -- --mobile           # Linux + iOS + Android");
        Console.WriteLine("  dotnet run -- --platform ios     # iOS only");
        Console.WriteLine("  dotnet run -- --platform android # Android only");
    }

    private class TestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
    }
}

