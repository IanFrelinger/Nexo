using System.IO;
using Xunit.Abstractions;

namespace Nexo.Tests.Shared.Infrastructure;

/// <summary>
/// Base class for demo-related tests providing common demo paths and assertions.
/// </summary>
public abstract class DemoTestBase : TestBase
{
    protected DemoTestBase(ITestOutputHelper? output = null) : base(output) { }

    /// <summary>
    /// Gets the path to the Avalonia demo project.
    /// </summary>
    protected static string GetAvaloniaDemoPath()
    {
        return Path.Combine(GetProjectRoot(), "src", "Nexo.UI.Demo.Avalonia");
    }

    /// <summary>
    /// Gets the path to the Unity demo project.
    /// </summary>
    protected static string GetUnityDemoPath()
    {
        return Path.Combine(GetProjectRoot(), "src", "Nexo.Core.UI.Unity", "Frameworks", "Unity");
    }

    /// <summary>
    /// Gets the path to the Director Studio project.
    /// </summary>
    protected static string GetDirectorStudioPath()
    {
        return Path.Combine(GetProjectRoot(), "src", "NexoDirectorStudio");
    }

    /// <summary>
    /// Gets the path to the NexoDirectorDemo Unity project.
    /// </summary>
    protected static string GetNexoDirectorDemoPath()
    {
        return Path.Combine(GetProjectRoot(), "UnityProjects", "NexoDirectorDemo");
    }

    /// <summary>
    /// Gets the path to the docs directory.
    /// </summary>
    protected static string GetDocsPath()
    {
        return Path.Combine(GetProjectRoot(), "docs");
    }

    /// <summary>
    /// Asserts that Avalonia demo structure is complete.
    /// </summary>
    protected static void AssertAvaloniaDemoStructure()
    {
        var demoPath = GetAvaloniaDemoPath();
        var requiredFiles = new[]
        {
            "Nexo.UI.Demo.Avalonia.csproj",
            "MainWindow.axaml",
            "MainWindow.axaml.cs",
            "App.axaml",
            "App.axaml.cs",
            "Program.cs"
        };

        foreach (var file in requiredFiles)
        {
            AssertFileExists(Path.Combine(demoPath, file));
        }
    }

    /// <summary>
    /// Asserts that Unity demo structure is complete.
    /// </summary>
    protected static void AssertUnityDemoStructure()
    {
        var demoPath = GetUnityDemoPath();
        AssertFileExists(Path.Combine(demoPath, "PrimitivesDemoWindow.cs"));
    }

    /// <summary>
    /// Asserts that Director Studio structure is complete.
    /// </summary>
    protected static void AssertDirectorStudioStructure()
    {
        var studioPath = GetDirectorStudioPath();
        var requiredPaths = new[]
        {
            Path.Combine(studioPath, "Assets"),
            Path.Combine(studioPath, "Assets", "NexoDirectorStudio"),
            Path.Combine(studioPath, "Assets", "NexoDirectorStudio", "Tests"),
            Path.Combine(studioPath, "Assets", "NexoDirectorStudio", "Tests", "EditMode"),
            Path.Combine(studioPath, "Assets", "NexoDirectorStudio", "Tests", "PlayMode")
        };

        AssertDirectoriesExist(requiredPaths);
    }

    /// <summary>
    /// Asserts that NexoDirectorDemo Unity project structure is complete.
    /// </summary>
    protected static void AssertNexoDirectorDemoStructure()
    {
        var projectPath = GetNexoDirectorDemoPath();
        var requiredPaths = new[]
        {
            Path.Combine(projectPath, "Assets"),
            Path.Combine(projectPath, "Assets", "Scripts"),
            Path.Combine(projectPath, "Assets", "Editor"),
            Path.Combine(projectPath, "Assets", "Scenes"),
            Path.Combine(projectPath, "Packages"),
            Path.Combine(projectPath, "ProjectSettings")
        };

        AssertDirectoriesExist(requiredPaths);
    }

    /// <summary>
    /// Asserts that documentation structure is complete.
    /// </summary>
    protected static void AssertDocumentationStructure()
    {
        var docsPath = GetDocsPath();
        var requiredFiles = new[]
        {
            "VIDEO_SCRIPT.md",
            "DEMO_RECORDING_CHECKLIST.md"
        };

        foreach (var file in requiredFiles)
        {
            AssertFileExists(Path.Combine(docsPath, file));
        }
    }
}
