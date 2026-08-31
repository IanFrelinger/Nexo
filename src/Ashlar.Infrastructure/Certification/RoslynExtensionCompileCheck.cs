using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Roslyn-backed <see cref="IExtensionCompileCheck"/>: compiles a proposal's <c>.cs</c> files
/// in-process (no .NET SDK) against the running application's loaded assemblies, so the admission
/// gate can require a REAL <c>build</c> course instead of a self-reported one.
///
/// <para>The reference set is <see cref="AppDomain.CurrentDomain"/>'s assemblies — framework plus
/// the Ashlar brick contracts already loaded in THIS process — which is the realistic reference set
/// for a brick destined to load in-process. A proposal that references project-private types not
/// loaded here will fail to compile in isolation and be held for a human: a conservative,
/// fail-closed outcome, never a false admission.</para>
/// </summary>
public sealed class RoslynExtensionCompileCheck : IExtensionCompileCheck
{
    private readonly ILogger<RoslynExtensionCompileCheck>? _logger;

    /// <summary>Creates the compile check.</summary>
    public RoslynExtensionCompileCheck(ILogger<RoslynExtensionCompileCheck>? logger = null) => _logger = logger;

    /// <inheritdoc />
    public Task<ExtensionCompileCheckResult> CheckAsync(
        IReadOnlyList<ProposedFileContent> files,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var csFiles = files
                .Where(f => f.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (csFiles.Count == 0)
            {
                return new ExtensionCompileCheckResult(true, "no .cs files in the proposal — nothing to compile");
            }

            var trees = csFiles
                .Select(f => CSharpSyntaxTree.ParseText(f.Content, path: f.Path, cancellationToken: cancellationToken))
                .ToList();

            var compilation = CSharpCompilation.Create(
                "ashlar-proposal-" + Guid.NewGuid().ToString("N"),
                trees,
                BuildReferenceSet(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var emit = compilation.Emit(ms, cancellationToken: cancellationToken);
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (emit.Success)
            {
                return new ExtensionCompileCheckResult(true, $"{csFiles.Count} file(s) compiled clean");
            }

            var shown = string.Join("; ", errors.Take(3).Select(e => e.GetMessage()));
            _logger?.LogInformation("Proposal compile check failed: {ErrorCount} error(s)", errors.Count);
            return new ExtensionCompileCheckResult(false, $"{errors.Count} compile error(s): {Truncate(shown, 300)}");
        }, cancellationToken);
    }

    private static IReadOnlyList<MetadataReference> BuildReferenceSet()
    {
        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.IsDynamic)
                {
                    continue;
                }
                var location = asm.Location;
                if (!string.IsNullOrEmpty(location) && seen.Add(location))
                {
                    refs.Add(MetadataReference.CreateFromFile(location));
                }
            }
            catch
            {
                // An assembly whose metadata cannot be read is simply not offered as a reference.
            }
        }
        return refs;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
