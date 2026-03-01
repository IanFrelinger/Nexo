using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Configuration;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Smoke tests for Autonomous Development Agent to verify basic functionality.
/// </summary>
public class AutonomousDevAgentSmokeTests : UnitTestBase
{
    private string? _tempProjectPath;
    
    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        // Create a temporary directory for test projects
        _tempProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempProjectPath);
        
        // Create a minimal project structure
        var projectFile = Path.Combine(_tempProjectPath, "TestProject.csproj");
        File.WriteAllText(projectFile, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");
        
        return Task.CompletedTask;
    }
    
    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempProjectPath != null && Directory.Exists(_tempProjectPath))
        {
            try
            {
                Directory.Delete(_tempProjectPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        return Task.CompletedTask;
    }
    
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAgentInitialization();
            await TestConfigValidation();
            await TestProjectAdapterCreation();
            await TestAutonomyLevels();
            await TestMockUserPersonas();
            
            return new TestResult
            {
                Name = nameof(AutonomousDevAgentSmokeTests),
                Category = "Agents",
                Passed = true,
                Message = "All Autonomous Development Agent smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AutonomousDevAgentSmokeTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(AutonomousDevAgentSmokeTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private Task TestAgentInitialization()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLoggerTester = new Mock<ILogger<UniversalTesterAgent>>();
        var mockTester = new UniversalTesterAgent(mockProviderFactory.Object, mockLoggerTester.Object);
        var mockLogger = new Mock<ILogger<AutonomousDevAgent>>();
        
        var agent = new AutonomousDevAgent(
            mockProviderFactory.Object,
            mockTester,
            mockLogger.Object);
        
        AssertNotNull(agent, "Agent should be initialized");
        return Task.CompletedTask;
    }
    
    private Task TestConfigValidation()
    {
        // Test minimal config
        var minimalConfig = new DevTaskConfig
        {
            Task = "Add a new feature",
            ProjectPath = _tempProjectPath ?? "/tmp/test"
        };
        
        AssertNotNull(minimalConfig, "Config should be created");
        AssertEqual("Add a new feature", minimalConfig.Task);
        AssertEqual(AutonomyLevel.Supervised, minimalConfig.Autonomy);
        AssertEqual(10, minimalConfig.MaxIterations);
        
        // Test full config
        var fullConfig = new DevTaskConfig
        {
            Task = "Add user authentication",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            ProjectType = ProjectType.DotNetApi,
            DetailedSpec = "Create REST API endpoint for login",
            References = new[] { "auth-example.md", "security-guidelines.md" },
            AcceptanceCriteria = "User can login and receive JWT token",
            MaxIterations = 5,
            Autonomy = AutonomyLevel.SemiAutonomous,
            TestTarget = "https://localhost:5001",
            TestPersona = MockUserPersona.Adversarial,
            Constraints = new[] { "Use JWT tokens", "Follow OAuth2 standards" }
        };
        
        AssertNotNull(fullConfig);
        AssertEqual(ProjectType.DotNetApi, fullConfig.ProjectType);
        AssertEqual(AutonomyLevel.SemiAutonomous, fullConfig.Autonomy);
        AssertEqual(MockUserPersona.Adversarial, fullConfig.TestPersona);
        AssertTrue(fullConfig.Constraints.Length == 2);
        return Task.CompletedTask;
    }
    
    private Task TestProjectAdapterCreation()
    {
        // Test that different project types can be specified
        var dotNetConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            ProjectType = ProjectType.DotNetApp
        };
        
        AssertEqual(ProjectType.DotNetApp, dotNetConfig.ProjectType);
        
        var unityConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            ProjectType = ProjectType.UnityGame
        };
        
        AssertEqual(ProjectType.UnityGame, unityConfig.ProjectType);
        
        var webConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            ProjectType = ProjectType.ReactApp
        };
        
        AssertEqual(ProjectType.ReactApp, webConfig.ProjectType);
        return Task.CompletedTask;
    }
    
    private Task TestAutonomyLevels()
    {
        var supervisedConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            Autonomy = AutonomyLevel.Supervised
        };
        
        AssertEqual(AutonomyLevel.Supervised, supervisedConfig.Autonomy);
        
        var semiConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            Autonomy = AutonomyLevel.SemiAutonomous
        };
        
        AssertEqual(AutonomyLevel.SemiAutonomous, semiConfig.Autonomy);
        
        var fullConfig = new DevTaskConfig
        {
            Task = "Test",
            ProjectPath = _tempProjectPath ?? "/tmp/test",
            Autonomy = AutonomyLevel.FullyAutonomous
        };
        
        AssertEqual(AutonomyLevel.FullyAutonomous, fullConfig.Autonomy);
        return Task.CompletedTask;
    }
    
    private Task TestMockUserPersonas()
    {
        var personas = new[]
        {
            MockUserPersona.Novice,
            MockUserPersona.Average,
            MockUserPersona.PowerUser,
            MockUserPersona.Adversarial,
            MockUserPersona.Accessibility,
            MockUserPersona.Impatient
        };
        
        foreach (var persona in personas)
        {
            var config = new DevTaskConfig
            {
                Task = "Test",
                ProjectPath = _tempProjectPath ?? "/tmp/test",
                TestPersona = persona
            };
            
            AssertEqual(persona, config.TestPersona, $"Persona {persona} should be set correctly");
        }
        return Task.CompletedTask;
    }
}
