namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// Assembly the certifier compiled, discovered, fenced, and will ship. The same bytes are
/// the judged program and the released artifact, so "judged ≠ shipped" has no representation.
/// </summary>
public sealed record GateEmittedArtifact
{
    /// <summary>Raw PE bytes produced by the certifier's closed-world compile.</summary>
    public required byte[] AssemblyBytes { get; init; }

    /// <summary>Base64 SHA-256 of <see cref="AssemblyBytes"/>.</summary>
    public required string AssemblySha256 { get; init; }

    /// <summary>Base64 SHA-256 of the exact source text the certifier compiled.</summary>
    public required string SourceSha256 { get; init; }

    /// <summary>Metadata-discovered concrete brick type (no constructor ran to find this).</summary>
    public required string BrickTypeName { get; init; }

    /// <summary>Canonical compile-options blob (language version, unsafe, overflow, output kind).</summary>
    public required string CompileOptionsBlob { get; init; }

    /// <summary>Compiler product/version string recorded as certifier identity evidence.</summary>
    public required string CompilerVersion { get; init; }
}
