using System.Reflection;
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

        // Captured as BYTES, and decoded from those same bytes. The certificate's content hash is
        // taken over this text, so the comparison against the compiler's per-file checksum below
        // has to be against exactly these bytes — not against whatever is on disk after the build.
        // A target that rewrites the brick source on its way to CoreCompile leaves the two
        // identical if the file is re-read afterwards, which is the check quietly passing on the
        // one case it exists for.
        var sourceBytes = await File.ReadAllBytesAsync(sourceFile, cancellationToken).ConfigureAwait(false);
        var sourceCode = Decode(sourceBytes);
        var witnessJson = await File.ReadAllTextAsync(witnessSpecPath, cancellationToken).ConfigureAwait(false);
        var witnessDto = JsonSerializer.Deserialize<WitnessSpecDto>(witnessJson, JsonOptions)
            ?? throw new InvalidOperationException("Witness spec is empty");

        var buildDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(buildDir);

        var build = await Task.Run(
            () => EvaluatedBrickProject.Build(
                csproj, buildDir, BuildConfiguration,
                Environment.GetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG")),
            cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0 || build.Project is null)
            throw new InvalidOperationException($"dotnet build failed: {build.Output}");

        var dllPath = Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileName(f).Contains(Path.GetFileNameWithoutExtension(csproj), StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Built brick assembly not found");

        // What the COMPILER recorded compiling, read out of the built assembly — and read BEFORE
        // Assembly.LoadFrom below, which executes the candidate's module initializers inside this
        // process. A payload smuggled in by a target is refused here rather than running first and
        // being refused afterwards.
        AssertCompiledSetIsExactlyTheCertifiedSet(
            csproj, projectDir,
            new Dictionary<string, byte[]>(PathComparer) { [Path.GetFullPath(sourceFile)] = sourceBytes },
            dllPath, build.Project);

        // Likewise the assemblies the compiler compiled against — resolved and checked before the
        // candidate's code runs, for the same reason.
        var references = CollectReferences(build.Project, dllPath);

        var assembly = Assembly.LoadFrom(dllPath);
        var brickType = assembly.GetTypes().FirstOrDefault(t => typeof(DomainBrick).IsAssignableFrom(t) && !t.IsAbstract)
            ?? throw new InvalidOperationException("No DomainBrick type in assembly");

        var brick = (DomainBrick)Activator.CreateInstance(brickType)!;

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

            // NOTE ON EVERY "Fix:" BELOW. A brick is ONE source file — see ResolveSingleBrickSource
            // — so "add the missing file to the brick directory" is not a fix, it is a trip into the
            // multi-file refusal one step later. Each of these used to say exactly that. The fix an
            // author can actually carry out is to move the CODE into the brick's own source file, or
            // to stop compiling the other file at all, and that is what these now say.
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
                    + "Fix: move its code into the brick's own single source file (a certificate binds one content "
                    + "hash over one source text, so adding a second file in the brick directory is refused too), or "
                    + "drop the Compile item and take the dependency as a package that is certified in its own "
                    + "right. Refusing rather than hashing a partial source set.");
            }

            if (!File.Exists(full))
            {
                throw new InvalidOperationException(
                    $"Brick project refused: {name} compiles '{item.Identity}' ({full}), and no such file exists to "
                    + "hash. A source the build generates on its way past is code the content hash, the analyzer "
                    + "fence and the mutation leg never see. Fix: drop the Compile item, or — if the code belongs "
                    + "in the brick — write it into the brick's own single source file, which is the one text a "
                    + "certificate can bind. Refusing rather than certifying a source set with a hole in it.");
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
    /// Refuses unless the source the COMPILER recorded compiling is exactly the source that was
    /// hashed, byte for byte.
    /// </summary>
    /// <remarks>
    /// <para>Evaluation closes four bypasses and leaves the same hole one step later, twice over.
    /// A <c>&lt;Target BeforeTargets="CoreCompile"&gt;&lt;ItemGroup&gt;&lt;Compile
    /// Include="Payload.cstxt" /&gt;</c> contributes nothing to the evaluated item list and
    /// everything to the compilation. Give that target a <c>Condition</c> on
    /// <c>$(OutputPath)</c> and it also contributes nothing to a SECOND MSBuild query, because the
    /// gate builds into a temp directory and a separate query does not — so "ask again after the
    /// build" splits in exactly the same way. Ask in the build's own invocation instead and a
    /// second target, <c>AfterTargets="CoreCompile"</c>, removes the payload from
    /// <c>@(Compile)</c> once the compile has happened and the answer is clean again. All three
    /// were reproduced live on this repo, with the payload's type present in the built assembly
    /// every time.</para>
    ///
    /// <para>The lesson is not "ask MSBuild more carefully". It is that <c>@(Compile)</c> is
    /// mutable state belonging to the project under judgement, and no reading of it is an
    /// authority on what was compiled. The authority is the compiler's own record: the source
    /// document table csc writes into the PDB while emitting, from the syntax trees it actually
    /// parsed. That is what this method compares against — see
    /// <see cref="CompiledSourceDocuments"/>.</para>
    ///
    /// <para>MSBuild's post-build answer is still used, for one thing only: to NARROW the single
    /// tolerance the gate grants. A compiled file outside the hashed set is admitted only when it
    /// is under the project's OWN intermediate output directory AND MSBuild reports it as declared
    /// by a file that ships with the SDK — the assembly-info boilerplate every SDK project
    /// compiles. Used this way its failure mode is a refusal rather than an admission, which is the
    /// direction that is safe to be wrong in; a payload dropped into <c>obj/</c> by an author's
    /// target is declared by the author's own file and is refused, and a payload whose target
    /// scrubbed it from <c>@(Compile)</c> has no SDK declaration to point at and is refused too.</para>
    ///
    /// <para>Two further checks, both cheap and both closing a hole a path comparison cannot see.
    /// A hashed file the compiler did NOT compile is bypass #4's shape — a certificate signed over
    /// a decoy — and is refused. And each hashed file's bytes on disk must equal the checksum the
    /// compiler recorded for it, so a candidate rewritten between the hash and the build binds the
    /// text that was actually compiled or nothing at all.</para>
    /// </remarks>
    /// <param name="csprojPath">The brick project.</param>
    /// <param name="projectDir">The brick directory, for rendering paths in refusals.</param>
    /// <param name="certified">Each hashed file, mapped to the EXACT bytes the certificate's
    /// content hash was taken over — not the file, which the build may since have rewritten.</param>
    /// <param name="assemblyPath">The built brick assembly.</param>
    /// <param name="built">The project as MSBuild reported it at the end of that build.</param>
    internal static void AssertCompiledSetIsExactlyTheCertifiedSet(
        string csprojPath,
        string projectDir,
        IReadOnlyDictionary<string, byte[]> certified,
        string assemblyPath,
        EvaluatedBrickProject built)
    {
        var name = Path.GetFileName(csprojPath);
        var hashed = new Dictionary<string, byte[]>(PathComparer);
        foreach (var (file, bytes) in certified)
        {
            hashed[Path.GetFullPath(file)] = bytes;
        }
        var documents = CompiledSourceDocuments.Read(assemblyPath);
        var sdkBoilerplate = SdkGeneratedFilesUnderIntermediateOutput(built);

        var compiled = new HashSet<string>(PathComparer);
        foreach (var document in documents)
        {
            string full;
            try
            {
                // Rooted, or the gate does not know which file this is. Path.GetFullPath would
                // resolve a relative document against the CURRENT PROCESS's working directory —
                // some host's, not the brick's — and quietly produce a confident wrong answer. The
                // forced empty PathMap means csc records absolute paths, so this is unreachable
                // through the front door; it is here because "resolve it against whatever directory
                // we happen to be in" is the shape of every bug in this file's history.
                if (!Path.IsPathRooted(document.Path))
                {
                    throw new ArgumentException("the compiler recorded it as a relative path");
                }

                full = Path.GetFullPath(document.Path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException(
                    $"Brick project refused: {name} was compiled from '{document.Path}', which is not a path the "
                    + $"gate can resolve ({ex.Message}). The certificate covers named files, so a compiled source "
                    + "the gate cannot locate is one it cannot have checked. Fix: remove any <PathMap> or "
                    + "<DeterministicSourcePaths> setting that rewrites source paths, and any target that invokes "
                    + "the compiler itself, then certify again. Refusing rather than certifying an assembly whose "
                    + "source set it could not read.", ex);
            }

            compiled.Add(full);

            if (hashed.TryGetValue(full, out var certifiedBytes))
            {
                if (!CompiledSourceDocuments.ContentMatches(document, certifiedBytes))
                {
                    throw new InvalidOperationException(
                        $"Brick project refused: the text of '{Path.GetRelativePath(projectDir, full)}' that the "
                        + "certificate would hash is not the text that was compiled — the compiler's own checksum "
                        + "for that file does not match the bytes the gate read. Something rewrote the brick source "
                        + "in between, so the record would bind a content hash over a program the assembly does not "
                        + "contain, and the analyzer fence and the mutation leg would judge that program rather than "
                        + "the shipped one. Fix: remove the MSBuild target in the project or in the "
                        + $"Directory.Build.props / Directory.Build.targets beside {name} that writes to the brick "
                        + "source during the build, then certify again. Refusing rather than signing a hash of one "
                        + "text over a build of another.");
                }

                continue;
            }

            if (sdkBoilerplate.Contains(full))
            {
                continue; // The SDK's own assembly-info boilerplate, in the SDK's own directory.
            }

            throw new InvalidOperationException(
                $"Brick project refused: {name} was compiled from '{Relative(projectDir, full)}', which is not in the "
                + "set the certificate hashes. The compiler recorded it as part of this assembly, so it ships inside "
                + "the signed brick while being invisible to the content hash, the analyzer fence and the mutation "
                + "leg. It reached the compilation after the project was evaluated — an MSBuild target that adds a "
                + "Compile item, or one that replaces the compile step. Fix: remove the target that adds it, from "
                + $"the project or from the Directory.Build.props / Directory.Build.targets beside {name}; if the "
                + "code belongs in the brick, move it into the brick's own single source file, which is the one "
                + "text a certificate can bind. Refusing rather than certifying an assembly the gate did not read "
                + "all of.");
        }

        foreach (var file in hashed.Keys)
        {
            if (compiled.Contains(file))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Brick project refused: the certificate would hash '{Path.GetRelativePath(projectDir, file)}', which "
                + $"{name} did NOT compile — the compiler's record of this assembly does not mention it. A hash over "
                + "text the assembly does not contain is a certificate signed over a decoy: the analyzer fence and "
                + "the mutation leg would judge one program while the shipped one is another. Fix: remove whatever "
                + "excludes this file from the compilation (a <Compile Remove> in the project or in a "
                + "Directory.Build.props beside it). Refusing rather than signing a record about a file that is not "
                + "in the brick.");
        }
    }

    /// <summary>
    /// The files under the project's own intermediate output directory that the SDK's own files
    /// declared — the only compiled source the gate tolerates outside the certified set.
    /// </summary>
    /// <remarks>
    /// <para>Both halves are load-bearing and neither is a name match. The DIRECTORY comes from
    /// MSBuild's own <c>IntermediateOutputPath</c> rather than from the string "obj", so a project
    /// that relocates it is judged where its output actually is. The ORIGIN comes from MSBuild's
    /// <c>DefiningProjectFullPath</c> rather than from the file's name, so
    /// <c>Brick.AssemblyInfo.cs</c> written by <c>Microsoft.NET.GenerateAssemblyInfo.targets</c> is
    /// tolerated while <c>obj/Brick.AssemblyInfo.cs</c> written by an author's target — same name,
    /// same directory — is not. Four previous rounds drew this line on names and each one moved the
    /// hole rather than closing it.</para>
    ///
    /// <para>This is the one place MSBuild's post-build item list is consulted, and it can only
    /// ever SHRINK what is tolerated: a file missing from it (because a target scrubbed the item
    /// after the compile) is a file with no SDK declaration to point at, which is a refusal.</para>
    /// </remarks>
    private static HashSet<string> SdkGeneratedFilesUnderIntermediateOutput(EvaluatedBrickProject built)
    {
        var tolerated = new HashSet<string>(PathComparer);
        var intermediate = built.DirectoryProperty("IntermediateOutputPath")
            ?? built.DirectoryProperty("BaseIntermediateOutputPath");
        if (intermediate is null)
        {
            // No intermediate output directory means no tolerance, which refuses rather than
            // admits. That is the correct direction for a question the gate cannot answer.
            return tolerated;
        }

        foreach (var item in built.Compile)
        {
            if (string.IsNullOrWhiteSpace(item.FullPath) || !built.IsSdkDeclared(item))
            {
                continue;
            }

            string full;
            try
            {
                full = Path.GetFullPath(item.FullPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (IsInside(intermediate, full))
            {
                tolerated.Add(full);
            }
        }

        return tolerated;
    }

    /// <summary>
    /// A path relative to the brick directory when it is inside it, and absolute when it is not —
    /// so a refusal never renders an outside path as a pile of <c>../</c> the reader must decode.
    /// </summary>
    private static string Relative(string projectDir, string full) =>
        IsInside(projectDir, full) ? Path.GetRelativePath(projectDir, full) : full;

    /// <summary>
    /// Brick source bytes as text, with exactly <see cref="File.ReadAllText(string)"/>'s encoding
    /// rules — UTF-8, with a byte-order mark honoured when one is present.
    /// </summary>
    /// <remarks>
    /// The gate reads the source once, as bytes, so that the text it hashes and the bytes it
    /// compares against the compiler's checksum are provably the same read. Decoding here rather
    /// than reading the file a second time is what makes that true.
    /// </remarks>
    private static string Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
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

    /// <summary>
    /// The assemblies the brick was compiled against, as paths for Roslyn metadata references —
    /// the compiler's own reference set, located through the build's, never the other way round.
    /// </summary>
    /// <remarks>
    /// <para>This used to glob <c>*.dll</c> out of the build output directory. That directory is a
    /// MODEL of the compiler's reference set, and a poor one: the SDK does not copy package
    /// assemblies into a library's output (<c>CopyLocalLockFileAssemblies</c> defaults to off), so
    /// a stock brick referencing <c>Ashlar.Authoring</c> — the shape <c>ashlar new brick</c>
    /// scaffolds — built to an output holding only itself, Roslyn was handed no assembly defining
    /// <c>Ashlar.Core.Domain.Bricks.Brick</c>, and the analyzer fence refused with "analyzer anchor
    /// type ... is not resolvable" until the author added an MSBuild property the docs listed under
    /// "things that will bite you". A scaffold that cannot be certified as scaffolded is a defect
    /// in the gate, not in the scaffold.</para>
    ///
    /// <para>The build already knows its reference set twice over, and this method uses both
    /// halves for what each can honestly answer. MSBuild's <c>ReferencePathWithRefAssemblies</c>
    /// items — asked for in the build's own invocation, see <see cref="EvaluatedBrickProject.Build"/>
    /// — are the paths <c>CoreCompile</c> handed csc, so they say WHERE the assemblies live. But it
    /// is a post-build item list, and <see cref="CompiledSourceDocuments"/> explains at length why
    /// no such list is an authority on what was compiled: a target can edit it after the compile.
    /// So membership is decided by the compiler's own record instead — the metadata-reference table
    /// csc writes into the portable PDB (<see cref="CompiledMetadataReferences"/>), one file name
    /// and one MVID per reference — and a path is accepted for a recorded reference only when the
    /// file at that path HAS that MVID. A reference the compiler recorded that no reported file
    /// matches is a refusal by name; a reported path the compiler did not record is simply not
    /// handed on, because Roslyn is meant to see what csc saw and nothing else. The one deliberate
    /// difference from "what csc saw": the target framework's reference assemblies are verified
    /// like every other reference and then withheld, because the in-process compilation already
    /// carries the host runtime's framework and a second core library breaks it — see the comment
    /// in the method body.</para>
    ///
    /// <para>The two filters from the previous design are kept and now apply to the reported paths:
    /// anything under a <c>runtimes/&lt;rid&gt;/native/</c> segment is skipped without being opened,
    /// and everything else is probed for a managed metadata root before its MVID is read. Handing
    /// an unmanaged file to <c>MetadataReference.CreateFromFile</c> produces CS0009 at compile
    /// time and the fence then blames the candidate for the harness's own error; that happened
    /// once, with LLamaSharp's natives, and it must not happen again whatever list they arrive in.
    /// An unmanaged file has no MVID, so it can match no recorded reference either.</para>
    ///
    /// <para>Fail CLOSED, throughout. An empty reported list, a PDB without the record, a recorded
    /// reference with no matching file: each is "the gate cannot establish what the brick was
    /// compiled against", and each is a refusal naming what is missing rather than a fallback to a
    /// partial set. A partial set is what the glob was.</para>
    /// </remarks>
    /// <param name="built">The project as MSBuild reported it at the end of the build that
    /// produced <paramref name="primaryDll"/>.</param>
    /// <param name="primaryDll">The built brick assembly, whose PDB carries the compiler's record.</param>
    internal static List<string> CollectReferences(EvaluatedBrickProject built, string primaryDll)
    {
        var name = Path.GetFileName(built.ProjectPath);
        if (built.CompilerReferences.Count == 0)
        {
            throw new InvalidOperationException(
                $"Brick project refused: the build of {name} reported no compiler references at all "
                + "(ReferencePathWithRefAssemblies was empty when the build finished). Every C# compilation "
                + "references at least the target framework, so the list was emptied after CoreCompile ran, and "
                + "the gate cannot locate the assemblies the brick was compiled against. Fix: remove the MSBuild "
                + $"target in the project or in the Directory.Build.props / Directory.Build.targets beside {name} "
                + "that removes items from ReferencePathWithRefAssemblies after the compile, then certify again. "
                + "Refusing rather than analyzing the brick against a reference set the gate had to guess.");
        }

        var reported = new List<string>();
        var framework = new HashSet<string>(PathComparer);
        foreach (var item in built.CompilerReferences)
        {
            var path = string.IsNullOrWhiteSpace(item.FullPath) ? item.Identity : item.FullPath;
            reported.Add(path);
            if (string.IsNullOrWhiteSpace(item.Meta("FrameworkReferenceName")))
            {
                continue;
            }

            try
            {
                framework.Add(Path.GetFullPath(path));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Unresolvable as a path, so ResolveCompilerReferences cannot match it either; if the
                // compiler recorded it, that is the refusal.
            }
        }

        var resolved = ResolveCompilerReferences(name, reported, CompiledMetadataReferences.Read(primaryDll), primaryDll);

        // Every reference is verified above, framework included. The targeting pack's reference
        // assemblies are then NOT handed on: the fence and the mutation leg compile inside this
        // process, and RoslynCodeAnalysisService.BuildReferenceSet always supplies the running
        // runtime's own framework assemblies first. Adding a second System.Runtime — the reference
        // assembly, which defines System.Object itself rather than forwarding to a core library —
        // leaves Roslyn with two candidate core libraries and no way to choose, and every predefined
        // type then fails to resolve (CS0518 on System.Void, System.Object, System.String ...).
        // Observed live on the first run of this method; the brick's own framework has always been
        // the host's job here, and this keeps it that way.
        return resolved.Where(path => !framework.Contains(path)).ToList();
    }

    /// <summary>
    /// Joins the paths the build reported to the references the compiler recorded, by MVID. Pure
    /// apart from reading the files named, so the refusals can be pinned without a build.
    /// </summary>
    /// <param name="projectName">The brick project's file name, for the refusals.</param>
    /// <param name="reportedPaths">Where MSBuild says the compiler's references live.</param>
    /// <param name="recorded">What the compiler says it compiled against.</param>
    /// <param name="primaryDll">The brick assembly itself, always first in the result.</param>
    internal static List<string> ResolveCompilerReferences(
        string projectName,
        IReadOnlyList<string> reportedPaths,
        IReadOnlyList<CompiledMetadataReference> recorded,
        string primaryDll)
    {
        // Every reported path the compiler COULD have opened, indexed by the identity the compiler
        // recorded it under. Anything unmanaged is dropped here, before Roslyn can see it.
        var byMvid = new Dictionary<Guid, string>();
        // Both keyed by FILE NAME, because that is all the compiler records: when a recorded
        // reference matches nothing, the reported path of the same name says which way it failed.
        var unreadable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var readableByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reportedPath in reportedPaths)
        {
            string full;
            try
            {
                full = Path.GetFullPath(reportedPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                unreadable.TryAdd(Path.GetFileName(reportedPath), reportedPath);
                continue;
            }

            if (IsUnderNativeRuntimesFolder(full))
            {
                continue; // the runtime graph, not the compile graph — never a reference, whatever list it is in
            }

            var mvid = CompiledMetadataReferences.TryReadMvid(full);
            if (mvid is null)
            {
                unreadable.TryAdd(Path.GetFileName(full), full);
                continue;
            }

            readableByName.TryAdd(Path.GetFileName(full), full);
            byMvid.TryAdd(mvid.Value, full); // the same module reported twice: one path is enough
        }

        var references = new List<string> { primaryDll };
        var seen = new HashSet<string>(PathComparer) { primaryDll };
        foreach (var reference in recorded)
        {
            if (byMvid.TryGetValue(reference.Mvid, out var path))
            {
                if (seen.Add(path))
                {
                    references.Add(path);
                }

                continue;
            }

            // Three different states end here, and the author needs to know which. Name it.
            string because;
            if (unreadable.TryGetValue(reference.FileName, out var unreadablePath))
            {
                because = $"the build reported it at '{unreadablePath}', but that file is missing, unreadable, or "
                          + "not a managed assembly, so it cannot be what the compiler opened";
            }
            else if (readableByName.TryGetValue(reference.FileName, out var differentPath))
            {
                because = $"the build reported it at '{differentPath}', but the assembly at that path is a DIFFERENT "
                          + $"module (its MVID is not {reference.Mvid:D}) from the one the compiler opened — it was "
                          + "replaced after the compile";
            }
            else
            {
                because = "the reference list the build reported when it finished does not contain it — it was "
                          + "removed from ReferencePathWithRefAssemblies after CoreCompile ran";
            }

            throw new InvalidOperationException(
                $"Brick project refused: {projectName} was compiled against '{reference.FileName}' (MVID "
                + $"{reference.Mvid:D}) — the compiler's own record says so — but the gate cannot locate that "
                + $"assembly: {because}. The analyzer fence and the mutation leg re-compile the brick source "
                + "against the compiler's references, and a reference the gate cannot find is one it would have "
                + "to guess at or leave out, either of which judges a different program from the one that was "
                + "built. Fix: remove the MSBuild target in the project or in the Directory.Build.props / "
                + $"Directory.Build.targets beside {projectName} that edits ReferencePathWithRefAssemblies or "
                + "replaces reference assemblies after the compile (an AfterTargets=\"CoreCompile\" target, or one "
                + "that invokes the compiler itself), then certify again. Refusing rather than analyzing the "
                + "brick against assemblies its compiler never saw.");
        }

        return references;
    }

    /// <summary>True for <c>.../runtimes/&lt;rid&gt;/native/...</c>, the convention location for the
    /// unmanaged payloads NuGet ships alongside a managed graph.</summary>
    private static bool IsUnderNativeRuntimesFolder(string file)
    {
        var segments = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
