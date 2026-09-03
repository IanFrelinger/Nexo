using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace Ashlar.Infrastructure.Certification;

/// <summary>One source file the C# compiler recorded as part of a compilation it performed.</summary>
/// <param name="Path">The path csc saw, as it wrote it into the debug information.</param>
/// <param name="Algorithm">The checksum algorithm, by GUID, or <c>null</c> when unrecognised.</param>
/// <param name="Checksum">The compiler's checksum of the file's bytes as it read them.</param>
internal sealed record CompiledDocument(string Path, string? Algorithm, byte[] Checksum);

/// <summary>
/// The source files a built assembly was ACTUALLY compiled from, read out of the compiler's own
/// record of the compilation rather than out of MSBuild's state.
/// </summary>
/// <remarks>
/// <para>This type exists because MSBuild's <c>@(Compile)</c> item list — however carefully it is
/// obtained — is not an authority on what was compiled. It is mutable state belonging to the
/// project being judged, and the project's own targets run before the gate can read it. Two
/// bypasses were reproduced live on this repo against a gate that trusted it:</para>
///
/// <list type="number">
/// <item><description>A <c>&lt;Target BeforeTargets="CoreCompile"&gt;</c> whose Condition tests a
/// property the two runs disagree about — <c>$(OutputPath)</c> is the obvious one, since the gate
/// builds into a temp directory and any second query does not. The target then adds its payload
/// during the real build and stays dormant during the check.</description></item>
/// <item><description>A second target, <c>AfterTargets="CoreCompile"</c>, that simply REMOVES the
/// payload from <c>@(Compile)</c> again. The compile already happened; the item list the gate reads
/// afterwards is clean, and the payload is in the assembly. This one defeats a query issued from
/// the build's own invocation, so "ask in the same process" does not close it either.</description></item>
/// </list>
///
/// <para>Both are the same fact as the four bypasses before them: an answer taken from something
/// ADJACENT to the compilation is a model of the compilation. The compiler's own record is not
/// adjacent — csc writes the document table while emitting, from the syntax trees it actually
/// parsed, and no MSBuild target can edit it after the fact without changing the assembly too. So
/// this is the authority the gate compares against, and MSBuild's answer is demoted to what it can
/// honestly be: a way to NARROW a tolerance, never a way to grant one.</para>
///
/// <para>The record also carries the compiler's checksum of each file, which buys a second
/// guarantee for free: the bytes the gate hashed are the bytes that were compiled. Without it a
/// candidate could be rewritten between the hash and the build and the certificate would bind the
/// wrong text with every path check still passing.</para>
///
/// <para>Every failure here is a REFUSAL. No debug information, an unreadable PDB, an unrecognised
/// checksum algorithm: each of those is "the gate cannot establish what was compiled", and the one
/// invariant this area keeps rediscovering is that such a state is refused rather than assumed
/// away. The loader forces <c>DebugType=portable</c>, <c>ChecksumAlgorithm=SHA256</c> and an empty
/// <c>PathMap</c> as GLOBAL properties precisely so an honest project cannot land here by accident
/// — a global property cannot be overridden from inside the project being judged.</para>
///
/// <para><strong>What this does NOT establish, stated plainly so the next round does not have to
/// rediscover it.</strong> The document table is the compiler's record, and it is bound to the
/// assembly (the PDB's id must match the assembly's debug directory, which
/// <c>TryOpenAssociatedPortablePdb</c> enforces) — so a PDB from a different compilation is
/// rejected, and a real compilation cannot omit a file it compiled. It is not, however, a proof
/// about the IL. An MSBuild target that runs after the build and writes BOTH a forged assembly and
/// a matching forged PDB would satisfy every check here: the gate would be reading a record the
/// candidate authored rather than one csc authored. Closing that means comparing the shipped IL
/// against a compilation the gate performs itself from the hashed source, which is a different and
/// much larger mechanism than this file. Until that exists, the honest claim is "no file reaches
/// the compilation without being hashed", NOT "the assembly provably contains only the hashed
/// source".</para>
/// </remarks>
internal static class CompiledSourceDocuments
{
    // Roslyn's source-hash algorithm GUIDs (System.Reflection.Metadata does not expose them).
    private static readonly Guid Sha1 = new("ff1816ec-aa5e-4d10-87f7-6f4963833460");
    private static readonly Guid Sha256 = new("8829d00f-11b8-4213-878b-770e8597ac16");

    /// <summary>
    /// Every source document recorded in <paramref name="assemblyPath"/>'s portable PDB, or a
    /// refusal explaining why the compiled set could not be established.
    /// </summary>
    public static IReadOnlyList<CompiledDocument> Read(string assemblyPath)
    {
        MetadataReaderProvider? provider = null;
        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var pe = new PEReader(stream);
            // Embedded and side-by-side PDBs both land here. The resolver also looks beside the
            // assembly, because a build that copies its output elsewhere leaves the debug directory
            // pointing at the intermediate copy.
            if (!pe.TryOpenAssociatedPortablePdb(assemblyPath, path => OpenCandidate(assemblyPath, path), out provider, out _)
                || provider is null)
            {
                throw new InvalidOperationException(Refusal(assemblyPath,
                    "it carries no portable debug information, so the compiler left no record of which files it "
                    + "compiled"));
            }

            var reader = provider.GetMetadataReader();
            var documents = new List<CompiledDocument>(reader.Documents.Count);
            foreach (var handle in reader.Documents)
            {
                var document = reader.GetDocument(handle);
                var name = reader.GetString(document.Name);
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException(Refusal(assemblyPath,
                        "the compiler recorded a source document with no name, so the gate cannot say which file "
                        + "it was"));
                }

                var algorithm = reader.GetGuid(document.HashAlgorithm);
                documents.Add(new CompiledDocument(
                    name,
                    algorithm == Sha256 ? "SHA256" : algorithm == Sha1 ? "SHA1" : null,
                    reader.GetBlobBytes(document.Hash)));
            }

            return documents;
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(Refusal(assemblyPath,
                $"its debug information could not be read ({ex.Message})"), ex);
        }
        finally
        {
            provider?.Dispose();
        }
    }

    /// <summary>
    /// True when <paramref name="certifiedBytes"/> are the bytes the compiler hashed for
    /// <paramref name="document"/>.
    /// </summary>
    /// <remarks>
    /// <para>The caller passes the bytes the CERTIFICATE was taken over, captured before the build,
    /// rather than a path to re-read. Re-reading is the version of this check that cannot fail: a
    /// target that rewrites the brick source on its way to the compiler leaves the file on disk
    /// equal to what was compiled, so the comparison passes on exactly the case it exists for. That
    /// mistake was made here first and caught by the test for it.</para>
    ///
    /// <para>An unrecognised algorithm, or a document the compiler recorded no checksum for,
    /// returns false: "cannot confirm" and "does not match" are the same verdict when the answer
    /// gets signed.</para>
    /// </remarks>
    public static bool ContentMatches(CompiledDocument document, byte[] certifiedBytes)
    {
        if (document.Algorithm is null || document.Checksum.Length == 0)
        {
            return false;
        }

        var actual = document.Algorithm == "SHA256"
            ? SHA256.HashData(certifiedBytes)
            : SHA1.HashData(certifiedBytes);
        return CryptographicOperations.FixedTimeEquals(actual, document.Checksum);
    }

    /// <summary>
    /// Opens the PDB the assembly's debug directory names, looking first where it was recorded and
    /// then beside the assembly. Shared with <see cref="CompiledMetadataReferences"/>, which reads a
    /// different table out of the same record.
    /// </summary>
    internal static Stream? OpenCandidate(string assemblyPath, string recordedPath)
    {
        foreach (var candidate in new[]
                 {
                     recordedPath,
                     Path.Combine(Path.GetDirectoryName(assemblyPath) ?? ".", Path.GetFileName(recordedPath))
                 })
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return File.OpenRead(candidate);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Try the next candidate; exhausting them is the refusal above.
            }
        }

        return null;
    }

    private static string Refusal(string assemblyPath, string because) =>
        $"Brick project refused: the gate could not read what {Path.GetFileName(assemblyPath)} was compiled from — "
        + because + ". Certification signs a content hash over the COMPILED source set, and the compiler's own "
        + "record of that set is what the gate compares against, so a build that leaves no such record cannot be "
        + "certified. The gate already forces <DebugType>portable</DebugType> as a global MSBuild property, which "
        + "the project cannot override, so reaching this means something in the build actively removed or replaced "
        + "the record. Fix: remove the MSBuild target in the brick project or in the Directory.Build.props / "
        + "Directory.Build.targets beside it that deletes the .pdb, or that invokes the compiler itself instead of "
        + "letting CoreCompile run, and certify again.";
}
