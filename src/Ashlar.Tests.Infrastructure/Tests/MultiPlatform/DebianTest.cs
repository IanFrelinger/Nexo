using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Debian 12 platform (.NET 8.0).
/// Executes framework tests in Debian Docker container.
/// </summary>
public class Debian80Test : MultiPlatformTestBase
{
    public Debian80Test() : base("debian-8.0", "8.0", "Debian 12 (.NET 8.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework-debian",
            nativeExecutor: null,
            cancellationToken);
    }
}
