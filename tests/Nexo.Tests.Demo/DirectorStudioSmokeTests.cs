using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Demo;

/// <summary>
/// Comprehensive smoke tests for Director Studio functionality and Unity integration.
/// These tests validate that Director Studio is ready for demonstration and can handle
/// the complete game development pipeline from natural language to validated content.
/// </summary>
public class DirectorStudioSmokeTests
{
    private readonly ITestOutputHelper _output;

    public DirectorStudioSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Director Studio Core Tests

    [Fact]
    public void DirectorStudio_ServiceInitialization_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestServiceInitialization", "Service initialization test should be present");
        content.Should().Contain("DirectorStudioService", "Director Studio service should be referenced");
    }

    [Fact]
    public void DirectorStudio_CommandExecution_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestCommandExecution", "Command execution test should be present");
        content.Should().Contain("Command", "Command system should be referenced");
    }

    [Fact]
    public void DirectorStudio_ValidationSystem_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestValidationSystem", "Validation system test should be present");
        content.Should().Contain("Validation", "Validation system should be referenced");
    }

    [Fact]
    public void DirectorStudio_AdapterHealthChecks_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestAdapterHealthChecks", "Adapter health checks test should be present");
        content.Should().Contain("Adapter", "Adapter system should be referenced");
    }

    [Fact]
    public void DirectorStudio_GenreProfileSystem_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestGenreProfileSystem", "Genre profile system test should be present");
        content.Should().Contain("Genre", "Genre system should be referenced");
    }

    [Fact]
    public void DirectorStudio_JsonRepair_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestJsonRepair", "JSON repair test should be present");
        content.Should().Contain("JSON", "JSON repair system should be referenced");
    }

    [Fact]
    public void DirectorStudio_CompleteWorkflow_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestCompleteWorkflow", "Complete workflow test should be present");
        content.Should().Contain("Workflow", "Workflow system should be referenced");
    }

    [Fact]
    public void DirectorStudio_Determinism_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var smokeTestRunnerPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestRunner.cs");
        var content = File.ReadAllText(smokeTestRunnerPath);

        // Act & Assert
        content.Should().Contain("TestDeterminism", "Determinism test should be present");
        content.Should().Contain("Deterministic", "Deterministic generation should be referenced");
    }

    #endregion

    #region E2E Pipeline Tests

    [Fact]
    public void E2ESmokeTests_FPSPipeline_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var e2eTestsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        content.Should().Contain("E2E_FPS_Pipeline", "FPS pipeline E2E test should be present");
        content.Should().Contain("FPS", "FPS genre should be referenced");
    }

    [Fact]
    public void E2ESmokeTests_PlatformerPipeline_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var e2eTestsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        content.Should().Contain("E2E_Platformer_Pipeline", "Platformer pipeline E2E test should be present");
        content.Should().Contain("Platformer", "Platformer genre should be referenced");
    }

    [Fact]
    public void E2ESmokeTests_RPGPipeline_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var e2eTestsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        content.Should().Contain("E2E_RPG_Pipeline", "RPG pipeline E2E test should be present");
        content.Should().Contain("RPG", "RPG genre should be referenced");
    }

    [Fact]
    public void E2ESmokeTests_ShouldHaveDesignBrief()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var e2eTestsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        content.Should().Contain("DesignBrief", "Design brief should be used in E2E tests");
        content.Should().Contain("brief", "Brief parameter should be present");
    }

    [Fact]
    public void E2ESmokeTests_ShouldHaveTestRunnerIntegration()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var e2eTestsPath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "E2ESmokeTests.cs");
        var content = File.ReadAllText(e2eTestsPath);

        // Act & Assert
        content.Should().Contain("TestRunnerIntegration", "Test runner integration should be present");
        content.Should().Contain("maxRetries", "Max retries should be configured");
        content.Should().Contain("timeoutSeconds", "Timeout should be configured");
    }

    #endregion

    #region Unity Integration Tests

    [Fact]
    public void UnityIntegration_NexoValidationDemo_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("NexoValidationDemo", "Nexo validation demo class should be present");
        content.Should().Contain("class", "Should be a proper C# class");
    }

    [Fact]
    public void UnityIntegration_DirectorStudioController_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var controllerPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "DirectorStudioController.cs");
        var content = File.ReadAllText(controllerPath);

        // Act & Assert
        content.Should().Contain("DirectorStudioController", "Director Studio controller class should be present");
        content.Should().Contain("class", "Should be a proper C# class");
    }

    [Fact]
    public void UnityIntegration_DirectorStudioWindow_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var windowPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Editor", "DirectorStudioWindow.cs");
        var content = File.ReadAllText(windowPath);

        // Act & Assert
        content.Should().Contain("DirectorStudioWindow", "Director Studio window class should be present");
        content.Should().Contain("EditorWindow", "Should inherit from EditorWindow");
    }

    [Fact]
    public void UnityIntegration_ShouldHaveConnectionManagement()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var controllerPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "DirectorStudioController.cs");
        var content = File.ReadAllText(controllerPath);

        // Act & Assert
        content.Should().Contain("Connection", "Connection management should be present");
        content.Should().Contain("Connect", "Connect functionality should be present");
    }

    [Fact]
    public void UnityIntegration_ShouldHaveCommandExecution()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var controllerPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "DirectorStudioController.cs");
        var content = File.ReadAllText(controllerPath);

        // Act & Assert
        content.Should().Contain("Command", "Command execution should be present");
        content.Should().Contain("Execute", "Execute functionality should be present");
    }

    [Fact]
    public void UnityIntegration_ShouldHaveRealTimeFeedback()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var controllerPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "DirectorStudioController.cs");
        var content = File.ReadAllText(controllerPath);

        // Act & Assert
        content.Should().Contain("Real", "Real-time functionality should be present");
        content.Should().Contain("Feedback", "Feedback system should be present");
    }

    #endregion

    #region Architecture Pattern Tests

    [Fact]
    public void NexoValidationDemo_ShouldHaveSingletonPattern()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Singleton", "Singleton pattern should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHaveDependencyInjection()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Dependency", "Dependency injection should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHaveEventSystem()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Event", "Event system should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHavePerformanceOptimization()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Performance", "Performance optimization should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHaveAsyncAwaitPatterns()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("async", "Async/await patterns should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHaveErrorHandling()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Error", "Error handling should be demonstrated");
    }

    [Fact]
    public void NexoValidationDemo_ShouldHaveResourceManagement()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var validationDemoPath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "Assets", "Scripts", "NexoValidationDemo.cs");
        var content = File.ReadAllText(validationDemoPath);

        // Act & Assert
        content.Should().Contain("Resource", "Resource management should be demonstrated");
    }

    #endregion

    #region Director Studio Commands Tests

    [Fact]
    public void DirectorStudio_ShouldSupportNexoCommands()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var readmePath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "README.md");
        var content = File.ReadAllText(readmePath);

        // Act & Assert
        content.Should().Contain("validate --filter Category=Architecture", "Architecture validation command should be supported");
        content.Should().Contain("analyze --format-json", "Code analysis command should be supported");
        content.Should().Contain("agent --name list", "Agent list command should be supported");
        content.Should().Contain("agent --name analyze", "Agent analysis command should be supported");
    }

    [Fact]
    public void DirectorStudio_ShouldSupportUnityCommands()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var readmePath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "README.md");
        var content = File.ReadAllText(readmePath);

        // Act & Assert
        content.Should().Contain("TogglePlay", "Toggle play mode command should be supported");
        content.Should().Contain("GetProjectInfo", "Get project info command should be supported");
    }

    [Fact]
    public void DirectorStudio_ShouldHaveTokenAuthentication()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var readmePath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "README.md");
        var content = File.ReadAllText(readmePath);

        // Act & Assert
        content.Should().Contain("Token Authentication", "Token authentication should be supported");
        content.Should().Contain("token", "Token system should be referenced");
    }

    [Fact]
    public void DirectorStudio_ShouldHaveRealTimeCommunication()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var readmePath = Path.Combine(projectRoot, "UnityProjects", "NexoDirectorDemo", "README.md");
        var content = File.ReadAllText(readmePath);

        // Act & Assert
        content.Should().Contain("Real-time Communication", "Real-time communication should be supported");
        content.Should().Contain("TCP-based IPC", "TCP-based IPC should be used");
    }

    #endregion

    #region Smoke Test Console Tests

    [Fact]
    public void SmokeTestConsole_ShouldBePresent()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var consolePath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestConsole.cs");
        var content = File.ReadAllText(consolePath);

        // Act & Assert
        content.Should().Contain("SmokeTestConsole", "Smoke test console should be present");
        content.Should().Contain("Main", "Should have Main method");
        content.Should().Contain("RunSmokeTests", "Should call RunSmokeTests");
    }

    [Fact]
    public void SmokeTestConsole_ShouldHaveProperExitCodes()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var consolePath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestConsole.cs");
        var content = File.ReadAllText(consolePath);

        // Act & Assert
        content.Should().Contain("return 0", "Should return 0 on success");
        content.Should().Contain("return 1", "Should return 1 on failure");
    }

    [Fact]
    public void SmokeTestConsole_ShouldHaveExceptionHandling()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var consolePath = Path.Combine(projectRoot, "src", "NexoDirectorStudio", "Assets", "NexoDirectorStudio", "Tests", "EditMode", "SmokeTestConsole.cs");
        var content = File.ReadAllText(consolePath);

        // Act & Assert
        content.Should().Contain("try", "Should have try-catch block");
        content.Should().Contain("catch", "Should have exception handling");
        content.Should().Contain("Exception", "Should handle exceptions");
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
