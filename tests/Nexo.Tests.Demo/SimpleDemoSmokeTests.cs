using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Demo;

/// <summary>
/// Simplified smoke tests for the demo applications to ensure they're ready for recording.
/// These tests validate the essential components without complex assertions.
/// </summary>
public class SimpleDemoSmokeTests
{
    [Fact]
    public void AvaloniaDemo_ShouldExist()
    {
        // Arrange
        var projectPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "Nexo.UI.Demo.Avalonia.csproj");
        var mainWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml");
        var appPath = Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "App.axaml");
        
        // Act & Assert
        File.Exists(projectPath).Should().BeTrue($"Demo project should exist at {projectPath}");
        File.Exists(mainWindowPath).Should().BeTrue("MainWindow.axaml should exist");
        File.Exists(appPath).Should().BeTrue("App.axaml should exist");
    }

    [Fact]
    public void UnityDemo_ShouldExist()
    {
        // Arrange
        var demoWindowPath = Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs");
        
        // Act & Assert
        File.Exists(demoWindowPath).Should().BeTrue($"Unity demo window should exist at {demoWindowPath}");
    }

    [Fact]
    public void VideoScript_ShouldExist()
    {
        // Arrange
        var scriptPath = Path.Combine(GetProjectRoot(), "docs", "VIDEO_SCRIPT.md");
        
        // Act & Assert
        File.Exists(scriptPath).Should().BeTrue($"Video script should exist at {scriptPath}");
    }

    [Fact]
    public void RecordingChecklist_ShouldExist()
    {
        // Arrange
        var checklistPath = Path.Combine(GetProjectRoot(), "docs", "DEMO_RECORDING_CHECKLIST.md");
        
        // Act & Assert
        File.Exists(checklistPath).Should().BeTrue($"Recording checklist should exist at {checklistPath}");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveCorrectTitle()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("Forge - Framework-Agnostic Design System | Avalonia Implementation");
    }

    [Fact]
    public void UnityDemo_ShouldHaveCorrectTitle()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("Forge Design System - Unity");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveKeyMetrics()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("80%");
        mainWindowContent.Should().Contain("$450K");
        mainWindowContent.Should().Contain("20h");
    }

    [Fact]
    public void UnityDemo_ShouldHaveKeyMetrics()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("80%");
        demoWindowContent.Should().Contain("$450K");
        demoWindowContent.Should().Contain("20h");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveAllButtonVariants()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("Primary - Default action");
        mainWindowContent.Should().Contain("Secondary - Alternative action");
        mainWindowContent.Should().Contain("Success - Confirm action");
        mainWindowContent.Should().Contain("Warning - Caution required");
        mainWindowContent.Should().Contain("Error - Destructive action");
        mainWindowContent.Should().Contain("Info - Informational");
        mainWindowContent.Should().Contain("Disabled - Unavailable");
    }

    [Fact]
    public void UnityDemo_ShouldHaveAllButtonVariants()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("Primary - Default action");
        demoWindowContent.Should().Contain("Secondary - Alternative action");
        demoWindowContent.Should().Contain("Success - Confirm action");
        demoWindowContent.Should().Contain("Warning - Caution required");
        demoWindowContent.Should().Contain("Error - Destructive action");
        demoWindowContent.Should().Contain("Info - Informational");
        demoWindowContent.Should().Contain("Disabled - Unavailable");
    }

    [Fact]
    public void VideoScript_ShouldHaveCorrectDuration()
    {
        // Arrange
        var scriptContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "VIDEO_SCRIPT.md"));
        
        // Act & Assert
        scriptContent.Should().Contain("Total Duration: 3-4 minutes");
    }

    [Fact]
    public void VideoScript_ShouldHaveAllSections()
    {
        // Arrange
        var scriptContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "VIDEO_SCRIPT.md"));
        
        // Act & Assert
        scriptContent.Should().Contain("## INTRO SEQUENCE");
        scriptContent.Should().Contain("## PROBLEM STATEMENT");
        scriptContent.Should().Contain("## SOLUTION OVERVIEW");
        scriptContent.Should().Contain("## DEMO PART 1: AVALONIA");
        scriptContent.Should().Contain("## DEMO PART 2: UNITY");
        scriptContent.Should().Contain("## CODE WALKTHROUGH");
        scriptContent.Should().Contain("## IMPACT & FUTURE");
        scriptContent.Should().Contain("## CLOSING");
    }

    [Fact]
    public void RecordingChecklist_ShouldHaveAllSections()
    {
        // Arrange
        var checklistContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "DEMO_RECORDING_CHECKLIST.md"));
        
        // Act & Assert
        checklistContent.Should().Contain("## PRE-RECORDING SETUP");
        checklistContent.Should().Contain("## RECORDING SEQUENCE");
        checklistContent.Should().Contain("## POST-RECORDING REVIEW");
        checklistContent.Should().Contain("## EDITING CHECKLIST");
        checklistContent.Should().Contain("## EXPORT SETTINGS");
        checklistContent.Should().Contain("## DISTRIBUTION CHECKLIST");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("Width=\"1200\"");
        mainWindowContent.Should().Contain("Height=\"800\"");
    }

    [Fact]
    public void UnityDemo_ShouldHaveCorrectWindowSize()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("minSize = new Vector2(800, 600)");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveArchitectureSection()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("Design Tokens");
        mainWindowContent.Should().Contain("Primitives");
        mainWindowContent.Should().Contain("Renderers");
    }

    [Fact]
    public void UnityDemo_ShouldHaveArchitectureSection()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("Design Tokens");
        demoWindowContent.Should().Contain("Primitives");
        demoWindowContent.Should().Contain("Renderers");
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveProfessionalFooter()
    {
        // Arrange
        var mainWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        
        // Act & Assert
        mainWindowContent.Should().Contain("Built with framework-agnostic patterns");
        mainWindowContent.Should().Contain("This demo proves cross-framework pattern extraction works");
    }

    [Fact]
    public void UnityDemo_ShouldHaveProfessionalFooter()
    {
        // Arrange
        var demoWindowContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        demoWindowContent.Should().Contain("Built with framework-agnostic patterns");
        demoWindowContent.Should().Contain("This demo proves cross-framework pattern extraction works");
    }

    [Fact]
    public void VideoScript_ShouldHaveKeyMetrics()
    {
        // Arrange
        var scriptContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "VIDEO_SCRIPT.md"));
        
        // Act & Assert
        scriptContent.Should().Contain("80%");
        scriptContent.Should().Contain("$450K");
        scriptContent.Should().Contain("20 hours");
    }

    [Fact]
    public void RecordingChecklist_ShouldHaveTimeEstimates()
    {
        // Arrange
        var checklistContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "DEMO_RECORDING_CHECKLIST.md"));
        
        // Act & Assert
        checklistContent.Should().Contain("Time Estimates");
        checklistContent.Should().Contain("6-9 hours");
    }

    [Fact]
    public void RecordingChecklist_ShouldHaveSuccessCriteria()
    {
        // Arrange
        var checklistContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "docs", "DEMO_RECORDING_CHECKLIST.md"));
        
        // Act & Assert
        checklistContent.Should().Contain("Success Criteria");
        checklistContent.Should().Contain("READY TO RECORD");
    }

    [Fact]
    public void Documentation_ShouldBeComplete()
    {
        // Arrange
        var docsPath = Path.Combine(GetProjectRoot(), "docs");
        var requiredDocs = new[]
        {
            "VIDEO_SCRIPT.md",
            "DEMO_RECORDING_CHECKLIST.md",
            "METRICS.md",
            "DESIGN_DECISIONS.md"
        };
        
        // Act & Assert
        foreach (var doc in requiredDocs)
        {
            var docPath = Path.Combine(docsPath, doc);
            File.Exists(docPath).Should().BeTrue($"Required documentation {doc} should exist");
        }
    }

    [Fact]
    public void AvaloniaDemo_ShouldHaveConsistentContentWithUnity()
    {
        // Arrange
        var avaloniaContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia", "MainWindow.axaml"));
        var unityContent = File.ReadAllText(Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity", "PrimitivesDemoWindow.cs"));
        
        // Act & Assert
        // Both should have the same key metrics
        avaloniaContent.Should().Contain("80%");
        unityContent.Should().Contain("80%");
        
        avaloniaContent.Should().Contain("$450K");
        unityContent.Should().Contain("$450K");
        
        // Both should have the same main header
        avaloniaContent.Should().Contain("Framework-Agnostic Design System");
        unityContent.Should().Contain("Framework-Agnostic Design System");
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
}
