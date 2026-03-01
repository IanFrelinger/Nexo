using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Domain.Values;

namespace Nexo.Infrastructure.Analysis.BrickAnalyzer;

/// <summary>
/// Roslyn-based static analyzer for brick code. Validates schema, safety, and performance.
/// Dogfood: run against Block 1 (Observation) code first.
/// </summary>
public sealed class RoslynBrickStaticAnalyzer : IBrickStaticAnalyzer
{
    private readonly ILogger<RoslynBrickStaticAnalyzer>? _logger;

    public RoslynBrickStaticAnalyzer(ILogger<RoslynBrickStaticAnalyzer>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<BrickStaticAnalysisResult> AnalyzeSourceAsync(string sourcePath, bool includeAnalyzers = false, CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();
        var files = GatherSourceFiles(sourcePath, includeAnalyzers);

        foreach (var file in files)
        {
            try
            {
                var source = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
                var root = tree.GetRoot(cancellationToken);

                AnalyzeBlockingInAsync(root, file, violations);
                AnalyzeEmptyCatch(root, file, violations);
                AnalyzeDangerousApis(root, file, violations);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to analyze {File}", file);
                violations.Add(new Violation
                {
                    Rule = "ParseError",
                    Message = $"Failed to parse: {ex.Message}",
                    FilePath = file,
                    Severity = RiskLevel.Medium,
                });
            }
        }

        return Task.FromResult(new BrickStaticAnalysisResult
        {
            Passed = violations.Count == 0,
            Violations = violations,
        });
    }

    private static List<string> GatherSourceFiles(string path, bool includeAnalyzers)
    {
        var list = new List<string>();
        var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(Path.GetFullPath(path));
            if (!includeAnalyzers)
                return list;
        }
        else if (Directory.Exists(path))
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                list.Add(file);
        }

        if (includeAnalyzers && dir != null)
        {
            var repoRoot = dir;
            while (repoRoot != null && !File.Exists(Path.Combine(repoRoot, "Nexo.sln")))
                repoRoot = Path.GetDirectoryName(repoRoot);
            if (repoRoot != null)
            {
                var analysisPath = Path.Combine(repoRoot, "src", "Nexo.Infrastructure", "Analysis");
                var adaptationPath = Path.Combine(repoRoot, "src", "Nexo.Infrastructure", "Adaptation");
                foreach (var d in new[] { analysisPath, adaptationPath })
                {
                    if (Directory.Exists(d))
                    {
                        foreach (var file in Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
                        {
                            if (!list.Contains(file))
                                list.Add(file);
                        }
                    }
                }
            }
        }
        return list;
    }

    private static void AnalyzeBlockingInAsync(SyntaxNode root, string filePath, List<Violation> violations)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(SyntaxKind.AsyncKeyword)))
        {
            foreach (var ma in method.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var name = ma.Name.Identifier.Text;
                if (name is "Result" or "Wait" or "GetAwaiter")
                {
                    var line = ma.SyntaxTree?.GetLineSpan(ma.Span).StartLinePosition.Line + 1;
                    violations.Add(new Violation
                    {
                        Rule = "BlockingInAsync",
                        Message = "Blocking call (.Result, .Wait, .GetAwaiter().GetResult()) in async method can cause deadlocks. Use await instead.",
                        FilePath = filePath,
                        LineNumber = line,
                        Severity = RiskLevel.High,
                    });
                }
            }
        }
    }

    private static void AnalyzeEmptyCatch(SyntaxNode root, string filePath, List<Violation> violations)
    {
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            var block = catchClause.Block;
            var statements = block.Statements;
            if (statements.Count == 0)
            {
                var line = catchClause.SyntaxTree?.GetLineSpan(catchClause.Span).StartLinePosition.Line + 1;
                violations.Add(new Violation
                {
                    Rule = "EmptyCatch",
                    Message = "Empty catch block swallows exceptions. Log or rethrow.",
                    FilePath = filePath,
                    LineNumber = line,
                    Severity = RiskLevel.Medium,
                });
            }
        }
    }

    private static void AnalyzeDangerousApis(SyntaxNode root, string filePath, List<Violation> violations)
    {
        foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = inv.Expression.ToString();
            if (name.Contains("Process.Start", StringComparison.Ordinal) ||
                name.Contains("ProcessStartInfo", StringComparison.Ordinal))
            {
                var line = inv.SyntaxTree?.GetLineSpan(inv.Span).StartLinePosition.Line + 1;
                violations.Add(new Violation
                {
                    Rule = "DangerousApi",
                    Message = "Process.Start in application-scope code. Verify permission scope allows OS-level operations.",
                    FilePath = filePath,
                    LineNumber = line,
                    Severity = RiskLevel.Medium,
                });
            }
        }
    }

}
