using System.Text.RegularExpressions;

namespace Nexo.BackgroundAgents.Security;

/// <summary>
/// Classifies self-extend write targets and infers brick ids for certification admission checks.
/// </summary>
public static class BrickAdmissionPathHelper
{
    private static readonly Regex BrickClassRegex = new(
        @"class\s+(\w+Brick)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsRunnableBrickPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var rel = relativePath.Replace('\\', '/').TrimStart('/');
        return rel.StartsWith("src/Nexo.Bricks", StringComparison.OrdinalIgnoreCase)
               && rel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    public static string? InferBrickId(string relativePath, string? sourceContent)
    {
        if (!string.IsNullOrWhiteSpace(sourceContent))
        {
            var match = BrickClassRegex.Match(sourceContent);
            if (match.Success)
                return ClassNameToBrickId(match.Groups[1].Value);
        }

        var fileName = Path.GetFileNameWithoutExtension(relativePath.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(fileName) ? null : ClassNameToBrickId(fileName);
    }

    public static string ClassNameToBrickId(string className)
    {
        var name = className;
        if (name.EndsWith("Brick", StringComparison.Ordinal))
            name = name[..^"Brick".Length];

        if (string.IsNullOrEmpty(name))
            return className.ToLowerInvariant();

        var chars = new List<char>(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && chars.Count > 0 && chars[^1] != '-')
                chars.Add('-');
            chars.Add(char.ToLowerInvariant(c));
        }

        var kebab = new string(chars.ToArray());
        return kebab.EndsWith("-brick", StringComparison.Ordinal) ? kebab : $"{kebab}-brick";
    }
}
