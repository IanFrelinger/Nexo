using System.Text.RegularExpressions;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Core.Application.Generation;

/// <summary>
/// Pre-gate enforcing a <see cref="BrickConstraintManifest"/> on candidate ingest
/// (trust-loop spec R3.5, second use). Purely structural and deterministic — no
/// compiler, no I/O — so it runs ahead of the profile's own validators. Failure
/// reasons name a fix target and restate the violated instruction verbatim (R3.4),
/// using the same instruction text the proposer was given.
/// </summary>
public sealed class BrickConstraintManifestValidator : IPostValidator<GeneratedArtifact>
{
    private static readonly Regex UsingDirective = new(
        @"^\s*using\s+(?<body>[^;]+);",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex NamespaceDeclaration = new(
        @"namespace\s+(?<name>[\w.]+)",
        RegexOptions.Compiled);

    private static readonly Regex ClassDeclaration = new(
        @"class\s+(?<name>\w+)(?<rest>[^{]*)",
        RegexOptions.Compiled);

    private readonly BrickConstraintManifest _manifest;

    /// <summary>Initializes the validator for one manifest.</summary>
    public BrickConstraintManifestValidator(BrickConstraintManifest manifest)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    /// <inheritdoc />
    public ValueTask<(bool ok, string? reason)> ValidateAsync(GeneratedArtifact output, CancellationToken ct)
    {
        var content = output?.Content ?? "";
        if (string.IsNullOrWhiteSpace(content))
            return new ValueTask<(bool, string?)>((false, "Candidate artifact is empty — produce the artifact content."));

        var violations = new List<string>();

        foreach (var token in _manifest.ForbiddenApiTokens)
        {
            if (content.IndexOf(token, StringComparison.Ordinal) >= 0)
                violations.Add($"Forbidden API '{token}' is present — remove it. Constraint: {_manifest.ForbiddenApiInstruction(token)}");
        }

        if (_manifest.AllowedUsings.Count > 0)
        {
            foreach (Match match in UsingDirective.Matches(content))
            {
                var body = match.Groups["body"].Value.Trim();
                if (!_manifest.AllowedUsings.Contains(body, StringComparer.Ordinal))
                    violations.Add($"Using directive 'using {body};' is not allowlisted — remove it. Constraint: {_manifest.UsingsInstruction()}");
            }
        }

        if (!string.IsNullOrWhiteSpace(_manifest.RequiredNamespace))
        {
            var ns = NamespaceDeclaration.Match(content);
            if (!ns.Success)
                violations.Add($"No namespace declaration found — declare one. Constraint: {_manifest.NamespaceInstruction()}");
            else if (!string.Equals(ns.Groups["name"].Value, _manifest.RequiredNamespace, StringComparison.Ordinal))
                violations.Add($"Namespace '{ns.Groups["name"].Value}' is wrong — change it. Constraint: {_manifest.NamespaceInstruction()}");
        }

        var classMatch = ClassDeclaration.Match(content);
        if (!string.IsNullOrWhiteSpace(_manifest.RequiredBaseType))
        {
            var baseListed = classMatch.Success
                && Regex.IsMatch(
                    classMatch.Groups["rest"].Value,
                    $@":.*\b{Regex.Escape(_manifest.RequiredBaseType!)}\b");
            if (!baseListed)
                violations.Add($"The class does not derive from {_manifest.RequiredBaseType} — fix the base type. Constraint: {_manifest.BaseTypeInstruction()}");
        }

        if (!string.IsNullOrWhiteSpace(_manifest.RequiredClassNameSuffix))
        {
            if (!classMatch.Success)
                violations.Add($"No class declaration found — declare the class. Constraint: {_manifest.ClassNameInstruction()}");
            else if (!classMatch.Groups["name"].Value.EndsWith(_manifest.RequiredClassNameSuffix, StringComparison.Ordinal))
                violations.Add($"Class name '{classMatch.Groups["name"].Value}' has the wrong suffix — rename it. Constraint: {_manifest.ClassNameInstruction()}");
        }

        foreach (var declaration in _manifest.RequiredDeclarations)
        {
            if (content.IndexOf(declaration, StringComparison.Ordinal) < 0)
                violations.Add($"Required declaration is missing — add it. Constraint: {_manifest.RequiredDeclarationInstruction(declaration)}");
        }

        return violations.Count == 0
            ? new ValueTask<(bool, string?)>((true, (string?)null))
            : new ValueTask<(bool, string?)>((false, string.Join(" | ", violations)));
    }
}
