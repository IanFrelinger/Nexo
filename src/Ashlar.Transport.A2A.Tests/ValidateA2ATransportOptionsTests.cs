using FluentAssertions;
using Xunit;

namespace Ashlar.Transport.A2A.Tests;

public sealed class ValidateA2ATransportOptionsTests
{
    private static readonly ValidateA2ATransportOptions Validator = new();

    [Fact]
    public void Disabled_options_are_always_valid()
    {
        Validator.Validate(null, new A2ATransportOptions()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_under_airgapped_profile_fails()
    {
        Environment.SetEnvironmentVariable(ValidateA2ATransportOptions.DeploymentProfileVariable, "airgapped");
        try
        {
            var result = Validator.Validate(null, new A2ATransportOptions { Enabled = true });

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().Contain("AirGapped");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ValidateA2ATransportOptions.DeploymentProfileVariable, null);
        }
    }

    [Fact]
    public void Endpoint_prefix_must_be_absolute_http()
    {
        var options = new A2ATransportOptions { Enabled = true };
        options.Endpoints.Add(new A2ARemoteEndpointOptions { UrlPrefix = "not-a-url" });

        Validator.Validate(null, options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Api_key_header_and_env_var_must_be_paired_and_present()
    {
        var options = new A2ATransportOptions { Enabled = true };
        options.Endpoints.Add(new A2ARemoteEndpointOptions
        {
            UrlPrefix = "https://peer.example.com",
            ApiKeyHeader = "X-Api-Key",
        });

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("together");
    }

    [Fact]
    public void Referenced_but_unset_env_var_fails_at_startup()
    {
        var options = new A2ATransportOptions { Enabled = true };
        options.Endpoints.Add(new A2ARemoteEndpointOptions
        {
            UrlPrefix = "https://peer.example.com",
            ApiKeyHeader = "X-Api-Key",
            ApiKeyEnvVar = "ASHLAR_TEST_A2A_KEY_THAT_DOES_NOT_EXIST",
        });

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ASHLAR_TEST_A2A_KEY_THAT_DOES_NOT_EXIST");
    }
}
