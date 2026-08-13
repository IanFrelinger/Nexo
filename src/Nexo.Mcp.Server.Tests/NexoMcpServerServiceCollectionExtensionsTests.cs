using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace Nexo.Mcp.Server.Tests;

public sealed class NexoMcpServerServiceCollectionExtensionsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] pairs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(p => p.Key, p => (string?)p.Value))
            .Build();

    private static ServiceProvider Build(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoMcpServer(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Registers_bridge_gate_snapshot_factory_and_toolbox_contributor()
    {
        using var provider = Build(Config());

        provider.GetRequiredService<NexoMcpToolBridge>().Should().NotBeNull();
        provider.GetRequiredService<IMcpInvocationGate>().Should().BeOfType<PolicyMcpInvocationGate>();
        provider.GetRequiredService<IMcpWorldSnapshotFactory>().Should().BeOfType<OptionsMcpWorldSnapshotFactory>();
        provider.GetServices<IMcpToolContributor>().Should().ContainSingle(c => c is ToolboxMcpToolContributor);
    }

    [Fact]
    public void Binds_options_from_the_default_section()
    {
        using var provider = Build(Config(
            ($"{NexoMcpServerOptions.SectionPath}:Enabled", "true"),
            ($"{NexoMcpServerOptions.SectionPath}:ExposedToolIds:0", "repo.fs.read"),
            ($"{NexoMcpServerOptions.SectionPath}:ServerName", "nexo-test"),
            ($"{NexoMcpServerOptions.SectionPath}:ArgumentOverrides:repo.fs.read:root", "X:/pinned")));

        var options = provider.GetRequiredService<IOptions<NexoMcpServerOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.ExposedToolIds.Should().ContainSingle().Which.Should().Be("repo.fs.read");
        options.ServerName.Should().Be("nexo-test");
        options.ArgumentOverrides["repo.fs.read"]["root"].Should().Be("X:/pinned");
    }

    [Fact]
    public void Server_info_reflects_configured_name()
    {
        using var provider = Build(Config(
            ($"{NexoMcpServerOptions.SectionPath}:ServerName", "nexo-test"),
            ($"{NexoMcpServerOptions.SectionPath}:ServerVersion", "9.9.9")));

        var mcpOptions = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        mcpOptions.ServerInfo!.Name.Should().Be("nexo-test");
        mcpOptions.ServerInfo.Version.Should().Be("9.9.9");
    }

    [Fact]
    public void Enabled_under_airgapped_profile_fails_options_validation()
    {
        Environment.SetEnvironmentVariable(ValidateNexoMcpServerOptions.DeploymentProfileVariable, "airgapped");
        try
        {
            using var provider = Build(Config(($"{NexoMcpServerOptions.SectionPath}:Enabled", "true")));

            var act = () => provider.GetRequiredService<IOptions<NexoMcpServerOptions>>().Value;

            act.Should().Throw<OptionsValidationException>()
                .WithMessage("*AirGapped*", "protocol surfaces stay dark on air-gapped deployments");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ValidateNexoMcpServerOptions.DeploymentProfileVariable, null);
        }
    }

    [Fact]
    public void Disabled_under_airgapped_profile_is_valid()
    {
        Environment.SetEnvironmentVariable(ValidateNexoMcpServerOptions.DeploymentProfileVariable, "airgapped");
        try
        {
            using var provider = Build(Config());

            var options = provider.GetRequiredService<IOptions<NexoMcpServerOptions>>().Value;

            options.Enabled.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ValidateNexoMcpServerOptions.DeploymentProfileVariable, null);
        }
    }

    [Fact]
    public void Invalid_concurrency_fails_validation_when_enabled()
    {
        using var provider = Build(Config(
            ($"{NexoMcpServerOptions.SectionPath}:Enabled", "true"),
            ($"{NexoMcpServerOptions.SectionPath}:MaxConcurrentToolCalls", "0")));

        var act = () => provider.GetRequiredService<IOptions<NexoMcpServerOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().WithMessage("*MaxConcurrentToolCalls*");
    }
}
