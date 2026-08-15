using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexo.Mcp.Client.Tests;

public sealed class ValidateNexoMcpClientOptionsTests
{
    private static readonly ValidateNexoMcpClientOptions Validator = new();

    private static NexoMcpClientOptions Enabled(params McpServerEndpointOptions[] servers)
    {
        var options = new NexoMcpClientOptions { Enabled = true };
        foreach (var server in servers)
        {
            options.Servers.Add(server);
        }

        return options;
    }

    private static McpServerEndpointOptions Server(
        string name = "github",
        string? url = "https://mcp.example.com/mcp",
        string? header = null,
        string? envVar = null)
        => new() { Name = name, Url = url, ApiKeyHeader = header, ApiKeyEnvVar = envVar };

    [Fact]
    public void Disabled_options_are_always_valid()
    {
        var result = Validator.Validate(null, new NexoMcpClientOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Valid_enabled_configuration_passes()
    {
        Validator.Validate(null, Enabled(Server())).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_under_airgapped_profile_fails()
    {
        Environment.SetEnvironmentVariable(ValidateNexoMcpClientOptions.DeploymentProfileVariable, "AirGapped");
        try
        {
            var result = Validator.Validate(null, Enabled(Server()));

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("AirGapped");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ValidateNexoMcpClientOptions.DeploymentProfileVariable, null);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad name")]
    [InlineData("bad:name")]
    public void Server_names_outside_the_id_charset_fail(string name)
    {
        var result = Validator.Validate(null, Enabled(Server(name: name)));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Name");
    }

    [Fact]
    public void Duplicate_server_names_fail()
    {
        var result = Validator.Validate(null, Enabled(Server(), Server()));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("duplicate");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("/relative/path")]
    public void Non_http_urls_fail(string? url)
    {
        var result = Validator.Validate(null, Enabled(Server(url: url)));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Url");
    }

    [Fact]
    public void Api_key_header_without_env_var_fails()
    {
        var result = Validator.Validate(null, Enabled(Server(header: "X-Api-Key")));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("together");
    }

    [Fact]
    public void Referenced_but_unset_env_var_fails_at_startup()
    {
        var result = Validator.Validate(
            null,
            Enabled(Server(header: "X-Api-Key", envVar: "NEXO_TEST_MCP_KEY_THAT_DOES_NOT_EXIST")));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("NEXO_TEST_MCP_KEY_THAT_DOES_NOT_EXIST");
    }

    [Fact]
    public void Referenced_and_set_env_var_passes()
    {
        Environment.SetEnvironmentVariable("NEXO_TEST_MCP_KEY", "secret");
        try
        {
            var result = Validator.Validate(null, Enabled(Server(header: "X-Api-Key", envVar: "NEXO_TEST_MCP_KEY")));

            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_TEST_MCP_KEY", null);
        }
    }

    [Fact]
    public void Negative_refresh_interval_fails()
    {
        var options = Enabled(Server());
        options.ToolListRefreshInterval = TimeSpan.FromSeconds(-1);

        Validator.Validate(null, options).Failed.Should().BeTrue();
    }
}
