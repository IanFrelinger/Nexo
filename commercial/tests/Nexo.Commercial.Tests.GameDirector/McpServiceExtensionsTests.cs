using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Mcp;
using GameDirector.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Client;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

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
        services.AddSingleton<INexoClient>(new GameDirectorTestHost.StubNexoClient());

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
