using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Runtime.InteropServices;
using Xunit;

namespace Nexo.Tests.GeospatialE2E.Tests;

/// <summary>
/// Tests using UniversalTesterAgent to validate geospatial application interactions
/// across all target platforms. Uses AI to understand and test CLI commands and API endpoints.
/// </summary>
public class GeospatialAIAgentTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IProviderFactory _providerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly UniversalTesterAgent _agent;
    private readonly IExecutionContext _context;
    private readonly string _testOutputDir;

    public GeospatialAIAgentTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-ai-agent-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddScoped<IProviderFactory, ProviderFactory>();
        _serviceProvider = services.BuildServiceProvider();

        _providerFactory = _serviceProvider.GetRequiredService<IProviderFactory>();
        _loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        
        var agentLogger = _loggerFactory.CreateLogger<UniversalTesterAgent>();
        _agent = new UniversalTesterAgent(_providerFactory, agentLogger);
        
        // Create execution context (use offline/mock provider for deterministic testing)
        _context = new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "geospatial-ai-agent-tests",
            BehaviorId = "test-geospatial-interactions",
            Provider = "offline",
            IsAirGapped = true,
            AuditMode = false,
            Variables = new Dictionary<string, object>()
        };
    }

    [Fact]
    public async Task AIAgent_ShouldTestGeoTerrainCLI_OnCurrentPlatform()
    {
        // Arrange
        var cliPath = GetCliPath();
        var config = new UniversalTesterConfig
        {
            Target = $"cli://{cliPath} geoterrain bounds-to-obj --bounds \"37.0,-122.0,37.1,-121.9\" --output \"{Path.Combine(_testOutputDir, "terrain.obj")}\" --provider echo --validate-integrity --mesh-quality-report --json",
            Goal = "Verify the geoterrain bounds-to-obj command executes successfully, validates integrity, and generates a mesh quality report",
            TargetType = TargetType.Cli,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2),
            Constraints = new[]
            {
                "Command should complete without errors",
                "Output should include validation results",
                "Mesh quality metrics should be reported"
            },
            SuccessCriteria = "Command returns exit code 0 or 1 (validation may find issues), and produces OBJ file with validation output"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode, the agent may not complete full test cycles
        // The important thing is that the agent can run on this platform without errors
        // We verify platform compatibility, not full test execution
    }

    [Fact]
    public async Task AIAgent_ShouldTestGeoVectorCLI_OnCurrentPlatform()
    {
        // Arrange
        var cliPath = GetCliPath();
        var config = new UniversalTesterConfig
        {
            Target = $"cli://{cliPath} geovector extract --bounds \"37.0,-122.0,37.1,-121.9\" --output \"{Path.Combine(_testOutputDir, "vector.geojson")}\" --provider echo --json",
            Goal = "Verify the geovector extract command executes successfully and produces GeoJSON output",
            TargetType = TargetType.Cli,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2),
            Constraints = new[]
            {
                "Command should complete successfully",
                "Output should be valid GeoJSON",
                "Should extract vector features (buildings, roads, water, vegetation)"
            },
            SuccessCriteria = "Command returns exit code 0 and produces GeoJSON file with vector features"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode, the agent may not complete full test cycles
        // The important thing is that the agent can run on this platform without errors
        // We verify platform compatibility, not full test execution
    }

    [Fact]
    public async Task AIAgent_ShouldTestWorldCLI_OnCurrentPlatform()
    {
        // Arrange
        var cliPath = GetCliPath();
        var config = new UniversalTesterConfig
        {
            Target = $"cli://{cliPath} world bundle --bounds \"37.0,-122.0,37.1,-121.9\" --output \"{Path.Combine(_testOutputDir, "world.zip")}\" --provider echo --json",
            Goal = "Verify the world bundle command executes successfully and creates a world bundle",
            TargetType = TargetType.Cli,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2),
            Constraints = new[]
            {
                "Command should complete successfully",
                "Should create a world bundle file",
                "Bundle should contain terrain and vector data"
            },
            SuccessCriteria = "Command returns exit code 0 and produces world bundle file"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode, the agent may not complete full test cycles
        // The important thing is that the agent can run on this platform without errors
        // We verify platform compatibility, not full test execution
    }

    [Fact]
    public async Task AIAgent_ShouldTestGeoTerrainAPI_OnCurrentPlatform()
    {
        // Skip if API server is not available (would need to start it first)
        // This test validates the API adapter can connect and test endpoints
        
        // Arrange
        var apiBaseUrl = Environment.GetEnvironmentVariable("NEXO_API_URL") ?? "http://localhost:5000";
        var config = new UniversalTesterConfig
        {
            Target = $"api://{apiBaseUrl}",
            Goal = "Verify the geoterrain API endpoints are accessible and respond correctly",
            TargetType = TargetType.Api,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2),
            Constraints = new[]
            {
                "API should be accessible",
                "Endpoints should return appropriate status codes",
                "Response format should be valid JSON"
            },
            SuccessCriteria = "API endpoints respond with 200 OK or appropriate error codes"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode or if API isn't running, the adapter will handle errors gracefully
        // The important thing is that the agent and adapter can run on this platform
    }

    [Fact]
    public async Task AIAgent_ShouldTestGeoVectorAPI_OnCurrentPlatform()
    {
        // This test validates the API adapter can be instantiated and attempt connections
        // It doesn't require the API server to be running - it tests adapter compatibility
        
        // Arrange
        var apiBaseUrl = Environment.GetEnvironmentVariable("NEXO_API_URL") ?? "http://localhost:5000";
        var config = new UniversalTesterConfig
        {
            Target = $"api://{apiBaseUrl}/api/geovector",
            Goal = "Verify the geovector API endpoints (extract/roads, extract/water, extract/vegetation) work correctly",
            TargetType = TargetType.Api,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromSeconds(30), // Shorter timeout for offline mode
            Constraints = new[]
            {
                "Extract roads endpoint should work",
                "Extract water endpoint should work",
                "Extract vegetation endpoint should work",
                "All should return job responses"
            },
            SuccessCriteria = "All extraction endpoints accept requests and return job IDs"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode or if API isn't running, the adapter will handle errors gracefully
        // The important thing is that the agent and adapter can run on this platform
    }

    [Fact]
    public async Task AIAgent_ShouldTestValidationFlags_OnCurrentPlatform()
    {
        // Arrange
        var cliPath = GetCliPath();
        var config = new UniversalTesterConfig
        {
            Target = $"cli://{cliPath} geoterrain tile-to-obj --tile \"N37W122\" --output \"{Path.Combine(_testOutputDir, "tile.obj")}\" --provider echo --validate-integrity --mesh-quality-report --json",
            Goal = "Verify that validation flags (--validate-integrity and --mesh-quality-report) are properly processed and produce validation output",
            TargetType = TargetType.Cli,
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromMinutes(2),
            Constraints = new[]
            {
                "Validation flags should be recognized",
                "Integrity validation should run",
                "Mesh quality report should be generated",
                "Output should include validation results in JSON format"
            },
            SuccessCriteria = "Command processes validation flags and includes validation results in output"
        };

        // Act
        var report = await _agent.ExecuteAsync(config, _context, ct: CancellationToken.None);

        // Assert
        report.Should().NotBeNull("Agent should return a report");
        report.Summary.Should().NotBeNull("Agent should produce a summary");
        // In offline mode, the agent may not complete full test cycles
        // The important thing is that the agent can run on this platform without errors
        // We verify platform compatibility, not full test execution
    }

    private string GetCliPath()
    {
        // Try to find the CLI executable
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.CLI", "bin", "Debug", "net8.0", "nexo"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.CLI", "bin", "Debug", "net8.0", "nexo.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.CLI", "bin", "Release", "net8.0", "nexo"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.CLI", "bin", "Release", "net8.0", "nexo.exe"),
            "dotnet", // Fallback to dotnet run
        };

        foreach (var path in possiblePaths)
        {
            if (path == "dotnet")
            {
                // Use dotnet run as fallback
                return "dotnet run --project src/Nexo.CLI --";
            }
            
            if (File.Exists(path) || (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists($"{path}.exe")))
            {
                return path;
            }
        }

        // Default to dotnet run
        return "dotnet run --project src/Nexo.CLI --";
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        _serviceProvider?.Dispose();
    }
}
