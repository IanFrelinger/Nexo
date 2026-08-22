using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ashlar.Abstractions;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Roslyn-powered source analyzer for enforcing consistency rules on generated C# code.
/// Designed for offline use and CI; does not require MSBuild workspace.
/// </summary>
public sealed class RoslynAnalyzeTool : ITool
{
    public string Id => "roslyn.analyze";

    public ToolSchema Schema => new(Id, "Analyze C# source files via Roslyn (consistency rules)", """
    {
      "type":"object",
      "required":["root","files","rules"],
      "properties":{
        "root":{"type":"string"},
        "files":{"type":"array","items":{"type":"string"}},
        "rules":{
          "type":"object",
          "properties":{
            "requiredNamespace":{"type":"string"},
            "requireFileScopedNamespace":{"type":"boolean"},
            "requiredBaseType":{"type":"string"},
            "requirePublic":{"type":"boolean"},
            "requireSealed":{"type":"boolean"},
            "requiredClassName":{"type":"string"},
            "requiredCommandName":{"type":"string"}
          }
        }
      }
    }
    """);

    private sealed record Args(string root, string[] files, Rules rules);

    private sealed record Rules
    {
        public string? requiredNamespace { get; init; }
        public bool requireFileScopedNamespace { get; init; } = true;
        public string? requiredBaseType { get; init; }
        public bool requirePublic { get; init; } = true;
        public bool requireSealed { get; init; } = true;
        public string? requiredClassName { get; init; }
        public string? requiredCommandName { get; init; }
    }

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;

        var root = args.root;
        var violations = new List<object>();

        foreach (var rel in args.files ?? Array.Empty<string>())
        {
            ct.ThrowIfCancellationRequested();

            var filePath = Path.IsPathRooted(rel) ? rel : Path.Combine(root, rel);
            if (!File.Exists(filePath))
            {
                violations.Add(new
                {
                    rule = "Roslyn.MissingFile",
                    message = $"File not found: {rel}",
                    filePath = rel,
                    lineNumber = (int?)null,
                    severity = "High"
                });
                continue;
            }

            var text = await File.ReadAllTextAsync(filePath, ct);
            AnalyzeFile(rel, text, args.rules, violations);
        }

        var ok = violations.Count == 0;
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"roslyn:ok={ok.ToString().ToLowerInvariant()}");
        delta.AddLog($"roslyn:violations={violations.Count}");

        return new ToolResult(delta, new { ok, violations });
    }

    private static void AnalyzeFile(string relPath, string source, Rules rules, List<object> violations)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: relPath);
        var root = tree.GetCompilationUnitRoot();

        var fileNs = GetNamespace(root);
        if (!string.IsNullOrWhiteSpace(rules.requiredNamespace) &&
            !string.Equals(fileNs, rules.requiredNamespace, StringComparison.Ordinal))
        {
            AddViolation(violations, "Roslyn.Namespace", $"Expected namespace '{rules.requiredNamespace}', got '{fileNs ?? "<none>"}'", relPath, line: GetNamespaceLine(root));
        }

        if (rules.requireFileScopedNamespace && root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault() == null)
        {
            AddViolation(violations, "Roslyn.NamespaceStyle", "Expected file-scoped namespace declaration", relPath, line: GetNamespaceLine(root));
        }

        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDecl == null)
        {
            AddViolation(violations, "Roslyn.Class", "No class declaration found", relPath, line: 1);
            return;
        }

        if (!string.IsNullOrWhiteSpace(rules.requiredClassName) &&
            !string.Equals(classDecl.Identifier.Text, rules.requiredClassName, StringComparison.Ordinal))
        {
            AddViolation(violations, "Roslyn.ClassName", $"Expected class '{rules.requiredClassName}', got '{classDecl.Identifier.Text}'", relPath, GetLine(classDecl));
        }

        if (rules.requirePublic && !classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
        {
            AddViolation(violations, "Roslyn.Modifiers", "Expected 'public' modifier on command class", relPath, GetLine(classDecl));
        }

        if (rules.requireSealed && !classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)))
        {
            AddViolation(violations, "Roslyn.Modifiers", "Expected 'sealed' modifier on command class", relPath, GetLine(classDecl));
        }

        if (!string.IsNullOrWhiteSpace(rules.requiredBaseType))
        {
            var inherits = classDecl.BaseList?.Types
                .Select(t => t.Type)
                .OfType<IdentifierNameSyntax>()
                .Any(id => string.Equals(id.Identifier.Text, rules.requiredBaseType, StringComparison.Ordinal)) == true;
            if (!inherits)
            {
                AddViolation(violations, "Roslyn.BaseType", $"Expected base type '{rules.requiredBaseType}'", relPath, GetLine(classDecl));
            }
        }

        if (!string.IsNullOrWhiteSpace(rules.requiredCommandName))
        {
            var ctor = classDecl.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var baseArgs = ctor?.Initializer?.ArgumentList?.Arguments;
            var firstArg = baseArgs?.FirstOrDefault()?.Expression as LiteralExpressionSyntax;
            var cmdName = firstArg?.Token.ValueText;
            if (!string.Equals(cmdName, rules.requiredCommandName, StringComparison.Ordinal))
            {
                AddViolation(violations, "Roslyn.CommandName", $"Expected base() command name '{rules.requiredCommandName}', got '{cmdName ?? "<none>"}'", relPath, ctor != null ? GetLine(ctor) : GetLine(classDecl));
            }
        }
    }

    private static string? GetNamespace(CompilationUnitSyntax root)
    {
        var f = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (f != null) return f.Name.ToString();
        var b = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return b?.Name.ToString();
    }

    private static int GetNamespaceLine(CompilationUnitSyntax root)
    {
        var ns = (SyntaxNode?)root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()
                 ?? root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return ns == null ? 1 : GetLine(ns);
    }

    private static int GetLine(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return span.StartLinePosition.Line + 1;
    }

    private static void AddViolation(List<object> violations, string rule, string message, string filePath, int line)
    {
        violations.Add(new
        {
            rule,
            message,
            filePath,
            lineNumber = line,
            severity = "High"
        });
    }
}

