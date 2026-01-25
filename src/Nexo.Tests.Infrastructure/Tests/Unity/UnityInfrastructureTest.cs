using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.Unity;

/// <summary>
/// Infrastructure test for Unity environment compatibility.
/// 
/// Executes Unity tests in Docker container using GameCI's Unity Docker images.
/// Validates:
/// - .NET Standard 2.0 compatibility
/// - Unity UI component builds
/// - Framework tests in Unity environment
/// 
/// This test is discoverable by TestRunnerAdapter and can be executed
/// through the standard test runner infrastructure.
/// </summary>
public class UnityInfrastructureTest : MultiPlatform.MultiPlatformTestBase
{
    public UnityInfrastructureTest() 
        : base("unity-8.0", "8.0", "Unity (.NET Standard 2.0)")
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Use Docker-based execution (GameCI Unity images)
        return await ExecuteTestAsync(
            dockerfile: ".docker/Dockerfile.test-framework-unity",
            nativeExecutor: null,
            cancellationToken);
    }
}
