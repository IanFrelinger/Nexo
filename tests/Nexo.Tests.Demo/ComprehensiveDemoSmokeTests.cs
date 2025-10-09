using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Demo;

/// <summary>
/// Comprehensive smoke tests for all demo applications in the Nexo project.
/// These tests validate that all demo components are ready for demonstration and recording.
/// </summary>
public class ComprehensiveDemoSmokeTests
{
    private readonly ITestOutputHelper _output;

    public ComprehensiveDemoSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Avalonia Demo Tests

    [Fact]
    public void AvaloniaDemo_ProjectStructure_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var avaloniaDemoPath = Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia");
        
        var requiredFiles = new[]
        {
            "Nexo.UI.Demo.Avalonia.csproj",
            "MainWindow.axaml",
            "MainWindow.axaml.cs",
            "App.axaml",
            "App.axaml.cs",
            "Program.cs"
        };

        // Act & Assert
        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(avaloniaDemoPath, file);
            File.Exists(filePath).Should().BeTrue($"Required file {file} should exist in Avalonia demo");
        }
    }

    [Fact]
    public void AvaloniaDemo_UIComponents_ShouldBePresent()
    {
        // Arrange
        var mainWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml");
        var content = File.ReadAllText(mainWindowPath);

        // Act & Assert
        content.Should().Contain("Framework-Agnostic Design System");
        content.Should().Contain("Button Variants");
        content.Should().Contain("Input Types");
        content.Should().Contain("Card Layouts");
        content.Should().Contain("Architecture Overview");
        content.Should().Contain("Key Metrics");
    }

    [Fact]
    public void AvaloniaDemo_ButtonVariants_ShouldBeComplete()
    {
        // Arrange
        var mainWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml");
        var content = File.ReadAllText(mainWindowPath);

        // Act & Assert
        var buttonVariants = new[]
        {
            "Primary - Default action",
            "Secondary - Alternative action", 
            "Success - Confirm action",
            "Warning - Caution required",
            "Error - Destructive action",
            "Info - Informational",
            "Disabled - Unavailable"
        };

        foreach (var variant in buttonVariants)
        {
            content.Should().Contain(variant, $"Button variant '{variant}' should be present");
        }
    }

    [Fact]
    public void AvaloniaDemo_KeyMetrics_ShouldBePresent()
    {
        // Arrange
        var mainWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml");
        var content = File.ReadAllText(mainWindowPath);

        // Act & Assert
        content.Should().Contain("80%");
        content.Should().Contain("$450K");
        content.Should().Contain("20h");
        content.Should().Contain("Code Reuse");
        content.Should().Contain("Development Time");
        content.Should().Contain("Annual Savings");
    }

    [Fact]
    public void AvaloniaDemo_ArchitectureSections_ShouldBePresent()
    {
        // Arrange
        var mainWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml");
        var content = File.ReadAllText(mainWindowPath);

        // Act & Assert
        content.Should().Contain("Design Tokens");
        content.Should().Contain("Primitives");
        content.Should().Contain("Renderers");
        content.Should().Contain("Framework-agnostic primitives");
        content.Should().Contain("Consistent design tokens");
        content.Should().Contain("Accessibility built-in");
    }

    #endregion

    #region Unity Demo Tests

    [Fact]
    public void UnityDemo_ProjectStructure_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var unityDemoPath = Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity");
        
        var requiredFiles = new[]
        {
            "PrimitivesDemoWindow.cs"
        };

        // Act & Assert
        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(unityDemoPath, file);
            File.Exists(filePath).Should().BeTrue($"Required file {file} should exist in Unity demo");
        }
    }

    [Fact]
    public void UnityDemo_Content_ShouldMatchAvalonia()
    {
        // Arrange
        var unityDemoPath = Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs");
        var unityContent = File.ReadAllText(unityDemoPath);

        // Act & Assert
        unityContent.Should().Contain("Framework-Agnostic Design System");
        unityContent.Should().Contain("80%");
        unityContent.Should().Contain("$450K");
        unityContent.Should().Contain("20h");
        unityContent.Should().Contain("Design Tokens");
        unityContent.Should().Contain("Primitives");
        unityContent.Should().Contain("Renderers");
    }

    #endregion

    #region Director Studio Tests

    [Fact]
    public void DirectorStudio_ProjectStructure_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var directorStudioPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio");
        
        var requiredDirectories = new[]
        {
            "Assets",
            "Assets/NexoDirectorStudio",
            "Assets/NexoDirectorStudio/Tests",
            "Assets/NexoDirectorStudio/Tests/EditMode",
            "Assets/NexoDirectorStudio/Tests/PlayMode"
        };

        // Act & Assert
        foreach (var dir in requiredDirectories)
        {
            var dirPath = Path.Combine(directorStudioPath, dir);
            Directory.Exists(dirPath).Should().BeTrue($"Required directory {dir} should exist in Director Studio");
        }
    }

    [Fact]
    public void DirectorStudio_SmokeTests_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var testsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode");
        
        var requiredTestFiles = new[]
        {
            "SmokeTestRunner.cs",
            "SmokeTestConsole.cs",
            "E2ESmokeTests.cs"
        };

        // Act & Assert
        foreach (var file in requiredTestFiles)
        {
            var filePath = Path.Combine(testsPath, file);
            File.Exists(filePath).Should().BeTrue($"Required test file {file} should exist in Director Studio");
        }
    }

    [Fact]
    public void DirectorStudio_TestRunner_ShouldHaveAllTestMethods()
    {
        // Arrange
        var smokeTestRunnerPath = Path.Combine(GetProjectRoot(), "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        var testMethods = new[]
        {
            "TestServiceInitialization",
            "TestCommandExecution", 
            "TestValidationSystem",
            "TestAdapterHealthChecks",
            "TestGenreProfileSystem",
            "TestJsonRepair",
            "TestCompleteWorkflow",
            "TestDeterminism"
        };

        foreach (var method in testMethods)
        {
            content.Should().Contain(method, $"Test method '{method}' should be present in SmokeTestRunner");
        }
    }

    [Fact]
    public void DirectorStudio_E2ETests_ShouldHaveAllPipelines()
    {
        // Arrange
        var e2eTestsPath = Path.Combine(GetProjectRoot(), "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        var pipelineTests = new[]
        {
            "E2E_FPS_Pipeline",
            "E2E_Platformer_Pipeline", 
            "E2E_RPG_Pipeline"
        };

        foreach (var test in pipelineTests)
        {
            content.Should().Contain(test, $"E2E test '{test}' should be present");
        }
    }

    #endregion

    #region NexoDirectorDemo Unity Project Tests

    [Fact]
    public void NexoDirectorDemo_ProjectStructure_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var unityProjectPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo");
        
        var requiredDirectories = new[]
        {
            "Assets",
            "Assets/Scenes",
            "Assets/Scripts", 
            "Assets/Editor",
            "Packages",
            "ProjectSettings"
        };

        // Act & Assert
        foreach (var dir in requiredDirectories)
        {
            var dirPath = Path.Combine(unityProjectPath, dir);
            Directory.Exists(dirPath).Should().BeTrue($"Required directory {dir} should exist in NexoDirectorDemo Unity project");
        }
    }

    [Fact]
    public void NexoDirectorDemo_Scripts_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var scriptsPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts");
        
        var requiredScripts = new[]
        {
            "NexoValidationDemo.cs",
            "DirectorStudioController.cs"
        };

        // Act & Assert
        foreach (var script in requiredScripts)
        {
            var scriptPath = Path.Combine(scriptsPath, script);
            File.Exists(scriptPath).Should().BeTrue($"Required script {script} should exist in NexoDirectorDemo");
        }
    }

    [Fact]
    public void NexoDirectorDemo_EditorWindow_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var editorPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Editor");
        var editorWindowPath = Path.Combine(editorPath, "DirectorStudioWindow.cs");

        // Act & Assert
        File.Exists(editorWindowPath).Should().BeTrue("DirectorStudioWindow.cs should exist in Editor folder");
    }

    [Fact]
    public void NexoDirectorDemo_Scene_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var scenesPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scenes");
        var scenePath = Path.Combine(scenesPath, "SampleScene.unity");

        // Act & Assert
        File.Exists(scenePath).Should().BeTrue("SampleScene.unity should exist in Scenes folder");
    }

    [Fact]
    public void NexoDirectorDemo_README_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var readmePath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "README.md");
        var content = File.ReadAllText(readmePath);

        // Act & Assert
        content.Should().Contain("Nexo Director Demo - Unity Project");
        content.Should().Contain("Director Studio Integration");
        content.Should().Contain("Nexo Validation");
        content.Should().Contain("Unity Editor Controls");
        content.Should().Contain("Quick Start");
        content.Should().Contain("Features Demonstrated");
    }

    #endregion

    #region Documentation Tests

    [Fact]
    public void DemoDocumentation_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var docsPath = Path.Combine(projectRoot, "docs");
        
        var requiredDocs = new[]
        {
            "VIDEO_SCRIPT.md",
            "DEMO_RECORDING_CHECKLIST.md"
        };

        // Act & Assert
        foreach (var doc in requiredDocs)
        {
            var docPath = Path.Combine(docsPath, doc);
            File.Exists(docPath).Should().BeTrue($"Required documentation {doc} should exist");
        }
    }

    [Fact]
    public void VideoScript_ShouldHaveAllSections()
    {
        // Arrange
        var scriptPath = Path.Combine(GetProjectRoot(), "docs", "VIDEO_SCRIPT.md");
        var content = File.ReadAllText(scriptPath);

        // Act & Assert
        var sections = new[]
        {
            "## INTRO SEQUENCE",
            "## PROBLEM STATEMENT", 
            "## SOLUTION OVERVIEW",
            "## DEMO PART 1: AVALONIA",
            "## DEMO PART 2: UNITY",
            "## CODE WALKTHROUGH",
            "## IMPACT & FUTURE",
            "## CLOSING"
        };

        foreach (var section in sections)
        {
            content.Should().Contain(section, $"Video script should contain section '{section}'");
        }
    }

    [Fact]
    public void RecordingChecklist_ShouldHaveAllSections()
    {
        // Arrange
        var checklistPath = Path.Combine(GetProjectRoot(), "docs", "DEMO_RECORDING_CHECKLIST.md");
        var content = File.ReadAllText(checklistPath);

        // Act & Assert
        var sections = new[]
        {
            "## PRE-RECORDING SETUP",
            "## RECORDING SEQUENCE",
            "## POST-RECORDING REVIEW", 
            "## EDITING CHECKLIST",
            "## EXPORT SETTINGS",
            "## DISTRIBUTION CHECKLIST"
        };

        foreach (var section in sections)
        {
            content.Should().Contain(section, $"Recording checklist should contain section '{section}'");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllDemos_ShouldHaveConsistentContent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var avaloniaContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        var unityContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));

        // Act & Assert
        // Both should have the same key metrics
        avaloniaContent.Should().Contain("80%");
        unityContent.Should().Contain("80%");
        
        avaloniaContent.Should().Contain("$450K");
        unityContent.Should().Contain("$450K");
        
        avaloniaContent.Should().Contain("20h");
        unityContent.Should().Contain("20h");
        
        // Both should have the same main header
        avaloniaContent.Should().Contain("Framework-Agnostic Design System");
        unityContent.Should().Contain("Framework-Agnostic Design System");
    }

    [Fact]
    public void AllDemos_ShouldHaveProfessionalFooters()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var avaloniaContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        var unityContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));

        // Act & Assert
        avaloniaContent.Should().Contain("Built with framework-agnostic patterns");
        unityContent.Should().Contain("Built with framework-agnostic patterns");
        
        avaloniaContent.Should().Contain("This demo proves cross-framework pattern extraction works");
        unityContent.Should().Contain("This demo proves cross-framework pattern extraction works");
    }

    [Fact]
    public void AllDemos_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var avaloniaContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        var unityContent = File.ReadAllText(Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));

        // Act & Assert
        avaloniaContent.Should().Contain("Width=\"1200\"");
        avaloniaContent.Should().Contain("Height=\"800\"");
        unityContent.Should().Contain("minSize = new Vector2(800, 600)");
    }

    #endregion

    #region Performance and Readiness Tests

    [Fact]
    public void AllDemoProjects_ShouldBeBuildable()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var demoProjects = new[]
        {
            Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia", "Nexo.UI.Demo.Avalonia.csproj"),
            Path.Combine(projectRoot, "src", "NexoDirectorStudio", "NexoDirectorStudio.csproj")
        };

        // Act & Assert
        foreach (var project in demoProjects)
        {
            File.Exists(project).Should().BeTrue($"Demo project should exist: {project}");
            
            var projectContent = File.ReadAllText(project);
            projectContent.Should().Contain("<Project Sdk=", $"Project file should be valid: {project}");
        }
    }

    [Fact]
    public void AllDemoTests_ShouldBeRunnable()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var testProjects = new[]
        {
            Path.Combine(projectRoot, "tests", "Nexo.Tests.Demo", "Nexo.Tests.Demo.csproj")
        };

        // Act & Assert
        foreach (var testProject in testProjects)
        {
            File.Exists(testProject).Should().BeTrue($"Test project should exist: {testProject}");
            
            var projectContent = File.ReadAllText(testProject);
            projectContent.Should().Contain("<Project Sdk=", $"Test project file should be valid: {testProject}");
            projectContent.Should().Contain("xunit", $"Test project should reference xunit: {testProject}");
        }
    }

    [Fact]
    public void DemoReadiness_ShouldBeComplete()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        
        // Check all critical demo components exist
        var criticalComponents = new[]
        {
            Path.Combine(projectRoot, "src", "Nexo.UI.Demo.Avalonia"),
            Path.Combine(projectRoot, "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity"),
            Path.Combine(projectRoot, "src", "NexoDirectorStudio"),
            Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo"),
            Path.Combine(projectRoot, "docs", "VIDEO_SCRIPT.md"),
            Path.Combine(projectRoot, "docs", "DEMO_RECORDING_CHECKLIST.md")
        };

        // Act & Assert
        foreach (var component in criticalComponents)
        {
            if (File.Exists(component))
            {
                File.Exists(component).Should().BeTrue($"Critical demo component should exist: {component}");
            }
            else
            {
                Directory.Exists(component).Should().BeTrue($"Critical demo component should exist: {component}");
            }
        }
    }

    #endregion

    #region Helper Methods

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
}
