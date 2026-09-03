using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The compile options a built assembly was ACTUALLY compiled with, read out of the compiler's own
/// record of the compilation rather than out of MSBuild's state.
/// </summary>
/// <remarks>
/// <para>csc writes a compilation-options block into every portable PDB it emits: the effective
/// language version, the preprocessor symbols it was given (<c>define</c>), and — when set —
/// <c>checked</c>, <c>nullable</c> and <c>unsafe</c>. That is the answer to "which program is this
/// source", from the process that turned the source into the program. MSBuild could be asked for
/// <c>DefineConstants</c> instead, but the SDK appends the implicit framework symbols inside a
/// target, a post-build property read is mutable state belonging to the project under judgement
/// (see <see cref="CompiledSourceDocuments"/> for why no such read is an authority), and every
/// hard-coded list of "the symbols the SDK defines" is wrong the next SDK over. The record needs no
/// list: it is what the compiler used.</para>
///
/// <para>Every failure here is a REFUSAL, in the same shape as <see cref="CompiledMetadataReferences"/>:
/// no debug information, no options block, a malformed or unreadable value. The in-process legs
/// cannot compile the program the build compiled without these, and compiling with defaults instead
/// is precisely the bypass this type closes.</para>
/// </remarks>
internal static class CompiledCompilationOptions
{
    // Roslyn's custom-debug-information kind for the compilation options
    // (PortableCustomDebugInfoKinds.CompilationOptions; System.Reflection.Metadata does not expose it).
    private static readonly Guid CompilationOptionsKind = new("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

    // The keys csc writes (Roslyn's CompilationOptionNames), as it spells them.
    private const string LanguageVersionKey = "language-version";
    private const string DefineKey = "define";
    private const string CheckedKey = "checked";
    private const string NullableKey = "nullable";
    private const string UnsafeKey = "unsafe";

    /// <summary>
    /// The options recorded in <paramref name="assemblyPath"/>'s portable PDB, with
    /// <see cref="BrickCompileOptions.GlobalUsings"/> left empty for the caller to fill from the
    /// compiled <c>global using</c> source — or a refusal naming what could not be read.
    /// </summary>
    public static BrickCompileOptions Read(string assemblyPath)
    {
        var entries = ReadEntries(assemblyPath);

        if (!entries.TryGetValue(LanguageVersionKey, out var languageVersion) || string.IsNullOrWhiteSpace(languageVersion))
        {
            throw new InvalidOperationException(Refusal(assemblyPath,
                $"its record of the compilation options carries no '{LanguageVersionKey}', which csc always writes"));
        }

        var symbols = entries.TryGetValue(DefineKey, out var define)
            ? define.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray()
            : Array.Empty<string>();

        return new BrickCompileOptions
        {
            LanguageVersion = languageVersion.Trim(),
            PreprocessorSymbols = symbols,
            CheckOverflow = ReadFlag(assemblyPath, entries, CheckedKey),
            Nullable = entries.TryGetValue(NullableKey, out var nullable) && !string.IsNullOrWhiteSpace(nullable)
                ? nullable.Trim()
                : "Disable",
            AllowUnsafe = ReadFlag(assemblyPath, entries, UnsafeKey),
        };
    }

    /// <summary>
    /// Every key/value pair in the compilation-options block, exactly as recorded. Exposed for the
    /// tests that pin the reader against a real emit.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> ReadEntries(string assemblyPath)
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
                    "it carries no portable debug information, so the compiler left no record of the options it "
                    + "compiled with"));
            }

            var reader = provider.GetMetadataReader();
            BlobReader? blob = null;
            foreach (var handle in reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var information = reader.GetCustomDebugInformation(handle);
                if (reader.GetGuid(information.Kind) == CompilationOptionsKind)
                {
                    blob = reader.GetBlobReader(information.Value);
                    break;
                }
            }

            if (blob is null)
            {
                throw new InvalidOperationException(Refusal(assemblyPath,
                    "its debug information carries no record of the compilation options — csc writes one into every "
                    + "portable PDB it emits, so this PDB was not written by the compiler that produced the assembly, "
                    + "or was rewritten afterwards"));
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

    private static bool ReadFlag(string assemblyPath, IReadOnlyDictionary<string, string> entries, string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            return false; // csc writes the flag only when it is on
        }

        if (bool.TryParse(value.Trim(), out var flag))
        {
            return flag;
        }

        // "cannot read" and "off" are not the same answer when the answer decides which program is judged.
        throw new InvalidOperationException(Refusal(assemblyPath,
            $"its record of the compilation options gives '{key}' the value '{value}', which is not a boolean"));
    }

    private static Dictionary<string, string> Parse(string assemblyPath, BlobReader blob)
    {
        // The block is a sequence of UTF-8 pairs: key NUL value NUL.
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            while (blob.RemainingBytes > 0)
            {
                var key = ReadNullTerminatedUtf8(ref blob);
                if (blob.RemainingBytes == 0)
                {
                    throw new BadImageFormatException($"key '{key}' has no value");
                }

                var value = ReadNullTerminatedUtf8(ref blob);
                if (key.Length == 0)
                {
                    throw new BadImageFormatException("an option has an empty key");
                }

                entries[key] = value;
            }
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException(Refusal(assemblyPath,
                $"its record of the compilation options is malformed ({ex.Message})"), ex);
        }

        return entries;
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
        $"Brick project refused: the gate could not read the compile options {Path.GetFileName(assemblyPath)} was "
        + "compiled with — " + because + ". The analyzer fence and the mutation leg re-compile the brick source "
        + "inside the certifying process, and they must compile the PROGRAM the build compiled — the same "
        + "preprocessor symbols, language version, overflow checking, nullable context and unsafe setting — or "
        + "they judge a different program from the one that ships under the certificate's content hash. The gate "
        + "already forces <DebugType>portable</DebugType> as a global MSBuild property, which the project cannot "
        + "override, so reaching this means something in the build removed or replaced the compiler's record. Fix: "
        + "remove the MSBuild target in the brick project or in the Directory.Build.props / Directory.Build.targets "
        + "beside it that deletes or rewrites the .pdb, or that invokes the compiler itself instead of letting "
        + "CoreCompile run, and certify again.";
}
