using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Roslyn-backed <see cref="IPostApplyVerification"/>: the A4 canary. After an admitted extension's
/// files land on disk, this recompiles them AS WRITTEN (read back from the working tree, not from the
/// in-memory proposal) against the running application's loaded assemblies, in-process — no .NET SDK,
/// so a deployed node runs it too.
///
/// <para>Why re-check what A2 already compiled: A2's <see cref="IExtensionCompileCheck"/> runs on the
/// proposal's in-memory content BEFORE admission. This runs on the BYTES THAT ACTUALLY LANDED, after
/// the mediated write — so any divergence between what was decided and what reached disk (an encoding
/// surprise, a truncated write, a concurrent edit) is caught here and reverted, rather than left on an
/// unattended node. It is defense-in-depth on the auto-apply path, and the seam a stronger canary (a
/// full build, a test course, a runtime probe) plugs into unchanged.</para>
///
/// <para>Emit()s to a <see cref="MemoryStream"/> and never loads or runs the assembly, and attaches no
/// analyzers or source generators — so verifying untrusted model output executes nothing. Fail-closed:
/// an unreadable file or any verifier error is a FAILED verification, never a pass.</para>
///
/// <para><b>Honest scope.</b> This compiles the CHANGED <c>.cs</c> files as a standalone assembly — it
/// does not rebuild the project, run tests, or exercise the node. So a PASS means "the change compiles
/// in isolation", NOT "the node still builds and runs": a change that re-signatures a member other
/// files depend on, or that only touches non-<c>.cs</c> files, passes here. It is shallow
/// defense-in-depth and the seam a stronger canary (a full build, a test course, a runtime probe) plugs
/// into — not a correctness proof. It errs conservative: because the reference set is the process's
/// currently-loaded assemblies, a valid change referencing a not-yet-loaded dependency can be rolled
/// back as a false positive — the safe direction (never a false pass).</para>
/// </summary>
public sealed class RoslynPostApplyVerification : IPostApplyVerification
{
    private readonly ILogger<RoslynPostApplyVerification>? _logger;

    /// <summary>Creates the post-apply verifier.</summary>
    public RoslynPostApplyVerification(ILogger<RoslynPostApplyVerification>? logger = null) => _logger = logger;

    /// <inheritdoc />
    public Task<PostApplyVerificationResult> VerifyAsync(
        string repoRoot,
        IReadOnlyList<AppliedFile> applied,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var csFiles = applied
                .Where(f => f.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (csFiles.Count == 0)
            {
                return new PostApplyVerificationResult(true, "no .cs files applied — nothing to verify");
            }

            // Match a realistic project as closely as the in-process check can, so a VALID change is
            // not spuriously rolled back: allow unsafe blocks and the latest language version.
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            var trees = new List<SyntaxTree>(csFiles.Count);
            foreach (var f in csFiles)
            {
                string content;
                try
                {
                    content = File.ReadAllText(f.FullPath);
                }
                catch (Exception ex)
                {
                    // Fail-closed: a file the gate just admitted that cannot now be read back is a
                    // post-apply anomaly, not a pass. The caller rolls the batch back.
                    _logger?.LogWarning(ex, "Post-apply verify: could not read applied file {Path}", f.RelativePath);
                    return new PostApplyVerificationResult(false, $"could not read applied file '{f.RelativePath}': {ex.Message}");
                }
                trees.Add(CSharpSyntaxTree.ParseText(content, parseOptions, path: f.RelativePath, cancellationToken: cancellationToken));
            }

            var compilation = CSharpCompilation.Create(
                "ashlar-postapply-" + Guid.NewGuid().ToString("N"),
                trees,
                BuildReferenceSet(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            using var ms = new MemoryStream();
            var emit = compilation.Emit(ms, cancellationToken: cancellationToken);
            if (emit.Success)
            {
                return new PostApplyVerificationResult(true, $"{csFiles.Count} applied file(s) verified clean");
            }

            var errors = emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            var shown = string.Join("; ", errors.Take(3).Select(e => e.GetMessage()));
            _logger?.LogWarning("Post-apply verify failed: {ErrorCount} error(s)", errors.Count);
            return new PostApplyVerificationResult(false, $"{errors.Count} post-apply error(s): {Truncate(shown, 300)}");
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
