using System.Xml.Linq;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Refuses author MSBuild and toolchain files before any restore. The certifier never
/// evaluates the author's project; this fence makes a regression that reintroduces
/// <c>dotnet build</c> on author files fail closed, and tells the author why.
/// </summary>
public static class BuildSurfaceFence
{
    private static readonly string[] ForbiddenSidecars =
    [
        "NuGet.Config",
        "nuget.config",
        "global.json",
        "Directory.Build.rsp",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props"
    ];

    private static readonly HashSet<string> ForbiddenElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Target",
        "UsingTask",
        "Import",
        "Exec"
    };

    /// <summary>Inspects <paramref name="projectDirectory"/> and throws on a forbidden surface.</summary>
    public static void Inspect(string projectDirectory, string csprojPath)
    {
        foreach (var name in ForbiddenSidecars)
        {
            var sidecar = Path.Combine(projectDirectory, name);
            if (File.Exists(sidecar))
            {
                throw new InvalidOperationException(
                    $"build-surface fence: author toolchain file '{name}' is refused before restore. "
                    + "The certifier compiles with closed-world options and does not evaluate author MSBuild.");
            }
        }

        XDocument document;
        try
        {
            document = XDocument.Load(csprojPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"build-surface fence: project file is not well-formed XML: {ex.Message}", ex);
        }

        foreach (var element in document.Descendants())
        {
            var local = element.Name.LocalName;
            if (ForbiddenElements.Contains(local))
            {
                throw new InvalidOperationException(
                    $"build-surface fence: author <{local}> is refused before restore. "
                    + "The certifier does not run author targets, tasks, imports, or Exec.");
            }

            if (string.Equals(local, "Analyzer", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(local, "PackageReference", StringComparison.OrdinalIgnoreCase)
                    && LooksLikeAnalyzerPackage(element)))
            {
                throw new InvalidOperationException(
                    "build-surface fence: author Analyzer items are refused before restore. "
                    + "The certifier attaches its own analyzer catalog.");
            }
        }
    }

    private static bool LooksLikeAnalyzerPackage(XElement packageReference)
    {
        var include = packageReference.Attribute("Include")?.Value
            ?? packageReference.Element(packageReference.Name.Namespace + "Include")?.Value
            ?? string.Empty;
        return include.Contains("Analyzer", StringComparison.OrdinalIgnoreCase);
    }
}
