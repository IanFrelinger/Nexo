using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Infrastructure.Certification;

internal sealed class BrickMutationEngine
{
    private static readonly IReadOnlyList<MutationDefinition> Catalog =
    [
        new("flip-gt-error-count", @"errorCount > 0", "errorCount < 0"),
        new("flip-lt-index", @"idx < 0", "idx > 0"),
        new("off-by-one-plus", @"\(idx \+ 5\)", "(idx + 4)"),
        new("negate-contains", @"\.Contains\(""ERROR""", @".Contains(""NOPE"""),
        new("drop-error-branch", @"if \(line\.Contains\(""ERROR"".*\)\)\s*\r?\n\s*errorLines\.Add\(line\);", "// dropped branch"),
        new(
            "mutate-first-message-only",
            @"var firstErrorMessage = errorCount > 0 \? ExtractErrorMessage\(errorLines\[0\]\) : string\.Empty;",
            @"var firstErrorMessage = ""escaped-mutant"";"),
    ];

    public IReadOnlyList<MutationDefinition> GetCatalog() => Catalog;

    public async Task<MutationTestResult> RunAsync(
        string sourceCode,
        string brickTypeName,
        WitnessSpec witness,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken)
    {
        var survivors = new List<string>();
        var killed = new List<string>();

        foreach (var mutation in Catalog)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!mutation.Pattern.IsMatch(sourceCode))
                continue;

            var mutatedSource = mutation.Pattern.Replace(sourceCode, mutation.Replacement, 1);
            if (string.Equals(mutatedSource, sourceCode, StringComparison.Ordinal))
                continue;

            var mutant = await TryCompileMutantAsync(
                mutatedSource,
                brickTypeName,
                compilationReferences,
                cancellationToken).ConfigureAwait(false);

            if (mutant is null)
            {
                killed.Add(mutation.Id);
                continue;
            }

            var witnessPassed = await MutantWitnessExecutor.RunWitnessAsync(
                mutant.Instance,
                mutant.Assembly,
                witness,
                cancellationToken).ConfigureAwait(false);

            mutant.LoadContext.Unload();

            if (witnessPassed)
                survivors.Add(mutation.Id);
            else
                killed.Add(mutation.Id);
        }

        var total = survivors.Count + killed.Count;
        var escapeRate = total == 0 ? 0d : (double)survivors.Count / total;
        return new MutationTestResult(total, survivors, killed, escapeRate);
    }

    private static async Task<CompiledMutant?> TryCompileMutantAsync(
        string sourceCode,
        string brickTypeName,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-cert-mut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "mutant.dll");

        try
        {
            var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
            var compile = await compiler.CompileAsync(
                WrapWithGlobalUsings(sourceCode),
                "MutantBrick",
                outputPath,
                compilationReferences,
                cancellationToken).ConfigureAwait(false);

            if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath) || !File.Exists(compile.AssemblyPath))
                return null;

            var loadContext = new MutantAssemblyLoadContext();
            var assembly = loadContext.LoadFromAssemblyPath(compile.AssemblyPath);
            var type = assembly.GetType(brickTypeName) ?? assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name != "CertAuditContext");
            if (type is null)
            {
                loadContext.Unload();
                return null;
            }

            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                loadContext.Unload();
                return null;
            }

            return new CompiledMutant(instance, assembly, loadContext);
        }
        catch
        {
            return null;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static string WrapWithGlobalUsings(string sourceCode)
    {
        const string systemUsings = """
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

""";

        const string auditContext = """

internal sealed class CertAuditContext : Nexo.Core.Domain.Execution.IExecutionContext
{
    public string AgentId => "cert-gate";
    public string BehaviorId => "cert-gate";
    public bool IsAirGapped => true;
    public bool AuditMode => true;
    public string Provider => "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}

""";

        var namespaceIndex = sourceCode.IndexOf("namespace ", StringComparison.Ordinal);
        if (namespaceIndex < 0)
            return systemUsings + sourceCode;

        var braceIndex = sourceCode.IndexOf('{', namespaceIndex);
        if (braceIndex < 0)
            return systemUsings + sourceCode;

        return systemUsings + sourceCode.Insert(braceIndex + 1, auditContext);
    }
}

internal sealed record CompiledMutant(object Instance, Assembly Assembly, MutantAssemblyLoadContext LoadContext);

internal sealed class MutantAssemblyLoadContext : AssemblyLoadContext
{
    public MutantAssemblyLoadContext() : base(isCollectible: true)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}

internal sealed class MutationDefinition
{
    public MutationDefinition(string id, string pattern, string replacement)
    {
        Id = id;
        Pattern = new Regex(pattern, RegexOptions.Multiline);
        Replacement = replacement;
    }

    public string Id { get; }
    public Regex Pattern { get; }
    public string Replacement { get; }
}

internal sealed record MutationTestResult(
    int TotalMutants,
    IReadOnlyList<string> SurvivingMutantIds,
    IReadOnlyList<string> KilledMutantIds,
    double EscapeRate);
