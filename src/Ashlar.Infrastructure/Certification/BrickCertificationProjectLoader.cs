using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.Json;
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

        // What was compiled, asked AFTER the targets that build the compile list have run — and
        // asked BEFORE Assembly.LoadFrom below, which executes the candidate's module
        // initializers. A payload smuggled in by a target is refused here rather than running
        // first and being refused afterwards.
        AssertCompiledSetIsExactlyTheCertifiedSet(csproj, projectDir, [sourceFile]);

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
    /// Every file the compiler will compile into this brick, in a stable order — the whole brick,
    /// not a sample of it. The set is MSBuild's own evaluated <c>Compile</c> item list for the
    /// project, not a reconstruction of it.
    /// </summary>
    /// <remarks>
    /// <para>The set must equal what the compiler compiles, because everything downstream treats
    /// it as THE brick: it is the text the content hash covers, the text the analyzer fence judges,
    /// and the text the mutation leg mutates. A file the compiler compiles but this method does not
    /// return is code that ships inside the assembly while being invisible to all three — a
    /// certification bypass, not a tidy-up.</para>
    ///
    /// <para>Four rounds of this method tried to MODEL the compiler and each one left a hole one
    /// step to the side. It excluded <c>*.g.cs</c> and friends by NAME, so <c>Helper.g.cs</c> beside
    /// the brick was compiled, hashed by nothing, and slipped the multi-file refusal. Dropping the
    /// name list left <c>Directory.EnumerateFiles(projectDir, "*.cs")</c>, which cannot see
    /// <c>&lt;Compile Include="Payload.cstxt" /&gt;</c> — csc compiles whatever it is handed, and the
    /// extension is decoration. Excluding a directory named <c>obj</c> or <c>bin</c> at ANY depth
    /// diverged from the SDK, which excludes only the project's OWN two, so <c>Sub/obj/Payload.cs</c>
    /// was compiled and unhashed. And reading the <c>.csproj</c> as one XML document never saw a
    /// <c>Directory.Build.props</c> sitting beside it. The model is gone. MSBuild is asked instead,
    /// and it answers for the props chain, the imports, the removes and the non-<c>.cs</c> includes
    /// at once — see <see cref="EvaluatedBrickProject"/>.</para>
    ///
    /// <para>Everything this method cannot resolve is a refusal, never an omission: an evaluation
    /// that errors, a compile item that resolves outside the project directory, and a compile item
    /// with no file on disk at hash time (a build-generated source, say). An omission is a signed
    /// certificate asserting more than was checked, which is the failure this whole area exists to
    /// prevent.</para>
    /// </remarks>
    public static IReadOnlyList<string> FindBrickSourceFiles(string brickProjectDirectory)
    {
        var projectDir = Path.GetFullPath(brickProjectDirectory);
        var csproj = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault()
            ?? throw new FileNotFoundException($"No .csproj in {projectDir}");
        return CertifiedSourceSet(EvaluatedBrickProject.Evaluate(csproj), projectDir);
    }

    /// <summary>
    /// Refuses any project whose evaluated <c>Compile</c> set cannot be hashed exactly as the
    /// compiler will see it.
    /// </summary>
    /// <remarks>
    /// <para>This used to be an XML scan of the <c>.csproj</c> looking for the shapes that would
    /// make the <c>*.cs</c> glob a wrong model — the default items switched off, a Compile pattern
    /// walking out of the directory. Both of those checks were reading ONE file while MSBuild
    /// evaluates a chain: a <c>Directory.Build.props</c> beside the project could add whatever the
    /// csproj was refused for, and be admitted. The checks now run against the evaluated result, so
    /// wherever in the chain an item was declared makes no difference to the verdict.</para>
    ///
    /// <para>The comment that used to live here — "a <c>Compile Remove</c> can only make the set a
    /// SUPERSET of what is compiled, which is fail-closed" — was false, and it licensed the worst
    /// of the four bypasses. <c>&lt;Compile Remove="Brick.cs" /&gt;&lt;Compile Include="Real.cstxt"
    /// /&gt;</c> makes the hashed set DISJOINT from the compiled set: the certificate is then signed
    /// over a decoy the assembly does not contain. Nothing about the direction of a Remove is
    /// fail-closed, and nothing here reasons about Removes any more — the evaluated set already has
    /// them applied.</para>
    /// </remarks>
    internal static void AssertCompileSetIsHashable(string csprojPath, string projectDir)
        => CertifiedSourceSet(EvaluatedBrickProject.Evaluate(csprojPath), Path.GetFullPath(projectDir));

    /// <summary>
    /// The evaluated <c>Compile</c> set, checked to be hashable exactly as the compiler sees it,
    /// deduplicated and ordered stably — or a refusal naming the file that could not be covered.
    /// </summary>
    private static List<string> CertifiedSourceSet(EvaluatedBrickProject project, string projectDir)
    {
        var name = Path.GetFileName(project.ProjectPath);

        // Multi-targeting: the compiled set is per-TFM, and an outer evaluation answers for none of
        // them. (`dotnet build -o` refuses these anyway; refusing here says why.)
        var targetFrameworks = (project.Property("TargetFrameworks") ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
        if (targetFrameworks.Count > 1)
        {
            throw new InvalidOperationException(
                $"Brick project refused: {name} multi-targets ({string.Join(", ", targetFrameworks)}), so it has one "
                + "compiled source set per framework and a certificate binding ONE content hash cannot speak for all "
                + "of them. Fix: give the brick a single <TargetFramework>. Refusing rather than hashing one "
                + "framework's set and signing as if it covered every framework.");
        }

        // Belt and braces, kept from the previous design: with the default compile items switched
        // off the project is asserting a hand-maintained compile list. The evaluated set below is
        // still exact, so this is no longer NEEDED to know what is compiled — but a one-file brick
        // has no reason to carry that shape, and refusing it costs nothing legitimate.
        foreach (var property in new[] { "EnableDefaultItems", "EnableDefaultCompileItems" })
        {
            if (!string.Equals(project.Property(property), "false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Brick project refused: {name} evaluates <{property}>false</{property}>, so the compiled source set "
                + "is whatever its explicit Compile items name. Certification hashes, analyzes and mutates the "
                + "compiled text, and a hand-maintained compile list is a shape a single-file brick has no need of. "
                + $"Fix: remove <{property}>false</{property}> (from the project or from a Directory.Build.props "
                + "beside it) and let the SDK's default items define the brick.");
        }

        var set = new List<string>();
        var seen = new HashSet<string>(PathComparer);
        foreach (var item in project.Compile)
        {
            if (string.IsNullOrWhiteSpace(item.FullPath))
            {
                throw new InvalidOperationException(
                    $"Brick project refused: {name} compiles '{item.Identity}', which MSBuild resolved to no path on "
                    + "disk. The certification hash covers the compiled text, and text the gate cannot read is text "
                    + "no leg of the gate ever judged. Fix: make the compile item name a real file inside the brick "
                    + "directory.");
            }

            string full;
            try
            {
                full = Path.GetFullPath(item.FullPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException(
                    $"Brick project refused: {name} compiles '{item.Identity}', whose path the gate could not "
                    + $"resolve ({ex.Message}). Refusing rather than hashing a source set it cannot enumerate.", ex);
            }

            if (!IsInside(projectDir, full))
            {
                // Covers the walked-up relative path, the absolute path, the property that expanded
                // to somewhere else, and the <Compile Include="../x.cs" Link="x.cs" /> linked file —
                // all of which used to need their own pattern rule, and one of which (via
                // Directory.Build.props) had no rule at all.
                throw new InvalidOperationException(
                    $"Brick project refused: {name} compiles '{item.Identity}' ({full}), which is outside the brick "
                    + "directory. That file is compiled into the brick but sits outside the content hash, the "
                    + "analyzer fence and the mutation leg — the certificate would assert more than was checked. "
                    + "Fix: move the file into the brick directory, or move it into a package that is certified in "
                    + "its own right. Refusing rather than hashing a partial source set.");
            }

            if (!File.Exists(full))
            {
                throw new InvalidOperationException(
                    $"Brick project refused: {name} compiles '{item.Identity}' ({full}), and no such file exists to "
                    + "hash. A source the build generates on its way past is code the content hash, the analyzer "
                    + "fence and the mutation leg never see. Fix: commit the file, or drop the compile item. "
                    + "Refusing rather than certifying a source set with a hole in it.");
            }

            if (seen.Add(full))
            {
                set.Add(full);
            }
        }

        set.Sort(StringComparer.Ordinal);
        return set;
    }

    /// <summary>The configuration the loader builds in, and therefore the one it asks about.</summary>
    private const string BuildConfiguration = "Release";

    /// <summary>
    /// Refuses unless the set the COMPILER was handed is exactly the set that was hashed.
    /// </summary>
    /// <remarks>
    /// <para>Evaluation closes four bypasses and leaves a fifth open one step later. A
    /// <c>&lt;Target BeforeTargets="CoreCompile"&gt;&lt;ItemGroup&gt;&lt;Compile
    /// Include="Payload.cstxt" /&gt;</c> contributes nothing to the evaluated item list and
    /// everything to the compilation — reproduced on this repo, with the payload's type present in
    /// the built assembly and absent from every evaluation-time answer. A target lives in the same
    /// import chain a <c>Directory.Build.props</c> lives in, so it needs no csproj edit either. This
    /// is the same fact as the other four: an answer taken BEFORE the thing it describes is a model
    /// of the compiler, and the compiler is the only authority on what the compiler compiled.</para>
    ///
    /// <para>So the question is asked again once the targets have run, and the two answers must
    /// agree. Exactly one difference is tolerated, and it is named rather than guessed: a file
    /// under the project's OWN intermediate output directory that the SDK's own targets declared —
    /// the assembly-info boilerplate every SDK project compiles. Both halves are required. A file
    /// the author's chain declared is never tolerated wherever it sits, and an SDK-declared file
    /// outside <c>obj/</c> is not tolerated either.</para>
    ///
    /// <para>The reverse direction is checked too: a hashed file the compiler did NOT compile means
    /// the certificate is signed over a decoy, which is bypass #4's shape. Evaluation already
    /// catches it; catching it here as well costs one set lookup and closes the case where a target
    /// does the removing.</para>
    /// </remarks>
    internal static void AssertCompiledSetIsExactlyTheCertifiedSet(
        string csprojPath, string projectDir, IReadOnlyCollection<string> certified)
    {
        var project = EvaluatedBrickProject.EvaluateAfterCompile(csprojPath, BuildConfiguration);
        var name = Path.GetFileName(csprojPath);
        var hashed = new HashSet<string>(certified.Select(Path.GetFullPath), PathComparer);
        var intermediate = Path.GetFullPath(Path.Combine(
            projectDir, project.Property("BaseIntermediateOutputPath") is { Length: > 0 } p ? p : "obj"));

        var compiled = new HashSet<string>(PathComparer);
        foreach (var item in project.Compile)
        {
            if (string.IsNullOrWhiteSpace(item.FullPath))
            {
                throw new InvalidOperationException(
                    $"Brick project refused: after building {name}, MSBuild reported a compile item "
                    + $"('{item.Identity}') with no path on disk. The gate cannot confirm that what was compiled is "
                    + "what was hashed. Refusing rather than signing over a set it could not compare.");
            }

            var full = Path.GetFullPath(item.FullPath);
            compiled.Add(full);
            if (hashed.Contains(full))
            {
                continue;
            }

            if (IsInside(intermediate, full) && project.IsSdkDeclared(item))
            {
                continue; // The SDK's own assembly-info boilerplate, in the SDK's own directory.
            }

            throw new InvalidOperationException(
                $"Brick project refused: {name} compiled '{item.Identity}' ({full}), which is not in the set the "
                + $"certificate hashes. It was declared by {item.Meta("DefiningProjectFullPath") ?? "an unreported file"} "
                + "— a target or item that contributes to the compilation AFTER the project is evaluated, so it is "
                + "invisible to the content hash, the analyzer fence and the mutation leg while shipping inside the "
                + "signed assembly. Fix: declare the file as an ordinary Compile item in the brick directory, or "
                + "remove the target that adds it. Refusing rather than certifying an assembly the gate did not "
                + "read all of.");
        }

        foreach (var file in hashed)
        {
            if (compiled.Contains(file))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Brick project refused: the certificate would hash '{Path.GetRelativePath(projectDir, file)}', which "
                + $"{name} did NOT compile. A hash over text the assembly does not contain is a certificate signed "
                + "over a decoy — the analyzer fence and the mutation leg would judge one program while the shipped "
                + "one is another. Fix: remove whatever excludes this file from the compilation. Refusing rather "
                + "than signing a record about a file that is not in the brick.");
        }
    }

    /// <summary>
    /// Case-folding follows the filesystem, for the same reason <see cref="IsInside"/> does.
    /// </summary>
    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

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
        // The set is MSBuild's answer, checked to be hashable exactly as the compiler sees it. One
        // evaluation serves both: a set the gate cannot cover is a refusal, not a set to count.
        var sources = CertifiedSourceSet(EvaluatedBrickProject.Evaluate(csprojPath), projectDir);
        if (sources.Count == 0)
        {
            throw new FileNotFoundException(
                $"{Path.GetFileName(csprojPath)} compiles no source at all. Certification hashes, analyzes and "
                + "mutates the brick's own text, and there is none here to hash. Fix: put the brick source in "
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
