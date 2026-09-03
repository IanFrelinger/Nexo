using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Ashlar.Infrastructure.Certification;

/// <summary>One assembly the C# compiler recorded compiling AGAINST.</summary>
/// <param name="FileName">The reference's file name as csc saw it — a name, not a path; the
/// compiler deliberately records no directory.</param>
/// <param name="Mvid">The module version id of the assembly csc opened. Two files with the same
/// MVID are the same compiled module; a rebuilt, patched or forged copy has a different one.</param>
/// <param name="IsAssembly">True for an assembly reference, false for a netmodule.</param>
/// <param name="EmbedInteropTypes">True when the reference was embedded rather than linked.</param>
internal sealed record CompiledMetadataReference(string FileName, Guid Mvid, bool IsAssembly, bool EmbedInteropTypes);

/// <summary>
/// The assemblies a built brick was ACTUALLY compiled against, read out of the compiler's own
/// record of the compilation rather than out of the build's output directory or MSBuild's state.
/// </summary>
/// <remarks>
/// <para>The gate re-compiles the brick source itself — once for the analyzer fence and once per
/// mutant — and needs the same references csc had, or the fence cannot resolve
/// <c>Ashlar.Core.Domain.Bricks.Brick</c> and refuses (correctly) because every brick rule would
/// no-op. The reference set used to be whatever <c>*.dll</c> lay in the build output. The SDK does
/// not copy package assemblies there (<c>CopyLocalLockFileAssemblies</c> is off by default for a
/// library), so a stock brick that referenced <c>Ashlar.Authoring</c> produced an output holding
/// only itself, and the fence refused every scaffolded brick until the author added an MSBuild
/// property the docs described as a thing that "will bite you". The output directory was a model
/// of the compiler's reference set, in exactly the way the <c>*.cs</c> glob was a model of its
/// source set, and it was wrong in exactly the same direction.</para>
///
/// <para>Alongside the source-document table (<see cref="CompiledSourceDocuments"/>), csc writes
/// a second record into the portable PDB: one entry per metadata reference, carrying the file
/// name and the MVID of the module it opened. That is the compiler's answer to "what did you
/// compile against", written while emitting, and it is what this type reads. MSBuild's
/// <c>ReferencePathWithRefAssemblies</c> item list — the paths <c>CoreCompile</c> handed csc —
/// says where those assemblies live, and the loader accepts a path for a recorded reference only
/// when the file at that path has the recorded MVID. A path list edited after the compile, or an
/// assembly swapped under a path, matches nothing and is refused.</para>
///
/// <para>Every failure here is a REFUSAL. An assembly with no portable PDB, a PDB with no reference
/// record, a record that will not parse: each is "the gate cannot establish what the brick was
/// compiled against", and a reference set the gate cannot establish is one it must not guess at —
/// the guess is what the output-directory glob was.</para>
/// </remarks>
internal static class CompiledMetadataReferences
{
    // Roslyn's custom-debug-information kind for the compilation's metadata references
    // (PortableCustomDebugInfoKinds.CompilationMetadataReferences; System.Reflection.Metadata
    // does not expose it).
    private static readonly Guid CompilationMetadataReferencesKind = new("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

    /// <summary>
    /// Every metadata reference recorded in <paramref name="assemblyPath"/>'s portable PDB, or a
    /// refusal explaining why the compiled-against set could not be established.
    /// </summary>
    public static IReadOnlyList<CompiledMetadataReference> Read(string assemblyPath)
    {
        MetadataReaderProvider? provider = null;
        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var pe = new PEReader(stream);
            if (!pe.TryOpenAssociatedPortablePdb(
                    assemblyPath,
                    path => CompiledSourceDocuments.OpenCandidate(assemblyPath, path),
                    out provider,
                    out _)
                || provider is null)
            {
                throw new InvalidOperationException(Refusal(assemblyPath,
                    "it carries no portable debug information, so the compiler left no record of which assemblies "
                    + "it compiled against"));
            }

            var reader = provider.GetMetadataReader();
            BlobReader? blob = null;
            foreach (var handle in reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var information = reader.GetCustomDebugInformation(handle);
                if (reader.GetGuid(information.Kind) == CompilationMetadataReferencesKind)
                {
                    blob = reader.GetBlobReader(information.Value);
                    break;
                }
            }

            if (blob is null)
            {
                throw new InvalidOperationException(Refusal(assemblyPath,
                    "its debug information carries no record of the compilation's metadata references — csc writes "
                    + "one into every portable PDB it emits, so this PDB was not written by the compiler that "
                    + "produced the assembly, or was rewritten afterwards"));
            }

            return Parse(assemblyPath, blob.Value);
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
    /// The MVID of the managed module at <paramref name="file"/>, or <c>null</c> when the file is
    /// missing, unreadable, not a PE image, or a PE image with no managed metadata — every case in
    /// which the compiler could not have opened it as a reference either.
    /// </summary>
    public static Guid? TryReadMvid(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                return null;
            }

            var reader = pe.GetMetadataReader();
            return reader.GetGuid(reader.GetModuleDefinition().Mvid);
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException
                                       or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes Roslyn's reference record. Per reference, in order: file name (UTF-8, NUL-terminated),
    /// extern aliases (UTF-8, NUL-terminated, comma-separated, usually empty), one flags byte (bit 0:
    /// assembly rather than module; bit 1: embed interop types), the COFF timestamp (int32), the PE
    /// image size (int32), and the MVID (16 bytes).
    /// </summary>
    private static List<CompiledMetadataReference> Parse(string assemblyPath, BlobReader blob)
    {
        var references = new List<CompiledMetadataReference>();
        try
        {
            while (blob.RemainingBytes > 0)
            {
                var fileName = ReadNullTerminatedUtf8(ref blob);
                ReadNullTerminatedUtf8(ref blob); // extern aliases: irrelevant to which assembly it was
                var flags = blob.ReadByte();
                blob.ReadInt32(); // COFF timestamp
                blob.ReadInt32(); // SizeOfImage
                var mvid = blob.ReadGuid();

                if (fileName.Length == 0)
                {
                    throw new InvalidOperationException(Refusal(assemblyPath,
                        "the compiler recorded a metadata reference with no file name, so the gate cannot say which "
                        + "assembly it was"));
                }

                references.Add(new CompiledMetadataReference(
                    fileName,
                    mvid,
                    IsAssembly: (flags & 0b01) != 0,
                    EmbedInteropTypes: (flags & 0b10) != 0));
            }
        }
        catch (BadImageFormatException ex)
        {
            // BlobReader throws this when a read runs past the end of the blob: a truncated or
            // hand-written record, which is not one to trust half of.
            throw new InvalidOperationException(Refusal(assemblyPath,
                $"its record of the compilation's metadata references is malformed ({ex.Message})"), ex);
        }

        if (references.Count == 0)
        {
            throw new InvalidOperationException(Refusal(assemblyPath,
                "its record of the compilation's metadata references is empty, and no C# compilation has zero "
                + "references — even an empty class needs System.Runtime"));
        }

        return references;
    }

    private static string ReadNullTerminatedUtf8(ref BlobReader blob)
    {
        var start = blob.Offset;
        var length = 0;
        while (blob.ReadByte() != 0)
        {
            length++;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        blob.Offset = start;
        var bytes = blob.ReadBytes(length);
        blob.ReadByte(); // the terminator
        return Encoding.UTF8.GetString(bytes);
    }

    private static string Refusal(string assemblyPath, string because) =>
        $"Brick project refused: the gate could not read what {Path.GetFileName(assemblyPath)} was compiled against — "
        + because + ". Certification re-compiles the brick source for the analyzer fence and for every mutant, and "
        + "it must do so against the assemblies the compiler actually used, so a build that leaves no record of "
        + "them cannot be certified. The gate already forces <DebugType>portable</DebugType> as a global MSBuild "
        + "property, which the project cannot override, so reaching this means something in the build actively "
        + "removed or replaced the record. Fix: remove the MSBuild target in the brick project or in the "
        + "Directory.Build.props / Directory.Build.targets beside it that deletes or rewrites the .pdb, or that "
        + "invokes the compiler itself instead of letting CoreCompile run, and certify again.";
}
