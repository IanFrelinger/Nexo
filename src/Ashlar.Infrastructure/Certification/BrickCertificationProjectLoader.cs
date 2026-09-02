using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Xml.Linq;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Loads brick projects and witness specs from disk into certification requests.</summary>
public static class BrickCertificationProjectLoader
{
    /// <summary>Load asynchronously.</summary>
    public static async Task<CertificationRequest> LoadAsync(
        string brickProjectDirectory,
        string witnessSpecPath,
        CancellationToken cancellationToken = default)
    {
        var projectDir = Path.GetFullPath(brickProjectDirectory);
        var csproj = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault()
            ?? throw new FileNotFoundException($"No .csproj in {projectDir}");
        var sourceFile = ResolveSingleBrickSource(projectDir, csproj);

        var sourceCode = await File.ReadAllTextAsync(sourceFile, cancellationToken).ConfigureAwait(false);
        var witnessJson = await File.ReadAllTextAsync(witnessSpecPath, cancellationToken).ConfigureAwait(false);
        var witnessDto = JsonSerializer.Deserialize<WitnessSpecDto>(witnessJson, JsonOptions)
            ?? throw new InvalidOperationException("Witness spec is empty");

        var buildDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(buildDir);

        var build = await RunDotnetBuildAsync(csproj, buildDir, cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
            throw new InvalidOperationException($"dotnet build failed: {build.Output}");

        var dllPath = Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileName(f).Contains(Path.GetFileNameWithoutExtension(csproj), StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Built brick assembly not found");

        var assembly = Assembly.LoadFrom(dllPath);
        var brickType = assembly.GetTypes().FirstOrDefault(t => typeof(DomainBrick).IsAssignableFrom(t) && !t.IsAbstract)
            ?? throw new InvalidOperationException("No DomainBrick type in assembly");

        var brick = (DomainBrick)Activator.CreateInstance(brickType)!;
        var references = CollectReferences(buildDir, dllPath);

        var witness = new WitnessSpec(
            witnessDto.BrickId ?? brick.Id,
            witnessDto.Cases.Select(c => new WitnessCase(
                NormalizeDictionary(c.Input),
                NormalizeDictionary(c.ExpectedOutput))).ToList());

        return new CertificationRequest
        {
            Brick = brick,
            Witness = witness,
            SourceCode = sourceCode,
            ProjectPath = csproj,
            CompilationReferences = references,
            BrickTypeName = brickType.FullName
        };
    }

    /// <summary>
    /// Every C# file the compiler will compile into this brick, in a stable order — the whole
    /// brick, not a sample of it. The set is the SDK's own default <c>Compile</c> glob:
    /// <c>**/*.cs</c> under the project directory, minus <c>obj/</c> and <c>bin/</c>, which are the
    /// only two the SDK also excludes.
    /// </summary>
    /// <remarks>
    /// <para>The set must equal what the compiler compiles, because everything downstream treats
    /// it as THE brick: it is the text the content hash covers, the text the analyzer fence judges,
    /// and the text the mutation leg mutates. A file the compiler compiles but this method does not
    /// return is code that ships inside the assembly while being invisible to all three — a
    /// certification bypass, not a tidy-up.</para>
    ///
    /// <para>That is exactly what the first version of this method did. It excluded
    /// <c>*.g.cs</c>, <c>*.Designer.cs</c> and <c>*.AssemblyAttributes.cs</c> by NAME, on the
    /// reasoning that those names mean "generated". The SDK does not agree: it excludes only
    /// <c>obj/</c> and <c>bin/</c>, and compiles every one of those names when it finds them in the
    /// project tree. So <c>Helper.g.cs</c> beside the brick was compiled into the assembly, hashed
    /// by nothing, analyzed by nothing, mutated by nothing — and it also slipped the multi-file
    /// refusal, so two bricks with different behaviour certified Trusted under the SAME
    /// contentHash. A name is not evidence about what the compiler does; the compiler's own rule
    /// is. Real generated output lives under <c>obj/</c>, where the directory rule already
    /// excludes it, which is why nothing is lost by dropping the name list.</para>
    /// </remarks>
    public static IReadOnlyList<string> FindBrickSourceFiles(string brickProjectDirectory)
    {
        var projectDir = Path.GetFullPath(brickProjectDirectory);
        return Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsUnderBuildOutput(projectDir, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// True for the two directories the SDK's default compile glob excludes, and only those.
    /// Anything else under the project is compiled, so anything else is brick text.
    /// </summary>
    private static bool IsUnderBuildOutput(string projectDir, string file)
    {
        var relative = Path.GetRelativePath(projectDir, file);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Refuses any project whose <c>Compile</c> items make the default glob a WRONG model of what
    /// the compiler compiles — i.e. any project where <see cref="FindBrickSourceFiles"/> could
    /// miss a compiled file.
    /// </summary>
    /// <remarks>
    /// <para><see cref="FindBrickSourceFiles"/> reproduces the SDK default: <c>**/*.cs</c> under the
    /// project, minus <c>obj/</c> and <c>bin/</c>. That is only the truth while the project leaves
    /// the default items switched ON and adds no compile item from outside its own directory. Both
    /// of those are visible in the csproj, and both are refused here rather than silently assumed
    /// away — a compiled file the gate cannot hash must be a REFUSAL, never an omission, because an
    /// omission is a signed certificate asserting more than was checked.</para>
    ///
    /// <para>The reverse direction needs no refusal: a <c>Compile Remove</c> can only make the set
    /// a SUPERSET of what is compiled, and hashing more text than ships is fail-closed (at worst it
    /// costs a false multi-file refusal, which an author can see and fix).</para>
    /// </remarks>
    internal static void AssertCompileSetIsHashable(string csprojPath, string projectDir)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(csprojPath);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new InvalidOperationException(
                $"Cannot determine what {Path.GetFileName(csprojPath)} compiles: the project file is not valid XML "
                + $"({ex.Message}). The certification hash covers the compiled source set, so a project whose "
                + "compile items cannot be read is refused rather than hashed on a guess. Fix: repair the project "
                + "file.");
        }

        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        foreach (var property in new[] { "EnableDefaultItems", "EnableDefaultCompileItems" })
        {
            foreach (var element in doc.Descendants(ns + property))
            {
                if (!string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Brick project refused: {Path.GetFileName(csprojPath)} sets <{property}>false</{property}>, so "
                    + "the compiled source set is whatever its explicit Compile items name and the gate cannot know "
                    + "it from the directory. Certification hashes, analyzes and mutates the compiled text, and a "
                    + "file compiled into the brick but outside that hash is invisible to every leg of the gate. "
                    + $"Fix: remove <{property}>false</{property}> and let the SDK's default glob define the brick, "
                    + "or split the brick into a project that uses the default items. Refusing rather than signing "
                    + "over a source set that may be incomplete.");
            }
        }

        foreach (var compile in doc.Descendants(ns + "Compile"))
        {
            foreach (var attributeName in new[] { "Include", "Update" })
            {
                var value = compile.Attribute(attributeName)?.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                foreach (var piece in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = piece.Trim();
                    if (pattern.Length == 0 || !ReachesOutsideProject(projectDir, pattern))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Brick project refused: {Path.GetFileName(csprojPath)} has <Compile {attributeName}=\"{pattern}\" />, "
                        + "which pulls source from outside the brick directory (or through an MSBuild property this "
                        + "gate cannot evaluate). That file is compiled into the brick but sits outside the content "
                        + "hash, the analyzer fence and the mutation leg — the certificate would assert more than "
                        + "was checked. Fix: move the file into the brick directory, or move it into a package that "
                        + "is certified in its own right. Refusing rather than hashing a partial source set.");
                }
            }
        }
    }

    /// <summary>
    /// True when a Compile pattern can name a file outside the project directory: an absolute
    /// path, a pattern that walks up out of the directory, or one carrying an unexpanded MSBuild
    /// property (which could expand to anywhere, so it is treated as if it does).
    /// </summary>
    private static bool ReachesOutsideProject(string projectDir, string pattern)
    {
        if (pattern.Contains("$(", StringComparison.Ordinal))
        {
            return true;
        }

        if (Path.IsPathRooted(pattern))
        {
            return !IsInside(projectDir, pattern);
        }

        // Resolve against the project directory with the wildcards left in place: '*' and '?' are
        // not directory separators, so they cannot change which directory a segment lands in.
        // MSBuild treats BOTH slashes as separators on every platform, so both are normalised here
        // — on Linux, leaving a backslash alone would turn '..\Shared\Helper.cs' into a single
        // harmless-looking file name inside the project, and the escape would pass unnoticed.
        var normalized = pattern
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        try
        {
            var combined = Path.GetFullPath(Path.Combine(projectDir, normalized));
            return !IsInside(projectDir, combined);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A pattern this gate cannot resolve is a pattern whose compiled set it cannot model.
            // Fail closed: refuse, rather than assume it stayed inside.
            return true;
        }
    }

    private static bool IsInside(string projectDir, string candidate)
    {
        var root = projectDir.EndsWith(Path.DirectorySeparatorChar)
            ? projectDir
            : projectDir + Path.DirectorySeparatorChar;
        // Case-folding follows the filesystem. Folding on Linux would let a differently-cased
        // sibling directory read as "inside", which is the wrong way for a containment check to
        // be wrong; NOT folding on Windows would refuse a project whose path differs only in case.
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return Path.GetFullPath(candidate).StartsWith(root, comparison);
    }

    /// <summary>
    /// The one source file a certification request may be built from, or a refusal.
    /// </summary>
    /// <remarks>
    /// <para>This used to be <c>GetFiles("*.cs").FirstOrDefault(...)</c> — one file out of however
    /// many the brick had, chosen by whatever order the filesystem handed back. Everything
    /// downstream then treated that one file AS the brick: it is the text the analyzer fence
    /// judges, the text the mutation leg mutates, and the text the signed content hash covers. So
    /// a brick spanning several files got a certificate asserting far more than was ever checked —
    /// its other files were not analyzed, not mutated, and not bound by the hash, and renaming a
    /// helper could flip REJECT into ADMIT by changing which file came back first.
    /// </para>
    ///
    /// <para>Certifying the whole set is not a rename away: the legs compile the candidate as ONE
    /// compilation unit, and concatenating files is not a semantics-preserving merge (using
    /// directives, file-scoped namespaces and duplicate top-level members all break). So a
    /// multi-file brick is REFUSED, loudly and by name, until the gate can genuinely certify a
    /// set. Silently picking one file and signing as if it were the brick is the failure mode this
    /// refusal exists to end.</para>
    /// </remarks>
    private static string ResolveSingleBrickSource(string projectDir, string csprojPath)
    {
        // Before counting files, establish that counting files is a valid way to know what the
        // compiler compiles. If it is not, that is a refusal, not a set to hash.
        AssertCompileSetIsHashable(csprojPath, projectDir);

        var sources = FindBrickSourceFiles(projectDir);
        if (sources.Count == 0)
        {
            throw new FileNotFoundException(
                $"No .cs source in {projectDir}. Certification hashes, analyzes and mutates the "
                + "brick's own text, and there is none here to hash. Fix: put the brick source in "
                + "this directory, or point the loader at the directory that holds it.");
        }

        if (sources.Count > 1)
        {
            var listed = string.Join(", ", sources.Select(f => Path.GetRelativePath(projectDir, f)));
            throw new InvalidOperationException(
                $"Multi-file brick refused: {projectDir} holds {sources.Count} source files ({listed}). "
                + "A certification record binds ONE content hash over ONE source text, and the analyzer "
                + "and mutation legs judge that text alone — so certifying against one of these would "
                + "sign a record asserting more than was checked, and leave every other file outside the "
                + "hash. Fix: reduce the brick to a single .cs file (a brick is one unit of behaviour), "
                + "or move the helpers into a package that is certified in its own right. Refusing "
                + "rather than certifying an arbitrarily chosen file.");
        }

        return sources[0];
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetBuildAsync(
        string csproj,
        string outputDir,
        CancellationToken cancellationToken)
    {
        var configFile = Environment.GetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG");
        var configArg = string.IsNullOrWhiteSpace(configFile)
            ? string.Empty
            : $" --configfile \"{configFile}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments =
                $"build \"{csproj}\" -c Release -o \"{outputDir}\" -v q{configArg} " +
                "-p:TreatWarningsAsErrors=false -p:NuGetAudit=false",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return (process.ExitCode, stdout + stderr);
    }

    /// <summary>
    /// The managed assemblies in the brick's build output, as Roslyn metadata references.
    /// </summary>
    /// <remarks>
    /// <para>Every <c>*.dll</c> under the output used to go in unfiltered. Most brick projects got
    /// away with it; <c>Ashlar.Authoring</c> — one of the two packages a brick is ALLOWED to
    /// reference, and the one the CLI scaffold emits — does not, because its transitive graph
    /// copies LLamaSharp's unmanaged binaries into <c>runtimes/&lt;rid&gt;/native/</c>. Handing one of
    /// those to <c>MetadataReference.CreateFromFile</c> produces CS0009 ("PE image doesn't contain
    /// managed metadata") at compile time, and the analyzer fence then reported "candidate does not
    /// compile, so analyzer silence would be meaningless" — blaming brick source that was perfectly
    /// fine for a file the loader itself supplied.</para>
    ///
    /// <para>Two filters, cheapest first: skip anything under a <c>runtimes/*/native/</c> segment
    /// (where native payloads live by convention), then PROBE the rest — a PE file without a
    /// metadata root is unmanaged whatever its path says, and the probe is what actually decides.
    /// A file that cannot be opened or read as PE is skipped for the same reason: a reference the
    /// compiler could not have used is not one to pass it.</para>
    /// </remarks>
    internal static List<string> CollectReferences(string buildDir, string primaryDll)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { primaryDll };
        foreach (var dll in Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories))
        {
            if (IsUnderNativeRuntimesFolder(buildDir, dll) || !HasManagedMetadata(dll))
                continue;
            refs.Add(dll);
        }
        return refs.ToList();
    }

    /// <summary>True for <c>runtimes/&lt;rid&gt;/native/...</c>, the convention location for the
    /// unmanaged payloads NuGet copies alongside a managed graph.</summary>
    private static bool IsUnderNativeRuntimesFolder(string buildDir, string file)
    {
        var segments = Path.GetRelativePath(buildDir, file)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i + 2 < segments.Length; i++)
        {
            if (string.Equals(segments[i], "runtimes", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[i + 2], "native", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the file is a PE image carrying a managed metadata root — the exact condition
    /// <c>MetadataReference.CreateFromFile</c> needs, asked directly instead of inferred from a
    /// path or an extension.
    /// </summary>
    private static bool HasManagedMetadata(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var reader = new PEReader(stream);
            return reader.HasMetadata;
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed class WitnessSpecDto
    {
        /// <summary>Brick id.</summary>
        public string? BrickId { get; set; }
        /// <summary>Cases.</summary>
        public List<WitnessCaseDto> Cases { get; set; } = [];
    }

    private sealed class WitnessCaseDto
    {
        /// <summary>Input.</summary>
        public Dictionary<string, object> Input { get; set; } = new();
        /// <summary>Expected output.</summary>
        public Dictionary<string, object> ExpectedOutput { get; set; } = new();
    }

    private static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> values)
    {
        var normalized = new Dictionary<string, object>(values.Count);
        foreach (var (key, value) in values)
            normalized[key] = NormalizeValue(value);
        return normalized;
    }

    private static object NormalizeValue(object value) => value switch
    {
        JsonElement element => FromJsonElement(element),
        _ => value
    };

    private static object FromJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number when element.TryGetInt32(out var int32) => int32,
        JsonValueKind.Number when element.TryGetInt64(out var number) => number,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => element.GetRawText()
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
