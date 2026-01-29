namespace Nexo.Tests.Infrastructure;

/// <summary>
/// Shared test path helpers for E2E and CLI tests.
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// Walks up from <see cref="AppContext.BaseDirectory"/> until Nexo.sln is found.
    /// </summary>
    /// <returns>Repository root directory path.</returns>
    /// <exception cref="InvalidOperationException">Nexo.sln not found.</exception>
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Nexo.sln");
            if (File.Exists(sln)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate Nexo.sln from test base directory");
    }
}
