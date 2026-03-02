using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public sealed class CloudAvailabilityResolverTests
{
    [Fact]
    public async Task IsAirGappedAsync_WhenNexoAirgapEnvIs1_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("NEXO_AIRGAP", "1");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WhenNexoAirgapEnvIsTrue_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("NEXO_AIRGAP", "true");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WhenNexoAirgapEnvIs0_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("NEXO_AIRGAP", "0");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WithConfigFileContainingAirGappedTrue_ReturnsTrue()
    {
        var prevEnv = Environment.GetEnvironmentVariable("NEXO_AIRGAP");
        Environment.SetEnvironmentVariable("NEXO_AIRGAP", null);
        var configPath = Path.Combine(Path.GetTempPath(), $"nexo-cloud-resolver-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, """{"airGapped": true}""");
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger, configPath);

            var result = await resolver.IsAirGappedAsync(refresh: true);

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_AIRGAP", prevEnv);
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}
