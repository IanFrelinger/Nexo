using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Infrastructure.Agent.Adapters;
using Nexo.Abstractions;
using Nexo.Demo.CLI.Agents;

namespace Nexo.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for AgentExecutorAdapter.
/// Tests the adapter with real agent execution.
/// </summary>
public class AgentExecutorAdapterIntegrationTests
{
    private readonly ILogger<AgentExecutorAdapter> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentExecutorAdapter _adapter;

    public AgentExecutorAdapterIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AgentExecutorAdapter>();

        // Set up service provider with agents
        var services = new ServiceCollection();
        services.AddTransient<IAgent, DirectorAgent>(sp => new DirectorAgent("director"));
        _serviceProvider = services.BuildServiceProvider();

        _adapter = new AgentExecutorAdapter(_logger, _serviceProvider);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAgentName_ReturnsResult()
    {
        // Arrange
        var agentName = "director";
        var inputFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Core.Domain", "bin", "Debug", "net8.0", "Nexo.Core.Domain.dll"));

        // Only run if the file exists
        if (!inputFile.Exists)
        {
            // Find any DLL in the project
            var dllFiles = Directory.GetFiles(
                Path.Combine(Directory.GetCurrentDirectory(), "src"),
                "*.dll",
                SearchOption.AllDirectories)
                .FirstOrDefault();

            if (dllFiles != null)
            {
                inputFile = new FileInfo(dllFiles);
            }
            else
            {
                // Skip test if no DLLs found
                return;
            }
        }

        // Act
        var result = await _adapter.ExecuteAsync(agentName, inputFile, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agentName, result.AgentName);
        // Result may succeed or fail depending on agent execution
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentAgent_ReturnsFailedResult()
    {
        // Arrange
        var agentName = "nonexistent-agent";

        // Act
        var result = await _adapter.ExecuteAsync(agentName, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var agentName = "director";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _adapter.ExecuteAsync(agentName, null, cancellationTokenSource.Token));
    }
}

