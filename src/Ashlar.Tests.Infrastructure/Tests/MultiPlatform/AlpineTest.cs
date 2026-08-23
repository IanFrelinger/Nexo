using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Alpine Linux platform (.NET 8.0).
/// Executes framework tests in Alpine Docker container.
/// </summary>
public class Alpine80Test : MultiPlatformTestBase
{
    public Alpine80Test() : base("alpine-8.0", "8.0", "Alpine Linux (.NET 8.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework-alpine",
            nativeExecutor: null,
            cancellationToken);
    }
}
