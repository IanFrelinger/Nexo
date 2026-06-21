namespace Nexo.Infrastructure.Certification.Composition;

internal static class CompositionDependencyChecker
{
    private static readonly string[] ForbiddenSourceTokens =
    [
        "Nexo.Infrastructure",
        "Nexo.Core.Application",
        "ProjectReference",
        "src/Nexo",
        "/workspace"
    ];

    public static DependencyCheckResult Check(string? wiringMetadata)
    {
        var violations = new List<string>();
        var text = wiringMetadata ?? string.Empty;

        foreach (var token in ForbiddenSourceTokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
                violations.Add($"Composition wiring contains forbidden token '{token}'");
        }

        return new DependencyCheckResult(violations.Count == 0, violations);
    }

    internal sealed record DependencyCheckResult(bool Passed, IReadOnlyList<string> Violations);
}
