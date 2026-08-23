using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Infrastructure.Maintenance.Sdk.Extensions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SDK;
/// <summary>Tests for infrastructure sdk gap coverage.</summary>
public class InfrastructureSdkGapCoverageTests
{
    [Fact]
    public void AddArtifactCleanup_registers_cleanup_with_incomplete_blob_strategy()
    {
        var originalBlob = Environment.GetEnvironmentVariable("ASHLAR_INCOMPLETE_BLOB_PATH");
        var originalLifecycle = Environment.GetEnvironmentVariable("ASHLAR_BLOB_LIFECYCLE");
        var temp = Path.Combine(Path.GetTempPath(), "ashlar-blob-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);

        try
        {
            Environment.SetEnvironmentVariable("ASHLAR_INCOMPLETE_BLOB_PATH", temp);
            Environment.SetEnvironmentVariable("ASHLAR_BLOB_LIFECYCLE", "docker");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddArtifactCleanup();

            var provider = services.BuildServiceProvider();
            provider.GetServices<IArtifactCleanupStrategy>().Should().HaveCountGreaterThan(1);
            provider.GetService<IArtifactCleanupService>().Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_INCOMPLETE_BLOB_PATH", originalBlob);
            Environment.SetEnvironmentVariable("ASHLAR_BLOB_LIFECYCLE", originalLifecycle);
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }
}
