namespace Ashlar.Tests.Infrastructure;

/// <summary>
/// Shared test path helpers for E2E and CLI tests.
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// Walks up from <see cref="AppContext.BaseDirectory"/> until Ashlar.sln is found.
    /// </summary>
    /// <returns>Repository root directory path.</returns>
    /// <exception cref="InvalidOperationException">Ashlar.sln not found.</exception>
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Ashlar.sln");
            if (File.Exists(sln)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate Ashlar.sln from test base directory");
    }
}
