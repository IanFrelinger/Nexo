using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for Ubuntu 22.04 platform (.NET 8.0).
/// Executes framework tests in Ubuntu Docker container.
/// </summary>
public class Ubuntu80Test : MultiPlatformTestBase
{
    public Ubuntu80Test() : base("ubuntu-8.0", "8.0", "Ubuntu 22.04 (.NET 8.0)")
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
