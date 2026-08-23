using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ashlar.Infrastructure.Trust;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for cloud availability resolver.</summary>
public sealed class CloudAvailabilityResolverTests
{
    [Fact]
    public async Task IsAirGappedAsync_WhenAshlarAirgapEnvIs1_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", "1");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WhenAshlarAirgapEnvIsTrue_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", "true");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WhenAshlarAirgapEnvIs0_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", "0");
        try
        {
            var logger = new LoggerFactory().CreateLogger<CloudAvailabilityResolver>();
            var resolver = new CloudAvailabilityResolver(logger);

            var result = await resolver.IsAirGappedAsync();

            result.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", null);
        }
    }

    [Fact]
    public async Task IsAirGappedAsync_WithConfigFileContainingAirGappedTrue_ReturnsTrue()
    {
        var prevEnv = Environment.GetEnvironmentVariable("ASHLAR_AIRGAP");
        Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", null);
        var configPath = Path.Combine(Path.GetTempPath(), $"ashlar-cloud-resolver-{Guid.NewGuid():N}.json");
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
            Environment.SetEnvironmentVariable("ASHLAR_AIRGAP", prevEnv);
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}
