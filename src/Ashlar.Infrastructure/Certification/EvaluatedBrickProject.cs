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
/// <para>Why a separate <c>dotnet msbuild -getItem</c> process rather than emitting the item list
/// from the <c>dotnet build</c> the loader already runs: this evaluation must be able to REFUSE
/// before the build happens. <c>-getItem</c> without a target evaluates the project and stops — no
/// target executes, nothing is restored, nothing is compiled — so the gate can decide what it is
/// certifying, and whether it can certify at all, before any of the candidate's code exists as a
/// binary. Emitting from the build would put the answer AFTER the point where refusing is free,
/// and would mean injecting a custom target into someone else's project to get it. It also adds no
/// attack surface: <see cref="BrickCertificationProjectLoader.LoadAsync"/> already shells a full
/// <c>dotnet build</c> of this same project and then <c>Assembly.LoadFrom</c>s the result, so an
/// evaluation that runs no targets is strictly less than what already happens.</para>
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
        IReadOnlyList<EvaluatedItem> references)
    {
        ProjectPath = projectPath;
        Properties = properties;
        Compile = compile;
        PackageReferences = packageReferences;
        ProjectReferences = projectReferences;
        Analyzers = analyzers;
        References = references;
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
        "BaseIntermediateOutputPath"
    ];

    private static readonly string[] RequestedItems =
    [
        "Compile",
        "PackageReference",
        "ProjectReference",
        // Analyzer covers source generators — code that enters the assembly without ever being a
        // Compile item — and Reference covers a raw assembly reference, which is a dependency that
        // never passes the PackageReference allow-list. Both were invisible to the XML scan.
        "Analyzer",
        "Reference"
    ];

    /// <summary>How long the gate will wait for an evaluation before calling it unanswerable.</summary>
    private static readonly TimeSpan EvaluationTimeout = TimeSpan.FromMinutes(3);

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
    /// What the compiler was ACTUALLY handed: the <c>Compile</c> set as it stands once the targets
    /// that build it have run, not as it stood at evaluation.
    /// </summary>
    /// <remarks>
    /// <para>This exists because <see cref="Evaluate"/> alone leaves the same hole one step later.
    /// Evaluation answers for the props, the imports, the removes and the non-<c>.cs</c> includes —
    /// but a <c>&lt;Target BeforeTargets="CoreCompile"&gt;</c> adding
    /// <c>&lt;Compile Include="Payload.cstxt" /&gt;</c> contributes NOTHING at evaluation time and
    /// everything at compile time. Reproduced on this repo: the payload is in the built assembly and
    /// absent from every evaluation-time answer, which is bypass #1 again with the item declared in
    /// a target instead of an ItemGroup. A target is part of the same import chain a
    /// <c>Directory.Build.props</c> is part of, so it needs no csproj edit either.</para>
    ///
    /// <para>Running <c>-t:CoreCompile</c> costs nothing after the build the loader already runs:
    /// the compile is up to date, so MSBuild skips it and reports the item list. And it runs the
    /// author's targets — which the loader's own <c>dotnet build</c> has already run in full, and
    /// which it then <c>Assembly.LoadFrom</c>s the product of, so this adds no attack surface.</para>
    /// </remarks>
    /// <param name="projectPath">The brick project.</param>
    /// <param name="configuration">The configuration the loader built, so the query sees that
    /// build's own intermediate output rather than provoking a second one.</param>
    public static EvaluatedBrickProject EvaluateAfterCompile(string projectPath, string configuration) =>
        Run(projectPath,
        [
            "-t:CoreCompile",
            "-p:Configuration=" + configuration,
            "-p:TreatWarningsAsErrors=false",
            "-p:NuGetAudit=false"
        ]);

    private static EvaluatedBrickProject Run(string projectPath, IReadOnlyList<string> extraArguments)
    {
        var full = Path.GetFullPath(projectPath);
        if (!File.Exists(full))
        {
            throw new FileNotFoundException(
                $"Cannot evaluate {full}: the project file does not exist. Certification hashes the set of "
                + "files MSBuild compiles, and there is no project here to ask.", full);
        }

        var arguments = new List<string> { "msbuild", full, "-nologo" };
        arguments.AddRange(extraArguments);
        arguments.AddRange(RequestedItems.Select(i => "-getItem:" + i));
        arguments.AddRange(RequestedProperties.Select(p => "-getProperty:" + p));

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
            if (!process.WaitForExit((int)EvaluationTimeout.TotalMilliseconds))
            {
                TryKill(process);
                throw new InvalidOperationException(Refusal(full,
                    $"evaluating it did not finish within {EvaluationTimeout.TotalMinutes:0} minutes"));
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

        if (exitCode != 0)
        {
            throw new InvalidOperationException(Refusal(full,
                $"MSBuild could not evaluate it (exit {exitCode}). {Trim(stdout + stderr)}"));
        }

        return Parse(full, stdout);
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
            items["Reference"]);
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
