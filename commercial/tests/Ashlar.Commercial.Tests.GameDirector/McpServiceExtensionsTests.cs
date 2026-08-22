using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Mcp;
using GameDirector.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Client;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for mcp service extensions.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class McpServiceExtensionsTests
{
    [Fact]
    public void AddGameDirectorMcp_registers_all_tools_and_registry()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddSingleton<IDataDecisionAuditLog>(GameDirectorTestHost.CreateAuditLog());
        var registry = GameDirectorTestHost.CreateBrickRegistry();
        services.AddSingleton<IBrickRegistry>(registry);
        services.AddSingleton<IAshlarClient>(new GameDirectorTestHost.StubAshlarClient());

        services.AddGameDirectorMcp();

        using var provider = services.BuildServiceProvider();
        provider.GetService<McpBrickExecutor>().Should().NotBeNull();
        provider.GetService<AnalyzeBalanceTool>().Should().NotBeNull();
        provider.GetService<ValidateMapTool>().Should().NotBeNull();
        provider.GetService<GenerateContentTool>().Should().NotBeNull();
        provider.GetService<GetAuditTrailTool>().Should().NotBeNull();
        provider.GetService<QueryPatternsTool>().Should().NotBeNull();
        provider.GetService<McpToolRegistry>().Should().NotBeNull();
    }
}
