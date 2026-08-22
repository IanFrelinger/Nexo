using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Ubuntu 22.04 platform (.NET 7.0).
/// Executes framework tests in Ubuntu Docker container.
/// </summary>
public class Ubuntu70Test : MultiPlatformTestBase
{
    public Ubuntu70Test() : base("ubuntu-7.0", "7.0", "Ubuntu 22.04 (.NET 7.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework",
            nativeExecutor: null,
            cancellationToken);
    }
}
