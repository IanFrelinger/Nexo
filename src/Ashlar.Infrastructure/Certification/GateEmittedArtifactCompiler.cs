using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The certifier's own compile. Same wrap as the analyzer and mutation legs, closed-world
/// options, emit to memory. The resulting bytes are what discovery, the IL fence, witness
/// activation, the record, and the exporter all see.
/// </summary>
public static class GateEmittedArtifactCompiler
{
    /// <summary>Assembly name used for every gate-emitted compile (stable for hashing of IL shape, not of MVID).</summary>
    public const string AssemblyName = "Ashlar.GateEmittedBrick";

    /// <summary>Compiles <paramref name="sourceCode"/> under closed-world options.</summary>
    public static GateEmittedArtifact Compile(
        string sourceCode,
        IReadOnlyList<string>? compilationReferences = null)
    {
        if (string.IsNullOrEmpty(sourceCode))
            throw new InvalidOperationException("gate-emitted compile refused: source is empty");

        var wrapped = CandidateSourceWrapper.Wrap(sourceCode);
        var tree = CSharpSyntaxTree.ParseText(wrapped, Ashlar.Infrastructure.Certification.BrickCompileOptions.ParseOptions);
        var references = RoslynCodeAnalysisService.BuildReferenceSet(compilationReferences);
        references.Add(MetadataReference.CreateFromFile(typeof(DomainBrick).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            AssemblyName,
            new[] { tree },
            references,
            Ashlar.Infrastructure.Certification.BrickCompileOptions.CompilationOptions);

        using var peStream = new MemoryStream();
        var emit = compilation.Emit(peStream);
        if (!emit.Success)
        {
            var errors = string.Join(
                " | ",
                emit.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Take(8)
                    .Select(d => d.GetMessage()));
            throw new InvalidOperationException("gate-emitted compile failed: " + errors);
        }

        var bytes = peStream.ToArray();
        var typeName = MetadataBrickDiscovery.DiscoverBrickTypeName(bytes);
        return new GateEmittedArtifact
        {
            AssemblyBytes = bytes,
            AssemblySha256 = BrickContentHasher.ComputeSha256(bytes),
            SourceSha256 = BrickContentHasher.ComputeSha256(sourceCode),
            BrickTypeName = typeName,
            CompileOptionsBlob = Ashlar.Infrastructure.Certification.BrickCompileOptions.CanonicalBlob,
            CompilerVersion = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString() ?? "unknown"
        };
    }
}
