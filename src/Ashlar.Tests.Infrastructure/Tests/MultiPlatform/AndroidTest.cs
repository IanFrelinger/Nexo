using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Android platform (.NET 8.0).
/// Executes framework tests in Android Docker container.
/// </summary>
public class Android80Test : MultiPlatformTestBase
{
    public Android80Test() : base("android-8.0", "8.0", "Android (.NET 8.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework-android",
            nativeExecutor: null,
            cancellationToken);
    }
}
