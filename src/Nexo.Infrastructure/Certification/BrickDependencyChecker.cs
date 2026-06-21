using System.Xml.Linq;

namespace Nexo.Infrastructure.Certification;

internal static class BrickDependencyChecker
{
    private static readonly HashSet<string> AllowedPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nexo.Brick.Contracts",
        "Nexo.Authoring"
    };

    private static readonly string[] ForbiddenSourceTokens =
    [
        "Nexo.Infrastructure",
        "Nexo.Core.Application",
        "ProjectReference",
        "src/Nexo",
        "/workspace"
    ];

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
                violations.Add($"PackageReference '{include}' is not allowed (only Nexo.Brick.Contracts + Nexo.Authoring)");
        }

        foreach (var token in ForbiddenSourceTokens)
        {
            if (sourceCode.Contains(token, StringComparison.Ordinal))
                violations.Add($"Source contains forbidden token '{token}'");
        }

        return new DependencyCheckResult(violations.Count == 0, violations);
    }
}

internal sealed record DependencyCheckResult(bool Passed, IReadOnlyList<string> Violations);
