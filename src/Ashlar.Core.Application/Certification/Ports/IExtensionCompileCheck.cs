namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>A proposed file's repo-relative path and its full new content.</summary>
public sealed record ProposedFileContent(string Path, string Content);

/// <summary>
/// The result of compiling a proposed extension's source. <see cref="Passed"/> = false MUST block an
/// autonomous admission: a change that does not compile is never admissible on its own evidence.
/// </summary>
public sealed record ExtensionCompileCheckResult(bool Passed, string Detail);

/// <summary>
/// Compiles the source of a proposed extension so the admission gate can decide on RUN evidence
/// rather than a self-reported claim. This closes the core autonomy-safety gap: without it a
/// self-extending node admits (and, later, applies) a brick with zero correctness evidence.
///
/// <para>Implemented over Roslyn, in-process — no .NET SDK is required — so a deployed node
/// (aspnet runtime, no SDK) can still verify its own proposals before admitting them.</para>
/// </summary>
public interface IExtensionCompileCheck
{
    /// <summary>
    /// Compiles the <c>.cs</c> files among <paramref name="files"/> together against the running
    /// application's reference set. Non-code files (docs, config) are ignored; a proposal with no
    /// code compiles trivially. Never throws for a compile failure — it returns
    /// <see cref="ExtensionCompileCheckResult.Passed"/> = false with the diagnostics.
    /// </summary>
    Task<ExtensionCompileCheckResult> CheckAsync(
        IReadOnlyList<ProposedFileContent> files,
        CancellationToken cancellationToken = default);
}
