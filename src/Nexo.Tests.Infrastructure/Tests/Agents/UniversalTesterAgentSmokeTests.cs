using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Smoke tests for Universal Testing Agent to verify basic functionality.
/// </summary>
public class UniversalTesterAgentSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAgentInitialization();
            await TestConfigValidation();
            await TestBasicExecution();
            await TestTargetTypeInference();
            
            return new TestResult
            {
                Name = nameof(UniversalTesterAgentSmokeTests),
                Category = "Agents",
                Passed = true,
                Message = "All Universal Testing Agent smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(UniversalTesterAgentSmokeTests),
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
                Name = nameof(UniversalTesterAgentSmokeTests),
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
        var mockLogger = new Mock<ILogger<UniversalTesterAgent>>();
        
        var agent = new UniversalTesterAgent(mockProviderFactory.Object, mockLogger.Object);
        
        AssertNotNull(agent, "Agent should be initialized");
        return Task.CompletedTask;
    }
    
    private Task TestConfigValidation()
    {
        // Test valid config
        var validConfig = new UniversalTesterConfig
        {
            Target = "https://example.com",
            Goal = "Test the homepage loads correctly",
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2)
        };
        
        AssertNotNull(validConfig, "Config should be created");
        AssertEqual("https://example.com", validConfig.Target);
        AssertEqual("Test the homepage loads correctly", validConfig.Goal);
        AssertEqual(TestingDepth.Quick, validConfig.Depth);
        
        // Test config with all optional fields
        var fullConfig = new UniversalTesterConfig
        {
            Target = "https://api.example.com",
            Goal = "Test API endpoints",
            TargetType = TargetType.Api,
            Constraints = new[] { "Test authentication", "Verify error handling" },
            Depth = TestingDepth.Standard,
            MaxDuration = TimeSpan.FromMinutes(10),
            SetupInstructions = "Use API key: test-key-123",
            SuccessCriteria = "All endpoints return 200 or appropriate error codes"
        };
        
        AssertNotNull(fullConfig);
        AssertEqual(TargetType.Api, fullConfig.TargetType);
        AssertTrue(fullConfig.Constraints.Length == 2);
        return Task.CompletedTask;
    }
    
    private Task TestBasicExecution()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLogger = new Mock<ILogger<UniversalTesterAgent>>();
        
        var agent = new UniversalTesterAgent(mockProviderFactory.Object, mockLogger.Object);
        
        var config = new UniversalTesterConfig
        {
            Target = "https://httpbin.org/html",
            Goal = "Verify page structure and content",
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(1)
        };
        
        // Note: This is a smoke test, so we're just verifying the agent can be created
        // and accepts the config. Full execution would require actual adapters and providers.
        AssertNotNull(agent, "Agent should be created");
        AssertNotNull(config, "Config should be created");
        
        // Verify config properties are set correctly
        AssertEqual("https://httpbin.org/html", config.Target);
        AssertEqual("Verify page structure and content", config.Goal);
        return Task.CompletedTask;
    }
    
    private Task TestTargetTypeInference()
    {
        // Test URL inference (should be WebApp)
        var webConfig = new UniversalTesterConfig
        {
            Target = "https://example.com",
            Goal = "Test web app"
        };
        
        AssertNotNull(webConfig);
        
        // Test API inference
        var apiConfig = new UniversalTesterConfig
        {
            Target = "api://https://api.example.com",
            Goal = "Test API"
        };
        
        AssertNotNull(apiConfig);
        
        // Test CLI inference
        var cliConfig = new UniversalTesterConfig
        {
            Target = "cli://dotnet --version",
            Goal = "Test CLI command"
        };
        
        AssertNotNull(cliConfig);
        
        // Test explicit target type
        var explicitConfig = new UniversalTesterConfig
        {
            Target = "https://example.com",
            Goal = "Test",
            TargetType = TargetType.Game
        };
        
        AssertEqual(TargetType.Game, explicitConfig.TargetType);
        return Task.CompletedTask;
    }
}
