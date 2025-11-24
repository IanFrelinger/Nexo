namespace Nexo.Tests.Application.Helpers;

/// <summary>
/// Helper utilities for tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a temporary directory for testing.
    /// </summary>
    public static DirectoryInfo CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        return Directory.CreateDirectory(tempPath);
    }

    /// <summary>
    /// Cleans up a temporary directory.
    /// </summary>
    public static void CleanupTempDirectory(DirectoryInfo dir)
    {
        if (dir.Exists)
        {
            try
            {
                Directory.Delete(dir.FullName, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Creates a temporary assembly file for testing.
    /// </summary>
    public static FileInfo CreateTempAssemblyFile(DirectoryInfo dir, string name = "test.dll")
    {
        var filePath = Path.Combine(dir.FullName, name);
        File.WriteAllText(filePath, "dummy assembly content");
        return new FileInfo(filePath);
    }

    /// <summary>
    /// Creates a temporary test project file for testing.
    /// </summary>
    public static FileInfo CreateTempTestProjectFile(DirectoryInfo dir, string name = "TestProject.csproj")
    {
        var filePath = Path.Combine(dir.FullName, name);
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>";
        File.WriteAllText(filePath, content);
        return new FileInfo(filePath);
    }
}

