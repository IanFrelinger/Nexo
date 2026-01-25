using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Windows platform (.NET 8.0).
/// Executes framework tests in Windows Docker container.
/// </summary>
public class Windows80Test : MultiPlatformTestBase
{
    public Windows80Test() : base("windows-8.0", "8.0", "Windows (.NET 8.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework-windows",
            nativeExecutor: null,
            cancellationToken);
    }
}
