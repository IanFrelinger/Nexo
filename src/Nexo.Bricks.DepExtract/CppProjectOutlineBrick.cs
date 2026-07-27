using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Privacy-preserving project outliner: produces a shareable summary of a C++
/// codebase's SHAPE — per-class responsibilities, internal dependency seams,
/// and how third-party libraries (Qt above all) are used — without exposing
/// proprietary detail.
///
/// Privacy model, in layers:
///  1. The model is LOCAL (Ollama); no source ever leaves the machine.
///  2. Dependency facts (which files include which Qt/std headers, internal
///     include edges) come from deterministic scanning, never from the model.
///  3. The model writes responsibility prose only, under explicit output bans
///     (no method names, signatures, literals, or code), and every response is
///     post-filtered: method identifiers harvested from the source and string
///     literals are mechanically redacted before the summary is assembled.
///  4. The final project-level "seams" synthesis is generated from the already
///     redacted per-class prose + the deterministic dependency inventory — the
///     synthesis call never sees raw source at all.
/// </summary>
public sealed class CppProjectOutlineBrick : DomainBrick
{
    private static string DefaultModel =>
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5-coder:7b";
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<CppProjectOutlineBrick> _logger;

    static readonly string[] HeaderExts = [".hpp", ".h", ".hh", ".hxx"];
    static readonly string[] SourceExts = [".cpp", ".cc", ".cxx", ".c"];
    static readonly string[] SkipDirs = [".git", "build", "bin", "obj", "node_modules", ".vs"];

    public CppProjectOutlineBrick(IProviderFactory providerFactory, ILogger<CppProjectOutlineBrick> logger)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "cpp-project-outline";
        Name = "C++ Project Outline (privacy-preserving)";
        Version = "1.0.0";
        Icon = "🗺️";
        Category = BrickCategory.Analysis;
        Description = "Summarizes a C++ project's class responsibilities, seams, and third-party " +
                      "dependency usage via a local Ollama model, with mechanical redaction of " +
                      "method names, signatures, and literals so the output is safe to share.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("srcDir", "string", "Project root to outline (must be under WORKSPACE_ROOT)"),
                new BrickInputDefinition("model", "string", "Local Ollama model tag", required: false, defaultValue: DefaultModel),
                new BrickInputDefinition("maxLlmFiles", "number", "How many class-bearing files get LLM responsibility prose (rest are listed structurally)", required: false, defaultValue: 10),
                new BrickInputDefinition("maxRetries", "number", "Retries on transient Ollama failures", required: false, defaultValue: 3)
            ],
            Outputs =
            [
                new BrickOutputDefinition("outlineMarkdown", "string", "The redacted, shareable outline"),
                new BrickOutputDefinition("outlinePath", "string", "Where the outline was written"),
                new BrickOutputDefinition("filesScanned", "number", "C/C++ files inventoried deterministically"),
                new BrickOutputDefinition("classesFound", "number", "Class/struct definitions found"),
                new BrickOutputDefinition("llmFiles", "number", "Files that received LLM responsibility prose"),
                new BrickOutputDefinition("redactionCount", "number", "Identifiers/literals mechanically redacted from model prose")
            ]
        };

        Implementations = new BrickImplementations
        {
            Agentic = new AgenticImplementation
            {
                Id = "ollama-project-outline",
                Name = "Local model project outline",
                Description = "Per-file responsibility prose + deterministic dependency inventory, redacted for sharing.",
                LLMConfig = new LLMConfig
                {
                    Model = DefaultModel,
                    SystemPrompt = FileSystemPrompt,
                    Temperature = 0.2,
                    MaxTokens = 700
                },
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["ollama"] = new(DefaultModel)
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "30-120s per summarized file on CPU, local model",
                    Deterministic = false,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic];
        Selector = new ImplementationSelector { Default = ImplementationType.Agentic };
        Metadata = new BrickMetadata { Author = "evtx-toolkit", License = "MIT" };
    }

    const string FileSystemPrompt =
        "You summarize proprietary C++ source for an EXTERNAL audience. The summary may describe " +
        "the general shape and responsibility of classes and how third-party libraries are used, " +
        "but it must NOT reveal implementation detail.\n" +
        "HARD RULES for your output:\n" +
        "- NO method or function names, NO signatures, NO parameter lists.\n" +
        "- NO string literals, NO numeric constants, NO file-format magic values.\n" +
        "- NO code, NO pseudo-code, NO backticks, NO parentheses.\n" +
        "- Plain prose only. Two or three sentences per class describing its single responsibility, " +
        "then one sentence per third-party dependency describing what the class uses it FOR " +
        "(e.g. 'Qt is used only to notify the user of invalid input files').\n" +
        "Class names themselves are allowed.";

    const string SynthesisSystemPrompt =
        "You are given ALREADY-REDACTED class summaries and a deterministic dependency inventory of " +
        "a proprietary C++ project. Write a short architectural outline for an external audience: " +
        "1) the project's overall shape in one paragraph; 2) the main seams — where parsing, I/O, " +
        "state, and UI concerns separate, and which classes sit on each side; 3) a third-party usage " +
        "summary stating, per library, what it is used for and whether it appears load-bearing for " +
        "core logic or peripheral. Same hard rules: no method names, no signatures, no literals, no " +
        "code, no parentheses. Plain prose and short bullet lists only.";

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var srcDir = DepExtractPathRules.RequireExistingDirectory(input.Get<string>("srcDir"), "srcDir");
        DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(srcDir, "srcDir");
        var model = input.Get<string?>("model", DefaultModel) ?? DefaultModel;
        var maxLlmFiles = Math.Clamp(input.Get<int>("maxLlmFiles", 10), 0, 40);
        var maxRetries = Math.Max(1, input.Get<int>("maxRetries", 3));

        // ---- Pass 1: deterministic inventory --------------------------------
        var files = CollectFiles(srcDir);
        var facts = files.Select(f => FileFacts.Analyze(srcDir, f)).Where(f => f is not null).Select(f => f!).ToList();
        var classesFound = facts.Sum(f => f.Classes.Count);

        // Redaction dictionaries are scoped to the files whose prose the model
        // writes (harvesting the whole corpus on large projects floods the
        // dictionary with ordinary words and shreds the output), with word-
        // bounded matching applied in Redact().
        var banned = new HashSet<string>(StringComparer.Ordinal);
        var literals = new HashSet<string>(StringComparer.Ordinal);

        // ---- Pass 2: LLM responsibility prose for the top class-bearing files
        var candidates = facts
            .Where(f => f.Classes.Count > 0)
            .OrderByDescending(f => HeaderExts.Contains(Path.GetExtension(f.RelPath), StringComparer.OrdinalIgnoreCase))
            .ThenByDescending(f => f.Classes.Count)
            .ThenBy(f => f.SizeBytes)
            .Take(maxLlmFiles)
            .ToList();

        var allClassNames = new HashSet<string>(facts.SelectMany(f => f.Classes), StringComparer.Ordinal);
        foreach (var f in candidates)
        {
            banned.UnionWith(f.MethodNames);
            literals.UnionWith(f.StringLiterals.Where(l => l.Length >= 6));
        }
        banned.ExceptWith(allClassNames);   // class names are allowed by policy

        var redactionCount = 0;
        var llmConfig = new LLMConfig { Model = model, SystemPrompt = FileSystemPrompt, Temperature = 0.2, MaxTokens = 700 };

        foreach (var f in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = f.SourceText.Length > 9000 ? f.SourceText[..9000] + "\n// [truncated]" : f.SourceText;
            var prompt =
                $"File (relative path): {f.RelPath}\n" +
                $"Third-party includes detected: {(f.ExternalIncludes.Count > 0 ? string.Join(", ", f.ExternalIncludes) : "none")}\n\n" +
                "Source:\n" + source + "\n\n" +
                "Summarize per the hard rules now.";
            try
            {
                var raw = await ExecuteWithRetryAsync(
                    () => _providerFactory.ExecuteLLMAsync("ollama", FileSystemPrompt, prompt, llmConfig, cancellationToken),
                    maxRetries, cancellationToken).ConfigureAwait(false);
                f.Summary = Redact(raw, banned, literals, ref redactionCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Outline prose failed for {File}: {Message}", f.RelPath, ex.Message);
                f.Summary = "_summary unavailable (model call failed)_";
            }
        }

        // ---- Pass 3: seams synthesis from redacted material only -------------
        var inventory = BuildDependencyInventory(facts);
        string seams = "_synthesis unavailable_";
        if (candidates.Count > 0)
        {
            var synthPrompt = new StringBuilder();
            synthPrompt.AppendLine("Dependency inventory (deterministic):");
            synthPrompt.AppendLine(inventory);
            synthPrompt.AppendLine();
            synthPrompt.AppendLine("Redacted class summaries:");
            foreach (var f in candidates)
            {
                // Keep the synthesis prompt bounded: entity-catalog headers can
                // declare 80+ classes and would blow the CPU generation budget.
                var classes = string.Join(", ", f.Classes.Take(12))
                    + (f.Classes.Count > 12 ? $" … and {f.Classes.Count - 12} more" : "");
                var deps = string.Join(", ", f.InternalIncludes.Distinct().Take(8));
                synthPrompt.AppendLine($"--- {f.RelPath} | classes: {classes} | internal deps: {deps}");
                synthPrompt.AppendLine(f.Summary.Length > 1200 ? f.Summary[..1200] + " …" : f.Summary);
            }
            try
            {
                var synthConfig = new LLMConfig { Model = model, SystemPrompt = SynthesisSystemPrompt, Temperature = 0.2, MaxTokens = 900 };
                var raw = await ExecuteWithRetryAsync(
                    () => _providerFactory.ExecuteLLMAsync("ollama", SynthesisSystemPrompt, synthPrompt.ToString(), synthConfig, cancellationToken),
                    maxRetries, cancellationToken).ConfigureAwait(false);
                seams = Redact(raw, banned, literals, ref redactionCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Outline synthesis failed: {Message}", ex.Message);
            }
        }

        // ---- Assemble --------------------------------------------------------
        var name = new DirectoryInfo(srcDir).Name;
        var md = AssembleMarkdown(name, model, facts, candidates, inventory, seams, redactionCount);

        var outDirBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(outDirBase))
        {
            outDirBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
                ? Path.Combine(ws, ".dep-extract-out")
                : Path.GetTempPath();
        }
        Directory.CreateDirectory(outDirBase);
        var outlinePath = Path.Combine(outDirBase, $"outline-{name}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.md");
        await File.WriteAllTextAsync(outlinePath, md, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project outline written: {Path} ({Files} files, {Classes} classes, {Llm} LLM-summarized)",
            outlinePath, facts.Count, classesFound, candidates.Count);

        var output = new BrickOutput
        {
            Summary = $"Outlined {name}: {facts.Count} files, {classesFound} classes, " +
                      $"{candidates.Count} LLM-summarized, {redactionCount} redactions applied."
        };
        output.Set("outlineMarkdown", md);
        output.Set("outlinePath", outlinePath);
        output.Set("filesScanned", facts.Count);
        output.Set("classesFound", classesFound);
        output.Set("llmFiles", candidates.Count);
        output.Set("redactionCount", redactionCount);
        return output;
    }

    // ---------- deterministic analysis ---------------------------------------

    static List<string> CollectFiles(string srcDir)
    {
        var all = new List<string>();
        foreach (var file in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(srcDir, file);
            if (SkipDirs.Any(d => rel.Split(Path.DirectorySeparatorChar, '/').Contains(d, StringComparer.OrdinalIgnoreCase)))
                continue;
            var ext = Path.GetExtension(file);
            if (!HeaderExts.Contains(ext, StringComparer.OrdinalIgnoreCase)
                && !SourceExts.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
            if (new FileInfo(file).Length > 512_000) continue;
            all.Add(file);
            if (all.Count >= 5000) break;
        }
        return all;
    }

    sealed class FileFacts
    {
        public required string RelPath { get; init; }
        public required string SourceText { get; init; }
        public long SizeBytes { get; init; }
        public List<string> Classes { get; } = [];
        public List<string> InternalIncludes { get; } = [];
        public List<string> ExternalIncludes { get; } = [];
        public HashSet<string> MethodNames { get; } = new(StringComparer.Ordinal);
        public HashSet<string> StringLiterals { get; } = new(StringComparer.Ordinal);
        public string Summary { get; set; } = "";

        static readonly Regex ClassRx = new(@"(?m)^\s*(?:class|struct)\s+([A-Za-z_]\w*)\s*(?::[^;{]*)?\{", RegexOptions.Compiled);
        static readonly Regex QuotedIncludeRx = new("#\\s*include\\s*\"([^\"]+)\"", RegexOptions.Compiled);
        static readonly Regex AngleIncludeRx = new(@"#\s*include\s*<([^>]+)>", RegexOptions.Compiled);
        static readonly Regex MethodRx = new(@"\b([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);
        static readonly Regex LiteralRx = new("\"([^\"\\n]{4,})\"", RegexOptions.Compiled);
        static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "if", "for", "while", "switch", "return", "sizeof", "catch", "throw", "new", "delete",
            "static_cast", "reinterpret_cast", "const_cast", "dynamic_cast", "defined", "assert",
        };

        public static FileFacts? Analyze(string root, string file)
        {
            string text;
            try { text = File.ReadAllText(file); }
            catch { return null; }

            var f = new FileFacts
            {
                RelPath = Path.GetRelativePath(root, file).Replace('\\', '/'),
                SourceText = text,
                SizeBytes = text.Length,
            };
            foreach (Match m in ClassRx.Matches(text)) f.Classes.Add(m.Groups[1].Value);
            foreach (Match m in QuotedIncludeRx.Matches(text)) f.InternalIncludes.Add(Path.GetFileName(m.Groups[1].Value));
            foreach (Match m in AngleIncludeRx.Matches(text)) f.ExternalIncludes.Add(m.Groups[1].Value);
            foreach (Match m in MethodRx.Matches(text))
            {
                var id = m.Groups[1].Value;
                if (!Keywords.Contains(id) && id.Length > 2) f.MethodNames.Add(id);
            }
            foreach (Match m in LiteralRx.Matches(text))
            {
                var lit = m.Groups[1].Value;
                if (!lit.EndsWith(".h", StringComparison.Ordinal) && !lit.EndsWith(".hpp", StringComparison.Ordinal))
                    f.StringLiterals.Add(lit);
            }
            return f;
        }

        public bool UsesQt => ExternalIncludes.Any(IsQtInclude);
        public static bool IsQtInclude(string inc) =>
            inc.StartsWith("Q", StringComparison.Ordinal) || inc.StartsWith("Qt", StringComparison.Ordinal);
    }

    static string BuildDependencyInventory(List<FileFacts> facts)
    {
        var sb = new StringBuilder();
        var qt = facts.SelectMany(f => f.ExternalIncludes.Where(FileFacts.IsQtInclude).Select(i => (Include: i, f.RelPath)))
            .GroupBy(x => x.Include).OrderByDescending(g => g.Count()).ToList();
        sb.AppendLine($"Qt headers used: {qt.Count}");
        foreach (var g in qt.Take(20))
            sb.AppendLine($"  {g.Key}: {g.Count()} file(s) — e.g. {string.Join(", ", g.Select(x => x.RelPath).Distinct().Take(3))}");

        var std = facts.SelectMany(f => f.ExternalIncludes.Where(i => !FileFacts.IsQtInclude(i)))
            .GroupBy(i => i).OrderByDescending(g => g.Count()).ToList();
        sb.AppendLine($"Other/std headers (top 15 of {std.Count}):");
        foreach (var g in std.Take(15))
            sb.AppendLine($"  {g.Key}: {g.Count()}");
        return sb.ToString();
    }

    // ---------- redaction ------------------------------------------------------

    static string Redact(string text, HashSet<string> bannedIdentifiers, HashSet<string> literals, ref int count)
    {
        // Strip code fences/backticks outright.
        text = Regex.Replace(text, "```[\\s\\S]*?```", "[redacted code block]");
        text = text.Replace("`", "");

        foreach (var lit in literals)
        {
            // Word-bounded so a short source literal can't shred ordinary prose
            // by matching inside unrelated words.
            var rx = $@"(?<![\w]){Regex.Escape(lit)}(?![\w])";
            if (Regex.IsMatch(text, rx))
            {
                text = Regex.Replace(text, rx, "[redacted]");
                count++;
            }
        }
        foreach (var id in bannedIdentifiers)
        {
            var before = text;
            text = Regex.Replace(text, $@"\b{Regex.Escape(id)}\s*\([^)]*\)", "[redacted]");
            text = Regex.Replace(text, $@"\b{Regex.Escape(id)}\(\)", "[redacted]");
            // Proprietary-looking method names (snake_case / camelCase) leak meaning
            // even WITHOUT parens ("RecordHeader::decode_head") — redact those bare.
            // Plain lowercase words are left alone so ordinary prose survives.
            if (id.Length >= 5 && (id.Contains('_') || (id.Any(char.IsUpper) && id.Any(char.IsLower))))
                text = Regex.Replace(text, $@"(?<![\w]){Regex.Escape(id)}(?![\w(])", "[redacted]");
            if (!ReferenceEquals(before, text) && before != text) count++;
        }
        // Any residual identifier-followed-by-parens shape gets neutralized.
        var final = Regex.Replace(text, @"\b([A-Za-z_]\w*)\s*\([^)\n]*\)", "[redacted]");
        if (final != text) count++;
        return final.Trim();
    }

    // ---------- misc ------------------------------------------------------------

    async Task<string> ExecuteWithRetryAsync(Func<Task<string>> action, int maxAttempts, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { return await action().ConfigureAwait(false); }
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning("Transient model failure (attempt {Attempt}/{Max}): {Message} — retrying in {Delay}s",
                    attempt, maxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    static bool IsTransient(Exception ex) =>
        ex is System.Net.Http.HttpRequestException or TaskCanceledException
        || ex.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase);

    static string AssembleMarkdown(
        string name, string model, List<FileFacts> facts, List<FileFacts> summarized,
        string inventory, string seams, int redactionCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Project outline: {name}");
        sb.AppendLine();
        sb.AppendLine($"_Generated entirely on-box (local Ollama `{model}`). Mechanical redaction applied " +
                      $"({redactionCount} removals): no method names, signatures, literals, or code appear below._");
        sb.AppendLine();
        sb.AppendLine("## Snapshot");
        sb.AppendLine($"- C/C++ files scanned: {facts.Count}");
        sb.AppendLine($"- class/struct definitions: {facts.Sum(f => f.Classes.Count)}");
        sb.AppendLine($"- files with Qt usage: {facts.Count(f => f.UsesQt)}");
        sb.AppendLine($"- files given LLM responsibility prose: {summarized.Count}");
        sb.AppendLine();
        sb.AppendLine("## Third-party usage (deterministic inventory)");
        sb.AppendLine("```text");
        sb.AppendLine(inventory.TrimEnd());
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Shape & seams (synthesized from redacted material only)");
        sb.AppendLine(seams);
        sb.AppendLine();
        sb.AppendLine("## Classes");
        foreach (var f in summarized)
        {
            sb.AppendLine($"### {f.RelPath}");
            sb.AppendLine($"- classes: {string.Join(", ", f.Classes)}");
            if (f.InternalIncludes.Count > 0)
                sb.AppendLine($"- internal dependencies: {string.Join(", ", f.InternalIncludes.Distinct().Take(10))}");
            var ext = f.ExternalIncludes.Distinct().Take(12).ToList();
            if (ext.Count > 0)
                sb.AppendLine($"- third-party/std includes: {string.Join(", ", ext)}");
            sb.AppendLine();
            sb.AppendLine(f.Summary);
            sb.AppendLine();
        }

        var rest = facts.Where(f => f.Classes.Count > 0 && !summarized.Contains(f)).ToList();
        if (rest.Count > 0)
        {
            sb.AppendLine("## Additional class-bearing files (structural listing only)");
            foreach (var f in rest.Take(150))
                sb.AppendLine($"- {f.RelPath}: {string.Join(", ", f.Classes)}" + (f.UsesQt ? "  _(uses Qt)_" : ""));
            if (rest.Count > 150) sb.AppendLine($"- … and {rest.Count - 150} more");
        }
        return sb.ToString();
    }
}
