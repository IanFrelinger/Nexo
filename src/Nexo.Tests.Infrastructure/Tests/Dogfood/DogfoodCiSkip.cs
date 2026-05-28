namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Parallel matrix dogfood tests spawn nested <c>dotnet test</c> processes and are flaky on GitHub runners.
/// </summary>
internal static class DogfoodCiSkip
{
    public static bool ShouldSkip =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}
