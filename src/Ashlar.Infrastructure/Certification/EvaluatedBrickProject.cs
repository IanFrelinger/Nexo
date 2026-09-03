using System.Diagnostics;
using System.Text.Json;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// One MSBuild item, as MSBuild itself evaluated it: the spec the project wrote, the absolute path
/// it resolved to, and the metadata attached to it.
/// </summary>
/// <param name="Identity">The item spec as written (<c>Include</c> after property expansion).</param>
/// <param name="FullPath">The absolute path MSBuild resolved the identity to.</param>
/// <param name="Metadata">Every metadata value on the item, keyed case-insensitively.</param>
internal sealed record EvaluatedItem(
    string Identity,
    string FullPath,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>The value of one metadata entry, or <c>null</c>.</summary>
    public string? Meta(string name) => Metadata.TryGetValue(name, out var value) ? value : null;
}

/// <summary>
/// What MSBuild says a brick project actually is: the <c>Compile</c> items it will hand csc, the
/// package and project references it carries, and the properties that govern them.
/// </summary>
/// <remarks>
/// <para>This type exists because four rounds of increasingly careful GUESSING at those answers
/// each moved a certification bypass rather than closing it. Globbing <c>*.cs</c> and reading the
/// <c>.csproj</c> as XML models the compiler; it is not the compiler. Every gap between the model
/// and the truth is a file that ships inside the certified assembly while sitting outside the
/// content hash, the analyzer fence and the mutation leg — which is a signed certificate asserting
/// more than was ever checked.</para>
///
/// <para>Concretely, the model was wrong in at least four ways at once. <c>&lt;Compile
/// Include="Payload.cstxt" /&gt;</c> compiles a file the <c>*.cs</c> glob cannot see (csc compiles
/// whatever it is handed; the extension is decoration). <c>Sub/obj/Payload.cs</c> is compiled by
/// the SDK — which excludes only the project's OWN <c>obj/</c> and <c>bin/</c>, not a directory of
/// that name at any depth. A <c>Directory.Build.props</c> beside the project adds references and
/// compile items that reading one XML file never sees. And <c>&lt;Compile Remove="Brick.cs"
/// /&gt;</c> makes the hashed set DISJOINT from the compiled set, so the certificate is signed over
/// a decoy. Asking MSBuild removes all four at once, and every future variant of them, because the
/// answer is no longer an approximation of the build — it is the build's own answer.</para>
///
/// <para><see cref="Evaluate"/> runs no targets: <c>-getItem</c> without a target evaluates the
/// project and stops — nothing is restored, nothing is compiled, no author target executes — so
/// the gate can decide what it is certifying, and whether it can certify at all, before any of the
/// candidate's code exists as a binary. That is the answer the content hash is taken over. It adds
/// no attack surface either: <see cref="BrickCertificationProjectLoader.LoadAsync"/> already shells
/// a full build of this same project and then <c>Assembly.LoadFrom</c>s the result, so an
/// evaluation that runs no targets is strictly less than what already happens.</para>
///
/// <para><see cref="Build"/> is the other half, and it is deliberately ONE invocation that both
/// builds and reports: a query issued separately from the build runs under different properties,
/// and a conditioned target splits the two answers. Even so, the item list it returns is not
/// treated as authority over what was compiled — see <see cref="CompiledSourceDocuments"/> for why
/// nothing MSBuild reports can be, and for what is used instead.</para>
/// </remarks>
internal sealed class EvaluatedBrickProject
{
    private EvaluatedBrickProject(
        string projectPath,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyList<EvaluatedItem> compile,
        IReadOnlyList<EvaluatedItem> packageReferences,
        IReadOnlyList<EvaluatedItem> projectReferences,
        IReadOnlyList<EvaluatedItem> analyzers,
        IReadOnlyList<EvaluatedItem> references,
        IReadOnlyList<EvaluatedItem> compilerReferences)
    {
        ProjectPath = projectPath;
        Properties = properties;
        Compile = compile;
        PackageReferences = packageReferences;
        ProjectReferences = projectReferences;
        Analyzers = analyzers;
        References = references;
        CompilerReferences = compilerReferences;
    }

    /// <summary>Absolute path of the evaluated project file.</summary>
    public string ProjectPath { get; }

    /// <summary>The properties that were asked for, as evaluated.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>Every file the compiler will be handed, in MSBuild's order.</summary>
    public IReadOnlyList<EvaluatedItem> Compile { get; }

    /// <summary>Every <c>PackageReference</c>, wherever in the import chain it was declared.</summary>
    public IReadOnlyList<EvaluatedItem> PackageReferences { get; }

    /// <summary>Every <c>ProjectReference</c>, wherever in the import chain it was declared.</summary>
    public IReadOnlyList<EvaluatedItem> ProjectReferences { get; }

    /// <summary>
    /// Every <c>Analyzer</c> item — the assemblies Roslyn loads into the compilation, which
    /// includes SOURCE GENERATORS. A generator writes code straight into the assembly without ever
    /// being a <c>Compile</c> item, so this list is the other half of "what ends up inside the
    /// brick".
    /// </summary>
    public IReadOnlyList<EvaluatedItem> Analyzers { get; }

    /// <summary>
    /// Every <c>Reference</c> item — a raw assembly reference, which is a dependency that never
    /// passes through the <c>PackageReference</c> allow-list.
    /// </summary>
    public IReadOnlyList<EvaluatedItem> References { get; }

    /// <summary>
    /// Every <c>ReferencePathWithRefAssemblies</c> item — the resolved assembly paths the SDK's
    /// <c>CoreCompile</c> target hands csc as <c>/reference</c>, package assemblies in the NuGet
    /// cache and targeting-pack reference assemblies alike. Populated only by <see cref="Build"/>;
    /// it is produced by targets, so an <see cref="Evaluate"/> answer reports it empty.
    /// </summary>
    /// <remarks>
    /// This is where the references live, not proof of what was compiled against: it is a
    /// post-build item list, and <see cref="CompiledSourceDocuments"/> explains why no such list
    /// is an authority. <see cref="BrickCertificationProjectLoader"/> checks each path against the
    /// MVID the compiler itself recorded for that reference before handing it on.
    /// </remarks>
    public IReadOnlyList<EvaluatedItem> CompilerReferences { get; }

    /// <summary>The value of one evaluated property, or <c>null</c> when MSBuild did not report it.</summary>
    public string? Property(string name) => Properties.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// True when the file that DECLARED this item ships with the .NET SDK, rather than sitting
    /// anywhere in the author's project chain.
    /// </summary>
    /// <remarks>
    /// <para>This is the only distinction the gate draws between "the toolchain put this here" and
    /// "the candidate put this here", and it is drawn on MSBuild's own <c>DefiningProjectFullPath</c>
    /// rather than on a file NAME — the thing four previous rounds got wrong. The SDK's assembly-info
    /// boilerplate under <c>obj/</c> is declared by <c>Microsoft.NET.GenerateAssemblyInfo.targets</c>;
    /// a payload smuggled in by an author's target is declared by their own <c>.csproj</c>,
    /// <c>Directory.Build.props</c> or <c>.targets</c>, wherever it sits.</para>
    ///
    /// <para>It fails closed twice over: an item whose origin MSBuild did not report is NOT
    /// SDK-declared, and a project whose <c>NetCoreRoot</c> could not be read makes the question
    /// unanswerable, which <see cref="SdkRoot"/> turns into a refusal rather than a default.</para>
    /// </remarks>
    public bool IsSdkDeclared(EvaluatedItem item)
    {
        var origin = item.Meta("DefiningProjectFullPath");
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        try
        {
            return Path.GetFullPath(origin).StartsWith(SdkRoot, comparison);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// Where the .NET SDK lives, with a trailing separator — or a refusal, because "the gate cannot
    /// tell SDK-declared from author-declared" is not a state to guess in.
    /// </summary>
    public string SdkRoot
    {
        get
        {
            var root = Property("NetCoreRoot");
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new InvalidOperationException(Refusal(ProjectPath,
                    "MSBuild did not report NetCoreRoot, so the gate cannot tell which of its compile items the "
                    + "SDK itself declared and which the project did"));
            }

            var full = Path.GetFullPath(root);
            return full.EndsWith(Path.DirectorySeparatorChar) ? full : full + Path.DirectorySeparatorChar;
        }
    }

    private static readonly string[] RequestedProperties =
    [
        "EnableDefaultItems",
        "EnableDefaultCompileItems",
        "TargetFramework",
        "TargetFrameworks",
        // Where the SDK lives, and where this project's own intermediate output sits. Both are
        // needed to judge the post-compile set: the only compiled file the gate tolerates outside
        // the hash is SDK-generated assembly boilerplate under the project's own obj/.
        "NetCoreRoot",
        "BaseIntermediateOutputPath",
        "IntermediateOutputPath"
    ];

    /// <summary>
    /// An evaluated property as a directory path rooted at the project, with MSBuild's separators
    /// translated to this platform's.
    /// </summary>
    /// <remarks>
    /// MSBuild reports these with the separator the defining file wrote, which on Linux means
    /// <c>BaseIntermediateOutputPath</c> can come back as the literal <c>obj\</c> — observed on the
    /// SDK in this repo's container, and only on SOME of the two query shapes. <c>Path.Combine</c>
    /// then produces a directory named <c>obj\</c>, every containment test against it fails, and the
    /// SDK's own assembly-info boilerplate stops being recognised — which would refuse every honest
    /// brick. Translating the separator is not tidiness; it is the difference between a rule and a
    /// blanket refusal.
    /// </remarks>
    public string? DirectoryProperty(string name)
    {
        var value = Property(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value!.Replace('\\', Path.DirectorySeparatorChar)
                               .Replace('/', Path.DirectorySeparatorChar);
        try
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ProjectPath)!, normalized));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static readonly string[] RequestedItems =
    [
        "Compile",
        "PackageReference",
        "ProjectReference",
        // Analyzer covers source generators — code that enters the assembly without ever being a
        // Compile item — and Reference covers a raw assembly reference, which is a dependency that
        // never passes the PackageReference allow-list. Both were invisible to the XML scan.
        "Analyzer",
        "Reference",
        // The paths CoreCompile passes csc. A build-time item: MSBuild reports it as an empty list
        // under a target-less evaluation, which is the answer that is wanted there.
        "ReferencePathWithRefAssemblies"
    ];

    /// <summary>How long the gate will wait for an evaluation before calling it unanswerable.</summary>
    private static readonly TimeSpan EvaluationTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// How long the gate will wait for a build. Longer than an evaluation because it includes a
    /// restore, which on a cold NuGet cache is the slow part and is not the candidate's fault.
    /// </summary>
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Asks MSBuild what <paramref name="projectPath"/> compiles and references, or REFUSES.
    /// </summary>
    /// <remarks>
    /// Every failure here — MSBuild missing, evaluation erroring, the timeout expiring, output that
    /// will not parse — throws. That is deliberate and it is the invariant this whole area keeps
    /// rediscovering: a project whose compiled set the gate cannot establish must be a REFUSAL, not
    /// a fallback to the old guess. A fallback is precisely how an unhashed file gets into a signed
    /// assembly.
    /// </remarks>
    public static EvaluatedBrickProject Evaluate(string projectPath) => Run(projectPath, extraArguments: []);

    /// <summary>
    /// The properties the gate FORCES on the brick's build, as global properties, because each one
    /// is a precondition for being able to read what was compiled afterwards.
    /// </summary>
    /// <remarks>
    /// <para>A global property cannot be overridden from inside the project being judged — not by
    /// its <c>.csproj</c>, not by a <c>Directory.Build.props</c>, not by a <c>PropertyGroup</c>
    /// inside one of its targets. That is the whole point of setting these here rather than reading
    /// them: they turn "the candidate chose not to leave a record" from a bypass into an
    /// impossibility, and leave a genuine absence of the record (an author who replaced CoreCompile
    /// outright) as the refusal it should be.</para>
    ///
    /// <para><c>DebugType=portable</c> makes the PDB exist. <c>ChecksumAlgorithm=SHA256</c> makes
    /// the per-file checksums in it something the gate will accept — SHA1 is still read, but is not
    /// something to depend on for a signed claim. <c>PathMap</c> empty and
    /// <c>DeterministicSourcePaths=false</c> keep the document paths as real paths on this disk;
    /// under a path map they are rewritten placeholders, and every one of them would fail to match
    /// the hashed set, which is a refusal of honest bricks rather than a defence.</para>
    /// </remarks>
    private static readonly string[] ForcedBuildProperties =
    [
        "-p:DebugType=portable",
        "-p:ChecksumAlgorithm=SHA256",
        "-p:PathMap=",
        "-p:DeterministicSourcePaths=false",
        "-p:TreatWarningsAsErrors=false",
        "-p:NuGetAudit=false"
    ];

    /// <summary>The outcome of building a brick: how it went, what it said, and what it compiled.</summary>
    /// <param name="ExitCode">MSBuild's exit code.</param>
    /// <param name="Output">Diagnostics, populated on failure.</param>
    /// <param name="Project">The evaluated project as it stood when the build finished, or
    /// <c>null</c> when the build failed.</param>
    internal sealed record BuildOutcome(int ExitCode, string Output, EvaluatedBrickProject? Project);

    /// <summary>
    /// Builds the brick and reports what it compiled, in ONE MSBuild invocation.
    /// </summary>
    /// <remarks>
    /// <para>One invocation, not two, and that is the point. A second query — however it is
    /// phrased — runs under a different property set than the build, and a
    /// <c>&lt;Target BeforeTargets="CoreCompile" Condition="..."&gt;</c> that tests a property the
    /// two runs disagree about then contributes its payload to the build and nothing to the query.
    /// Reproduced live on this repo against exactly that shape, conditioned on
    /// <c>$(OutputPath)</c>, which necessarily differs because the gate builds into a temp
    /// directory: the payload's type was in the built assembly, and the verification query reported
    /// a clean two-file project. Asking in the same invocation removes the disagreement by
    /// construction rather than by enumerating which properties to match.</para>
    ///
    /// <para>Note what this still does NOT establish, and why <see cref="CompiledSourceDocuments"/>
    /// exists: a target running <c>AfterTargets="CoreCompile"</c> can remove its payload from
    /// <c>@(Compile)</c> once the compile has happened, and the item list read at the end of the
    /// build is then clean while the assembly is not. That was reproduced live too. MSBuild's
    /// answer is therefore used only to NARROW the one tolerance the gate grants (SDK boilerplate
    /// under the project's own <c>obj/</c>) — never to admit a file. Its failure mode is a refusal,
    /// which is the direction that is safe to be wrong in.</para>
    ///
    /// <para><c>dotnet msbuild -restore -t:Build</c> rather than <c>dotnet build</c> because
    /// <c>dotnet build -getItem:Compile</c> reports the EVALUATION-time item list even though it
    /// runs targets — verified against the SDK in this repo's container, where it missed a
    /// target-added compile item that <c>dotnet msbuild -t:Build -getItem:Compile</c> reported.
    /// </para>
    /// </remarks>
    /// <param name="projectPath">The brick project.</param>
    /// <param name="outputDirectory">Where the build output goes.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="nugetConfigFile">An explicit NuGet config for the restore, or <c>null</c>.</param>
    public static BuildOutcome Build(
        string projectPath, string outputDirectory, string configuration, string? nugetConfigFile)
    {
        var arguments = new List<string>
        {
            "-restore",
            "-t:Build",
            "-p:Configuration=" + configuration,
            // Trailing separator: MSBuild treats OutputPath as a directory prefix, and without it
            // the output lands in a sibling named after the directory.
            "-p:OutputPath=" + outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar
        };
        arguments.AddRange(ForcedBuildProperties);
        if (!string.IsNullOrWhiteSpace(nugetConfigFile))
        {
            arguments.Add("-p:RestoreConfigFile=" + nugetConfigFile);
        }

        var (exitCode, stdout, stderr) = Invoke(projectPath, [.. arguments, .. QueryArguments()], BuildTimeout);
        if (exitCode == 0)
        {
            return new BuildOutcome(exitCode, string.Empty, Parse(Path.GetFullPath(projectPath), stdout));
        }

        // -getItem replaces MSBuild's console output with JSON, so a failed build says nothing about
        // WHY. Re-run without it to recover the compiler's diagnostics: the build produced no
        // assembly, so there is nothing to be raced, and the cost falls only on the failure path.
        var (_, failOut, failErr) = Invoke(projectPath, [.. arguments, "-v:m"], BuildTimeout);
        var diagnostics = Trim(failOut + failErr);
        return new BuildOutcome(
            exitCode,
            diagnostics.Length > 0 ? diagnostics : Trim(stdout + stderr),
            null);
    }

    private static IEnumerable<string> QueryArguments() =>
        RequestedItems.Select(i => "-getItem:" + i).Concat(RequestedProperties.Select(p => "-getProperty:" + p));

    private static EvaluatedBrickProject Run(string projectPath, IReadOnlyList<string> extraArguments)
    {
        var full = Path.GetFullPath(projectPath);
        var (exitCode, stdout, stderr) = Invoke(full, [.. extraArguments, .. QueryArguments()]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(Refusal(full,
                $"MSBuild could not evaluate it (exit {exitCode}). {Trim(stdout + stderr)}"));
        }

        return Parse(full, stdout);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) Invoke(
        string projectPath, IReadOnlyList<string> extraArguments, TimeSpan? timeout = null)
    {
        var limit = timeout ?? EvaluationTimeout;
        var full = Path.GetFullPath(projectPath);
        if (!File.Exists(full))
        {
            throw new FileNotFoundException(
                $"Cannot evaluate {full}: the project file does not exist. Certification hashes the set of "
                + "files MSBuild compiles, and there is no project here to ask.", full);
        }

        var arguments = new List<string> { "msbuild", full, "-nologo" };
        arguments.AddRange(extraArguments);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(full)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }
        // Evaluation must answer for the AUTHOR's build, not for whatever build happens to be
        // running this gate. MSBuild reads environment variables as properties, so a host that is
        // itself an MSBuild process (a `dotnet test` run, the CI build) would otherwise inject its
        // own TargetFramework — changing which conditioned ItemGroups contribute — and its own
        // toolset paths, which point a nested evaluation at the wrong SDK.
        foreach (var poisoned in new[]
                 {
                     "TargetFramework", "TargetFrameworks", "Configuration", "Platform",
                     "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBuildExtensionsPath32",
                     "MSBuildExtensionsPath64", "MSBUILD_EXE_PATH", "MSBuildStartupDirectory",
                     "MSBuildLoadMicrosoftTargetsReadOnly"
                 })
        {
            psi.Environment.Remove(poisoned);
        }
        psi.Environment["MSBUILDTERMINALLOGGER"] = "off";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        string stdout;
        string stderr;
        int exitCode;
        try
        {
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start `dotnet msbuild`");
            var outTask = process.StandardOutput.ReadToEndAsync();
            var errTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)limit.TotalMilliseconds))
            {
                TryKill(process);
                throw new InvalidOperationException(Refusal(full,
                    $"MSBuild did not finish within {limit.TotalMinutes:0} minutes"));
            }
            stdout = outTask.GetAwaiter().GetResult();
            stderr = errTask.GetAwaiter().GetResult();
            exitCode = process.ExitCode;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or PlatformNotSupportedException)
        {
            throw new InvalidOperationException(Refusal(full,
                $"`dotnet` could not be run ({ex.Message}). The certified source set is whatever MSBuild "
                + "compiles, and without the SDK the gate cannot know it"), ex);
        }

        return (exitCode, stdout, stderr);
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            // Already gone, or not ours to kill. The refusal stands either way.
        }
    }

    private static EvaluatedBrickProject Parse(string projectPath, string stdout)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(stdout);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(Refusal(projectPath,
                $"MSBuild's answer could not be read as JSON ({ex.Message}). {Trim(stdout)}"), ex);
        }

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in props.EnumerateObject())
            {
                properties[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }
        }

        var items = new Dictionary<string, IReadOnlyList<EvaluatedItem>>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("Items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in itemsElement.EnumerateObject())
            {
                items[group.Name] = ReadItems(projectPath, group.Value);
            }
        }

        foreach (var name in RequestedItems)
        {
            if (!items.ContainsKey(name))
            {
                // A requested item group that came back absent means the answer is incomplete, and
                // an incomplete answer about what a project compiles is not one to certify against.
                throw new InvalidOperationException(Refusal(projectPath,
                    $"MSBuild's answer did not include the '{name}' items that were asked for"));
            }
        }

        return new EvaluatedBrickProject(
            projectPath,
            properties,
            items["Compile"],
            items["PackageReference"],
            items["ProjectReference"],
            items["Analyzer"],
            items["Reference"],
            items["ReferencePathWithRefAssemblies"]);
    }

    private static List<EvaluatedItem> ReadItems(string projectPath, JsonElement array)
    {
        var result = new List<EvaluatedItem>();
        if (array.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            var identity = metadata.TryGetValue("Identity", out var id) ? id : string.Empty;
            if (identity.Length == 0)
            {
                throw new InvalidOperationException(Refusal(projectPath,
                    "MSBuild reported an item with no Identity"));
            }
            metadata.TryGetValue("FullPath", out var fullPath);
            result.Add(new EvaluatedItem(identity, fullPath ?? string.Empty, metadata));
        }

        return result;
    }

    private static string Refusal(string projectPath, string because) =>
        $"Brick project refused: the gate could not establish what {Path.GetFileName(projectPath)} compiles — "
        + because + ". Certification signs a content hash over the COMPILED source set, so a project whose "
        + "compiled set cannot be established is refused rather than hashed on a guess. Fix: make "
        + $"`dotnet msbuild \"{projectPath}\" -getItem:Compile` succeed — usually that means repairing the "
        + "project file or its Directory.Build.props/targets, or installing the .NET SDK on this machine.";

    private static string Trim(string output)
    {
        var text = output.Trim();
        const int limit = 1200;
        return text.Length <= limit ? text : text[..limit] + " …";
    }
}
