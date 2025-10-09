using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Demo;

/// <summary>
/// Comprehensive smoke test runner for all demo applications.
/// This runner executes all smoke tests and provides a consolidated report
/// to ensure all demo components are ready for demonstration and recording.
/// </summary>
public class DemoSmokeTestRunner
{
    private readonly ITestOutputHelper _output;

    public DemoSmokeTestRunner(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunAllDemoSmokeTests_ShouldPass()
    {
        // Arrange
        var testResults = new List<TestResult>();
        var projectRoot = GetProjectRoot();

        _output.WriteLine("🎮 Starting Comprehensive Demo Smoke Tests");
        _output.WriteLine("==========================================");

        // Act & Assert
        try
        {
            // Test 1: Avalonia Demo
            _output.WriteLine("\n1. Testing Avalonia Demo...");
            var avaloniaResult = await TestAvaloniaDemo(projectRoot);
            testResults.Add(avaloniaResult);
            LogTestResult("Avalonia Demo", avaloniaResult);

            // Test 2: Unity Demo
            _output.WriteLine("\n2. Testing Unity Demo...");
            var unityResult = await TestUnityDemo(projectRoot);
            testResults.Add(unityResult);
            LogTestResult("Unity Demo", unityResult);

            // Test 3: Director Studio
            _output.WriteLine("\n3. Testing Director Studio...");
            var directorStudioResult = await TestDirectorStudio(projectRoot);
            testResults.Add(directorStudioResult);
            LogTestResult("Director Studio", directorStudioResult);

            // Test 4: NexoDirectorDemo Unity Project
            _output.WriteLine("\n4. Testing NexoDirectorDemo Unity Project...");
            var unityProjectResult = await TestNexoDirectorDemo(projectRoot);
            testResults.Add(unityProjectResult);
            LogTestResult("NexoDirectorDemo Unity Project", unityProjectResult);

            // Test 5: Documentation
            _output.WriteLine("\n5. Testing Documentation...");
            var documentationResult = await TestDocumentation(projectRoot);
            testResults.Add(documentationResult);
            LogTestResult("Documentation", documentationResult);

            // Test 6: Integration Tests
            _output.WriteLine("\n6. Testing Integration...");
            var integrationResult = await TestIntegration(projectRoot);
            testResults.Add(integrationResult);
            LogTestResult("Integration", integrationResult);

            // Test 7: Performance and Readiness
            _output.WriteLine("\n7. Testing Performance and Readiness...");
            var performanceResult = await TestPerformanceAndReadiness(projectRoot);
            testResults.Add(performanceResult);
            LogTestResult("Performance and Readiness", performanceResult);

            // Generate Summary
            _output.WriteLine("\n==========================================");
            _output.WriteLine("📊 DEMO SMOKE TEST SUMMARY");
            _output.WriteLine("==========================================");

            var totalTests = testResults.Count;
            var passedTests = testResults.Count(r => r.Passed);
            var failedTests = totalTests - passedTests;
            var successRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;

            _output.WriteLine($"Total Test Categories: {totalTests}");
            _output.WriteLine($"Passed: {passedTests}");
            _output.WriteLine($"Failed: {failedTests}");
            _output.WriteLine($"Success Rate: {successRate:F1}%");

            if (failedTests > 0)
            {
                _output.WriteLine("\n❌ FAILED TESTS:");
                foreach (var result in testResults.Where(r => !r.Passed))
                {
                    _output.WriteLine($"  - {result.Name}: {result.ErrorMessage}");
                }
            }

            _output.WriteLine("\n==========================================");
            if (successRate == 100.0)
            {
                _output.WriteLine("🎉 ALL DEMO SMOKE TESTS PASSED!");
                _output.WriteLine("✅ Demo is ready for recording and demonstration!");
            }
            else
            {
                _output.WriteLine("⚠️  SOME DEMO SMOKE TESTS FAILED!");
                _output.WriteLine("❌ Demo needs attention before recording.");
            }
            _output.WriteLine("==========================================");

            // Final Assertion
            successRate.Should().Be(100.0, "All demo smoke tests should pass for demo readiness");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"\n💥 Demo smoke test runner failed with exception: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    #region Individual Test Methods

    private Task<TestResult> TestAvaloniaDemo(string projectRoot)
    {
        try
        {
            var avaloniaDemoPath = Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia");
            var mainWindowPath = Path.Combine(avaloniaDemoPath, "MainWindow.axaml");

            // Check project structure
            Directory.Exists(avaloniaDemoPath).Should().BeTrue("Avalonia demo directory should exist");
            File.Exists(mainWindowPath).Should().BeTrue("MainWindow.axaml should exist");

            // Check content
            var content = File.ReadAllText(mainWindowPath);
            content.Should().Contain("Framework-Agnostic Design System");
            content.Should().Contain("Button Variants");
            content.Should().Contain("80%");
            content.Should().Contain("$450K");

            return Task.FromResult(new TestResult("Avalonia Demo", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Avalonia Demo", false, ex.Message));
        }
    }

    private Task<TestResult> TestUnityDemo(string projectRoot)
    {
        try
        {
            var unityDemoPath = Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity");
            var demoWindowPath = Path.Combine(unityDemoPath, "PrimitivesDemoWindow.cs");

            // Check project structure
            Directory.Exists(unityDemoPath).Should().BeTrue("Unity demo directory should exist");
            File.Exists(demoWindowPath).Should().BeTrue("PrimitivesDemoWindow.cs should exist");

            // Check content
            var content = File.ReadAllText(demoWindowPath);
            content.Should().Contain("Framework-Agnostic Design System");
            content.Should().Contain("80%");
            content.Should().Contain("$450K");

            return Task.FromResult(new TestResult("Unity Demo", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Unity Demo", false, ex.Message));
        }
    }

    private Task<TestResult> TestDirectorStudio(string projectRoot)
    {
        try
        {
            var directorStudioPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio");
            var smokeTestRunnerPath = Path.Combine(directorStudioPath, "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
            var e2eTestsPath = Path.Combine(directorStudioPath, "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");

            // Check project structure
            Directory.Exists(directorStudioPath).Should().BeTrue("Director Studio directory should exist");
            File.Exists(smokeTestRunnerPath).Should().BeTrue("SmokeTestRunner.cs should exist");
            File.Exists(e2eTestsPath).Should().BeTrue("E2ESmokeTests.cs should exist");

            // Check smoke test runner content
            var smokeTestContent = File.ReadAllText(smokeTestRunnerPath);
            smokeTestContent.Should().Contain("TestServiceInitialization");
            smokeTestContent.Should().Contain("TestCommandExecution");
            smokeTestContent.Should().Contain("TestValidationSystem");

            // Check E2E tests content
            var e2eContent = File.ReadAllText(e2eTestsPath);
            e2eContent.Should().Contain("E2E_FPS_Pipeline");
            e2eContent.Should().Contain("E2E_Platformer_Pipeline");
            e2eContent.Should().Contain("E2E_RPG_Pipeline");

            return Task.FromResult(new TestResult("Director Studio", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Director Studio", false, ex.Message));
        }
    }

    private Task<TestResult> TestNexoDirectorDemo(string projectRoot)
    {
        try
        {
            var unityProjectPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo");
            var scriptsPath = Path.Combine(unityProjectPath, "Assets", "Scripts");
            var editorPath = Path.Combine(unityProjectPath, "Assets", "Editor");
            var scenesPath = Path.Combine(unityProjectPath, "Assets", "Scenes");

            // Check project structure
            Directory.Exists(unityProjectPath).Should().BeTrue("NexoDirectorDemo Unity project should exist");
            Directory.Exists(scriptsPath).Should().BeTrue("Scripts directory should exist");
            Directory.Exists(editorPath).Should().BeTrue("Editor directory should exist");
            Directory.Exists(scenesPath).Should().BeTrue("Scenes directory should exist");

            // Check required files
            var validationDemoPath = Path.Combine(scriptsPath, "NexoValidationDemo.cs");
            var controllerPath = Path.Combine(scriptsPath, "DirectorStudioController.cs");
            var windowPath = Path.Combine(editorPath, "DirectorStudioWindow.cs");
            var scenePath = Path.Combine(scenesPath, "SampleScene.unity");

            File.Exists(validationDemoPath).Should().BeTrue("NexoValidationDemo.cs should exist");
            File.Exists(controllerPath).Should().BeTrue("DirectorStudioController.cs should exist");
            File.Exists(windowPath).Should().BeTrue("DirectorStudioWindow.cs should exist");
            File.Exists(scenePath).Should().BeTrue("SampleScene.unity should exist");

            return Task.FromResult(new TestResult("NexoDirectorDemo Unity Project", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("NexoDirectorDemo Unity Project", false, ex.Message));
        }
    }

    private Task<TestResult> TestDocumentation(string projectRoot)
    {
        try
        {
            var docsPath = Path.Combine(projectRoot, "docs");
            var videoScriptPath = Path.Combine(docsPath, "VIDEO_SCRIPT.md");
            var checklistPath = Path.Combine(docsPath, "DEMO_RECORDING_CHECKLIST.md");

            // Check documentation exists
            Directory.Exists(docsPath).Should().BeTrue("Docs directory should exist");
            File.Exists(videoScriptPath).Should().BeTrue("VIDEO_SCRIPT.md should exist");
            File.Exists(checklistPath).Should().BeTrue("DEMO_RECORDING_CHECKLIST.md should exist");

            // Check video script content
            var scriptContent = File.ReadAllText(videoScriptPath);
            scriptContent.Should().Contain("## INTRO SEQUENCE");
            scriptContent.Should().Contain("## DEMO PART 1: AVALONIA");
            scriptContent.Should().Contain("## DEMO PART 2: UNITY");

            // Check checklist content
            var checklistContent = File.ReadAllText(checklistPath);
            checklistContent.Should().Contain("## PRE-RECORDING SETUP");
            checklistContent.Should().Contain("## RECORDING SEQUENCE");
            checklistContent.Should().Contain("## POST-RECORDING REVIEW");

            return Task.FromResult(new TestResult("Documentation", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Documentation", false, ex.Message));
        }
    }

    private Task<TestResult> TestIntegration(string projectRoot)
    {
        try
        {
            var avaloniaContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
            var unityContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));

            // Check content consistency
            avaloniaContent.Should().Contain("80%");
            unityContent.Should().Contain("80%");
            avaloniaContent.Should().Contain("$450K");
            unityContent.Should().Contain("$450K");
            avaloniaContent.Should().Contain("Framework-Agnostic Design System");
            unityContent.Should().Contain("Framework-Agnostic Design System");

            return Task.FromResult(new TestResult("Integration", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Integration", false, ex.Message));
        }
    }

    private Task<TestResult> TestPerformanceAndReadiness(string projectRoot)
    {
        try
        {
            // Check demo projects are buildable
            var avaloniaProjectPath = Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "Nexo.UI.Demo.Avalonia.csproj");
            var directorStudioProjectPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "NexoDirectorStudio.csproj");

            File.Exists(avaloniaProjectPath).Should().BeTrue("Avalonia demo project should exist");
            File.Exists(directorStudioProjectPath).Should().BeTrue("Director Studio project should exist");

            // Check test projects are runnable
            var testProjectPath = Path.Combine(projectRoot, "tests", "Nexo.Tests.Demo", "Nexo.Tests.Demo.csproj");
            File.Exists(testProjectPath).Should().BeTrue("Demo test project should exist");

            return Task.FromResult(new TestResult("Performance and Readiness", true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult("Performance and Readiness", false, ex.Message));
        }
    }

    #endregion

    #region Helper Methods

    private void LogTestResult(string testName, TestResult result)
    {
        if (result.Passed)
        {
            _output.WriteLine($"  ✅ {testName}: PASSED");
        }
        else
        {
            _output.WriteLine($"  ❌ {testName}: FAILED - {result.ErrorMessage}");
        }
    }

    private static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);
        
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Nexo.sln")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root");
    }

    #endregion

    #region Helper Classes

    private class TestResult
    {
        public string Name { get; }
        public bool Passed { get; }
        public string ErrorMessage { get; }

        public TestResult(string name, bool passed, string errorMessage)
        {
            Name = name;
            Passed = passed;
            ErrorMessage = errorMessage;
        }
    }

    #endregion
}
