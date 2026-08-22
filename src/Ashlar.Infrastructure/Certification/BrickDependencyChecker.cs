using System.Xml.Linq;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Validates brick project dependencies against the certification allow-list.</summary>
internal static class BrickDependencyChecker
{
    private static readonly HashSet<string> AllowedPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ashlar.Brick.Contracts",
        "Ashlar.Authoring"
    };

    private static readonly string[] ForbiddenSourceTokens =
    [
        "Ashlar.Infrastructure",
        "Ashlar.Core.Application",
        "ProjectReference",
        "src/Ashlar",
        "/workspace"
    ];

    /// <summary>Check.</summary>
    public static DependencyCheckResult Check(string projectPath, string sourceCode)
    {
        var violations = new List<string>();

        if (!File.Exists(projectPath))
        {
            violations.Add($"Project file not found: {projectPath}");
            return new DependencyCheckResult(false, violations);
        }

        var projectDir = Path.GetDirectoryName(projectPath) ?? ".";
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        foreach (var projectRef in doc.Descendants(ns + "ProjectReference"))
        {
            violations.Add($"ProjectReference forbidden: {projectRef.Attribute("Include")?.Value}");
        }

        foreach (var packageRef in doc.Descendants(ns + "PackageReference"))
        {
            var include = packageRef.Attribute("Include")?.Value;
            if (include is null)
                continue;
            if (!AllowedPackages.Contains(include))
                violations.Add($"PackageReference '{include}' is not allowed (only Ashlar.Brick.Contracts + Ashlar.Authoring)");
        }

        foreach (var token in ForbiddenSourceTokens)
        {
            if (sourceCode.Contains(token, StringComparison.Ordinal))
                violations.Add($"Source contains forbidden token '{token}'");
        }

        return new DependencyCheckResult(violations.Count == 0, violations);
    }
}
