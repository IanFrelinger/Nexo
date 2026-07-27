using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Drafts an adapter that wires an extracted proprietary C++ parser (the output
/// of <see cref="CppDependencyExtractorBrick"/>) into the evtx service's
/// IEventReader seam. This is inherently a code-understanding task — there is
/// no deterministic mapping from an arbitrary unknown parser's API surface to
/// next()/position()/resume_at() — so it has only an Agentic implementation,
/// routed to a LOCAL Ollama model (no cloud calls; the only traffic is a loopback
/// HTTP request to an Ollama daemon on localhost, never off-box) so the whole
/// extract-then-adapt pipeline stays air-gapped.
/// </summary>
public sealed class CppParserAdapterBrick : GenerativeBrick
{
    private static string DefaultModel =>
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5-coder:7b";

    /// <summary>
    /// Default source-API coverage below which a draft is flagged as a likely bare-template
    /// echo rather than a real integration. Overridable per call via the templateEchoThreshold input.
    /// </summary>
    private const double DefaultTemplateEchoThreshold = 0.15;

    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<CppParserAdapterBrick> _logger;

    public CppParserAdapterBrick(IProviderFactory providerFactory, ILogger<CppParserAdapterBrick> logger)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "cpp-parser-adapter";
        Name = "C++ Parser -> IEventReader Adapter";
        Version = "1.1.0";
        Icon = "🔌";
        Category = BrickCategory.Generation;
        Description = "Drafts a CustomEventReader implementation wiring an extracted proprietary " +
                      "C++ parser into the evtx IEventReader seam, using a local (Ollama) model — " +
                      "no cloud calls. Output is a starting draft for review, not a finished, " +
                      "guaranteed-correct implementation.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("parserDir", "string", "Directory containing the extracted parser (e.g. dep-extract's duplicate/ output)"),
                new BrickInputDefinition("entryFiles", "string[]", "Entry file path(s), relative to parserDir, to read for API discovery"),
                new BrickInputDefinition("outputPath", "string", "Absolute path to write the generated adapter .hpp to"),
                new BrickInputDefinition("model", "string", "Local Ollama model tag", required: false, defaultValue: DefaultModel),
                new BrickInputDefinition("maxRetries", "number", "Retries on transient Ollama failures (connection refused / unavailable during a local restart), with exponential backoff", required: false, defaultValue: 3),
                new BrickInputDefinition("maxCompileRetries", "number", "Compile-gate feedback loops when requireCompile is true (feeds g++ errors back into the model)", required: false, defaultValue: 3),
                new BrickInputDefinition("requireCompile", "bool", "When true, syntax-check the draft with g++ against Poco common/ + parserDir and retry with compiler errors (hard-fails if Docker/POCO unavailable)", required: false, defaultValue: false),
                new BrickInputDefinition("pocoContext", "string", "Poco/evtx root (must contain common/processing.hpp) for compile gate", required: false),
                new BrickInputDefinition("operatorGuidance", "string", "Approved plan notes / operator feedback to honor while drafting", required: false),
                new BrickInputDefinition("preferScaffold", "bool", "Try a deterministic scaffold before the model when the parser surface looks simple (also enabled via PREFER_SCAFFOLD=1)", required: false, defaultValue: false),
                new BrickInputDefinition("templateEchoThreshold", "number", "Source-API coverage (0-1) below which a draft is flagged as a likely bare-template echo rather than a real integration", required: false, defaultValue: DefaultTemplateEchoThreshold)
            ],
            Outputs =
            [
                new BrickOutputDefinition("adapterCode", "string", "The generated C++ source"),
                new BrickOutputDefinition("outputPath", "string", "Where the adapter was written"),
                new BrickOutputDefinition("rawModelResponse", "string", "Unprocessed model output, for auditing (absent on templated/scaffold path)"),
                new BrickOutputDefinition(ProvenanceOutputKey, "object", "GenerativeProvenance: strategy, verified, warnings, RequiresHumanReview"),
                new BrickOutputDefinition("generationStrategy", "string", "Deterministic | Templated | Model"),
                new BrickOutputDefinition("sourceApiCoverage", "number", "Fraction (0-1) of the extracted parser's own identifiers that the draft actually references — low values mean the model likely echoed the bare template instead of integrating"),
                new BrickOutputDefinition("looksLikeTemplateEcho", "bool", "True when sourceApiCoverage is low enough that this draft is probably not a real integration attempt"),
                new BrickOutputDefinition("possibleHallucinations", "string[]", "Method/constant names the draft calls that appear nowhere in the extracted source or the target contract — candidates for invented APIs; always review these before compiling"),
                new BrickOutputDefinition("compileOk", "bool?", "Compile-gate result: true/false when run, null when skipped"),
                new BrickOutputDefinition("compileLog", "string", "Compiler stdout/stderr or skip reason"),
                new BrickOutputDefinition("compileAttempts", "number", "How many compile-gate attempts were made"),
                new BrickOutputDefinition("qtShims", "object", "Map of leaf header name -> generated headless shim content for Qt headers the parser #includes (present only when any were shimmed)"),
                new BrickOutputDefinition("scaffoldUnverified", "bool", "True when a deterministic scaffold was returned without passing the compile gate — it makes structural assumptions and must be reviewed (present only on the scaffold path)")
            ]
        };

        Implementations = new BrickImplementations
        {
            Agentic = new AgenticImplementation
            {
                Id = "ollama-cpp-adapter",
                Name = "Local CodeLlama adapter draft",
                Description = "Reads the extracted parser's real API surface and drafts the IEventReader glue against it.",
                LLMConfig = new LLMConfig
                {
                    Model = DefaultModel,
                    SystemPrompt = BuildSystemPrompt(),
                    Temperature = 0.1,
                    MaxTokens = 4000
                },
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["ollama"] = new(DefaultModel)
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "10-60s on CPU, local model",
                    Deterministic = false,
                    RequiresNetwork = false,   // no off-box egress; the only call is a localhost loopback request to the local Ollama daemon (not truly network-free, but air-gapped)
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic];

        Selector = new ImplementationSelector
        {
            PreferAgentic = ["environment.airGapped"],   // the only path this brick has, but explicit for clarity
            Default = ImplementationType.Agentic
        };

        Metadata = new BrickMetadata { Author = "evtx-toolkit", License = "MIT" };
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (implementation is not (ImplementationType.Agentic or ImplementationType.Auto))
        {
            throw new ArgumentException(
                $"CppParserAdapterBrick has no Deterministic implementation — the mapping from an " +
                $"arbitrary parser's API to the IEventReader seam requires reasoning, not rules " +
                $"(requested: {implementation}).");
        }

        var parserDir = DepExtractPathRules.RequireExistingDirectory(input.Get<string>("parserDir"), "parserDir");
        var entryFiles = DepExtractPathRules.ResolveEntries(parserDir, input.Get<string[]>("entryFiles") ?? []);
        var outputPath = input.Get<string>("outputPath");
        if (string.IsNullOrWhiteSpace(outputPath))
            throw DepExtractException.InvalidInput("outputPath is required.");
        var model = input.Get<string?>("model", DefaultModel) ?? DefaultModel;
        if (string.IsNullOrWhiteSpace(model))
            throw DepExtractException.InvalidInput("model must not be empty.");
        var maxRetries = Math.Max(1, input.Get<int>("maxRetries", 3));
        var maxCompileRetries = Math.Max(1, input.Get<int>("maxCompileRetries", maxRetries));
        var requireCompile = input.Get<bool?>("requireCompile", false) ?? false;
        var pocoContext = input.Get<string?>("pocoContext", null)
            ?? Environment.GetEnvironmentVariable("POCO_CONTEXT");
        var preferScaffold = input.Get<bool?>("preferScaffold", false) ?? false
            || string.Equals(Environment.GetEnvironmentVariable("PREFER_SCAFFOLD"), "1", StringComparison.Ordinal);
        var operatorGuidance = input.Get<string?>("operatorGuidance", null);
        var templateEchoThreshold = input.Get<double>("templateEchoThreshold", DefaultTemplateEchoThreshold);
        var compileAttempts = 0;
        var scaffoldCompileAttempts = 0;

        const long maxEntryBytes = 2_000_000;
        var sourceBundle = new StringBuilder();
        foreach (var f in entryFiles)
        {
            var full = Path.Combine(parserDir, f);
            var info = new FileInfo(full);
            if (info.Length > maxEntryBytes)
                throw DepExtractException.InvalidInput($"entry file too large (>{maxEntryBytes} bytes): {f}");
            sourceBundle.AppendLine($"// ==== {f} ====");
            sourceBundle.AppendLine(await File.ReadAllTextAsync(full, cancellationToken).ConfigureAwait(false));
            sourceBundle.AppendLine();
        }

        var sourceTextEarly = sourceBundle.ToString();

        // Desktop parsers often include Qt purely for user-facing warnings; shim
        // those headers so drafts that #include the proprietary entry compile in
        // the Qt-less gate and Dockerfile.custom build. Companion headers in the
        // duplicate are scanned too (project-wide globals frequently pull in Qt).
        // Load-bearing Qt usage still fails the gate (only notification types
        // get real shims).
        // Qt shims are written into a dedicated scratch dir (never into parserDir, which is
        // operator-provided / extracted source we treat as read-only); the compile gate mounts
        // that dir as -I/shims. The dir is removed in the finally at the end of this method.
        var shimBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(shimBase))
            shimBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } shimWs
                ? Path.Combine(shimWs, ".dep-extract-out")
                : Path.GetTempPath();
        var shimDir = Path.Combine(shimBase, "qt-shims-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(shimDir);
        try
        {
        var qtShims = QtHeaderShims.EnsureForDirectory(sourceTextEarly, parserDir, shimDir, _logger);

        if (preferScaffold && AdapterScaffold.LooksSimple(sourceTextEarly))
        {
            // Keep nested entry paths (core/Foo.hpp) so #include matches structure-preserving install.
            var entryInclude = entryFiles.Length > 0
                ? entryFiles[0].Replace('\\', '/').TrimStart('/')
                : null;
            var scaffold = AdapterScaffold.TryBuild(sourceTextEarly, entryInclude);
            if (scaffold is not null)
            {
                AdapterCompileGate.Result? scCompile = null;
                if (requireCompile)
                {
                    scaffoldCompileAttempts = 1;
                    scCompile = await AdapterCompileGate.TryCompileAsync(
                            scaffold, pocoContext, parserDir, shimDir: shimDir, requireSuccess: true,
                            logger: _logger, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                // Use scaffold when compile ok, or when compile not required.
                if (!requireCompile || scCompile is { Ok: true })
                {
                    var outDirSc = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outDirSc)) Directory.CreateDirectory(outDirSc);
                    await File.WriteAllTextAsync(outputPath, scaffold, cancellationToken).ConfigureAwait(false);
                    var candidatesSc = ExtractCandidateIdentifiers(sourceTextEarly);
                    var referencedSc = candidatesSc.Where(id => scaffold.Contains(id, StringComparison.Ordinal)).ToArray();
                    var coverageSc = candidatesSc.Count > 0 ? (double)referencedSc.Length / candidatesSc.Count : 1.0;
                    var echoSc = coverageSc < templateEchoThreshold;

                    // A scaffold that hasn't cleared the compile gate is UNVERIFIED: it's a
                    // deterministic template with baked-in structural assumptions (the pull method
                    // takes a `long&` out-param, file-backed timestamps are millisecond ticks, and
                    // t_end is opened to a 1e300 sentinel). Surface those as warnings and a
                    // scaffoldUnverified marker so callers never read "deterministic" as "known-correct".
                    var scaffoldVerified = scCompile is { Ok: true };
                    var scWarnings = new List<string>();
                    if (!scaffoldVerified)
                        scWarnings.Add(
                            "unverified scaffold — not checked against the real parser; assumes a `long&`-out pull " +
                            "method, millisecond timestamps, and a 1e300 t_end sentinel. Enable requireCompile or " +
                            "review before use");
                    if (echoSc)
                        scWarnings.Add($"low source coverage ({coverageSc:P0}) — scaffold may not reference the real API");

                    var outputSc = new BrickOutput
                    {
                        Summary = $"Scaffolded a {scaffold.Split('\n').Length}-line adapter at {outputPath}" +
                                  (scWarnings.Count > 0
                                      ? $" — WARNING: {string.Join("; ", scWarnings)}"
                                      : " (deterministic, compile-verified; review before use).")
                    };
                    outputSc.Set("adapterCode", scaffold);
                    outputSc.Set("outputPath", outputPath);
                    outputSc.Set("sourceApiCoverage", coverageSc);
                    outputSc.Set("looksLikeTemplateEcho", echoSc);
                    outputSc.Set("possibleHallucinations", Array.Empty<string>());
                    outputSc.Set("scaffoldUnverified", !scaffoldVerified);
                    if (scCompile?.Ok is { } okSc) outputSc.Set("compileOk", okSc);
                    outputSc.Set("compileLog", scCompile?.Log ?? "compile gate not requested");
                    outputSc.Set("compileAttempts", scaffoldCompileAttempts);
                    if (qtShims.Count > 0) outputSc.Set("qtShims", qtShims);
                    var scaffoldProvenance = ApplyAuditPolicy(
                        GenerativeProvenance.Create(
                            GenerationStrategy.Templated,
                            verified: scaffoldVerified,
                            confidence: scaffoldVerified ? 0.7 : 0.3,
                            warnings: scWarnings,
                            assumptions: new[]
                            {
                                "pull method takes a long& out-param",
                                "file-backed timestamps are millisecond ticks",
                                "t_end opened to a 1e300 sentinel"
                            },
                            requiresHumanReview: !scaffoldVerified || echoSc),
                        context);
                    EmitProvenance(outputSc, scaffoldProvenance);
                    if (scWarnings.Count > 0)
                        _logger.LogWarning("Deterministic scaffold for {OutputPath} is unverified: {Warnings}", outputPath, string.Join("; ", scWarnings));
                    else
                        _logger.LogInformation("Used compile-verified deterministic scaffold for {OutputPath}", outputPath);
                    return outputSc;
                }
                scaffoldCompileAttempts = Math.Max(scaffoldCompileAttempts, 1);
                _logger.LogWarning("Scaffold failed compile gate; falling back to model. {Log}", scCompile?.Log);
                // Don't mask a compile miss behind OLLAMA_UNREACHABLE when model use is disabled.
                if (string.Equals(Environment.GetEnvironmentVariable("SKIP_OLLAMA"), "1", StringComparison.Ordinal))
                {
                    throw DepExtractException.CompileFailed(
                        "Scaffold draft failed compile gate and model fallback is disabled (SKIP_OLLAMA=1):\n" +
                        (scCompile?.Log ?? "no compile log"));
                }
            }
        }

        var systemPrompt = BuildSystemPrompt();
        var guidanceBlock = string.IsNullOrWhiteSpace(operatorGuidance)
            ? ""
            : "\n\nApproved operator plan / feedback (honor these constraints):\n" + operatorGuidance.Trim() + "\n";
        var headerNames = entryFiles.Select(Path.GetFileName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal).ToArray();
        var includeReminder = headerNames.Length > 0
            ? "\nIMPORTANT: the type(s) declared in the source above (e.g. the proprietary parser class) are " +
              "NOT visible to your header unless you #include their file. Add " +
              string.Join(" and ", headerNames.Select(n => $"`#include \"{n}\"`")) +
              " near the top of your output, alongside `#include \"processing.hpp\"` — otherwise your adapter " +
              "will fail to compile with \"was not declared in this scope\".\n"
            : "";
        var contractStructs = TryExtractContractStructs(pocoContext);
        var contractBlock = contractStructs is null
            ? ""
            : "\nExact declarations of the Event/FileInfo types you must fill in above (use these EXACT field " +
              "names, order, and types — do not guess or invent field names, and use every field the same " +
              "way an aggregate initializer would need them, in this order):\n\n```cpp\n" + contractStructs + "\n```\n";
        var userPrompt =
            "Proprietary parser source to adapt (this is the ONLY API you may call — do not invent methods):\n\n" +
            sourceBundle +
            guidanceBlock +
            includeReminder +
            contractBlock +
            "\nWrite the CustomEventReader implementation now. Output only the C++ header file content.";

        var llmConfig = new LLMConfig
        {
            Model = model,
            SystemPrompt = systemPrompt,
            Temperature = 0.1,
            MaxTokens = 4000
        };

        _logger.LogInformation("Drafting adapter for {Count} entry file(s) via local model {Model}", entryFiles.Length, model);

        string? code = null;
        string? raw = null;
        AdapterCompileGate.Result? compile = null;
        var compileFeedback = "";

        for (var attempt = 1; attempt <= maxCompileRetries; attempt++)
        {
            var prompt = userPrompt + compileFeedback;
            raw = await ExecuteWithRetryAsync(
                () => _providerFactory.ExecuteLLMAsync("ollama", systemPrompt, prompt, llmConfig, cancellationToken),
                maxRetries, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(raw))
                throw DepExtractException.EmptyModelResponse();

            code = ExtractCodeBlock(raw);
            if (string.IsNullOrWhiteSpace(code) || !LooksLikeCppAdapter(code))
                throw DepExtractException.AdaptFailed(
                    "The local model response did not contain a usable C++ adapter (expected CustomEventReader / IEventReader).");

            if (!requireCompile)
            {
                compile = new AdapterCompileGate.Result(null, "compile gate not requested", Skipped: true);
                break;
            }

            compileAttempts = scaffoldCompileAttempts + attempt;
            compile = await AdapterCompileGate.TryCompileAsync(
                    code, pocoContext, parserDir, shimDir: shimDir, requireSuccess: true,
                    logger: _logger, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (compile.Ok == true)
                break;

            if (attempt == maxCompileRetries)
            {
                throw DepExtractException.CompileFailed(
                    $"Adapter draft failed syntax check after {maxCompileRetries} attempt(s):\n{compile.Log}");
            }

            _logger.LogWarning(
                "Compile gate failed (attempt {Attempt}/{Max}); feeding errors back to model",
                attempt, maxCompileRetries);
            compileFeedback =
                "\n\nYour previous draft failed to compile. Fix these errors and output a corrected full header only:\n" +
                compile.Log + "\n";
        }

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(outputPath, code!, cancellationToken).ConfigureAwait(false);

        var sourceText = sourceBundle.ToString();
        var candidates = ExtractCandidateIdentifiers(sourceText);
        var referenced = candidates.Where(id => code!.Contains(id, StringComparison.Ordinal)).ToArray();
        var coverage = candidates.Count > 0 ? (double)referenced.Length / candidates.Count : 1.0;
        var looksLikeTemplateEcho = coverage < templateEchoThreshold;
        var hallucinations = FindPossibleHallucinations(code!, sourceText);

        var warnings = new List<string>();
        if (looksLikeTemplateEcho)
            warnings.Add($"low source coverage ({coverage:P0}) — this looks like the bare template, not a real integration");
        if (hallucinations.Length > 0)
            warnings.Add($"{hallucinations.Length} possibly-invented symbol(s): {string.Join(", ", hallucinations)}");
        if (compile is { Ok: false })
            warnings.Add("compile gate failed");

        var output = new BrickOutput
        {
            Summary = $"Drafted a {code!.Split('\n').Length}-line adapter at {outputPath}" +
                      (warnings.Count > 0 ? $" — WARNING: {string.Join("; ", warnings)}" : " (local model draft — review before compiling into the seam).")
        };
        output.Set("adapterCode", code);
        output.Set("outputPath", outputPath);
        output.Set("rawModelResponse", raw!);
        output.Set("sourceApiCoverage", coverage);
        output.Set("looksLikeTemplateEcho", looksLikeTemplateEcho);
        output.Set("possibleHallucinations", hallucinations);
        if (compile?.Ok is { } ok)
            output.Set("compileOk", ok);
        output.Set("compileLog", compile?.Log ?? "");
        output.Set("compileAttempts", compileAttempts);
        if (qtShims.Count > 0) output.Set("qtShims", qtShims);
        var modelProvenance = ApplyAuditPolicy(
            GenerativeProvenance.Create(
                GenerationStrategy.Model,
                verified: compile?.Ok == true,
                confidence: looksLikeTemplateEcho ? 0.2 : Math.Clamp(coverage, 0, 1),
                warnings: warnings,
                unsupportedReferences: hallucinations,
                requiresHumanReview: true),
            context);
        EmitProvenance(output, modelProvenance);
        if (warnings.Count > 0)
            _logger.LogWarning("Adapter draft for {OutputPath} has quality warnings: {Warnings}", outputPath, string.Join("; ", warnings));
        return output;
        }
        finally
        {
            try { Directory.Delete(shimDir, recursive: true); } catch { /* best-effort scratch cleanup */ }
        }
    }

    /// <summary>Retries transient failures (Ollama unreachable/restarting) with exponential backoff (2s, 4s, 8s, ...). Non-transient errors (bad input, etc.) propagate immediately since they won't be fixed by retrying.</summary>
    private async Task<string> ExecuteWithRetryAsync(Func<Task<string>> action, int maxAttempts, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex, cancellationToken))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Ollama call failed (attempt {Attempt}/{Max}); retrying in {DelaySeconds}s", attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception ex, CancellationToken cancellationToken)
    {
        // Caller cancellation must not be retried — only transport/timeouts that aren't ours.
        if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            return false;
        return ex is ModelUnavailableException or HttpRequestException or TaskCanceledException or IOException;
    }

    /// <summary>
    /// Pulls the verbatim `struct Event { ... };` and `struct FileInfo { ... };` declarations out of
    /// processing.hpp so the model gets exact field names/order/types instead of guessing them from the
    /// target contract's prose comments — a guessed aggregate initializer is a common compile-gate failure.
    /// </summary>
    private static string? TryExtractContractStructs(string? pocoContext)
    {
        if (string.IsNullOrWhiteSpace(pocoContext)) return null;
        var path = Path.Combine(pocoContext, "common", "processing.hpp");
        if (!File.Exists(path)) return null;
        try
        {
            var text = File.ReadAllText(path);
            var evt = Regex.Match(text, @"struct\s+Event\s*\{.*?\};", RegexOptions.Singleline);
            var info = Regex.Match(text, @"struct\s+FileInfo\s*\{.*?\};", RegexOptions.Singleline);
            if (!evt.Success && !info.Success) return null;
            var sb = new StringBuilder();
            if (evt.Success) sb.AppendLine(evt.Value);
            if (info.Success) sb.AppendLine(info.Value);
            return sb.ToString().TrimEnd();
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool LooksLikeCppAdapter(string code) =>
        code.Contains("CustomEventReader", StringComparison.Ordinal)
        || code.Contains("IEventReader", StringComparison.Ordinal)
        || code.Contains("use_custom_reader", StringComparison.Ordinal);

    /// <summary>Class/struct names and function/method-looking identifiers found in the extracted source — the vocabulary a genuine integration should draw from.</summary>
    private static HashSet<string> ExtractCandidateIdentifiers(string source)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(source, @"\b(?:class|struct)\s+(\w{3,})")) ids.Add(m.Groups[1].Value);
        foreach (Match m in Regex.Matches(source, @"\b(\w{4,})\s*\(")) ids.Add(m.Groups[1].Value);
        ids.ExceptWith(CommonCppKeywords);
        return ids;
    }

    /// <summary>
    /// Method calls (`.name(`/`->name(`) and fileproc::name references in the draft that appear
    /// nowhere in the extracted source and aren't part of the fixed target contract's own
    /// vocabulary — candidate invented APIs a human should check before compiling.
    /// </summary>
    private static string[] FindPossibleHallucinations(string code, string source)
    {
        var calls = Regex.Matches(code, @"[\w:]+[.\-][>]?(\w{3,})\s*\(").Select(m => m.Groups[1].Value)
            .Concat(Regex.Matches(code, @"fileproc::(\w+)").Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal);
        return calls
            .Where(c => !KnownContractVocabulary.Contains(c) && !source.Contains(c, StringComparison.Ordinal))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    private static readonly HashSet<string> KnownContractVocabulary = new(StringComparer.Ordinal)
    {
        "IEventReader", "Event", "FileInfo", "reader_factory", "inspect_factory", "inspect_custom",
        "use_custom_reader", "next", "position", "resume_at", "can_resume", "set_requested_fields",
        "decodes_named_fields", "CustomEventReader",
        // std/stdlib surface the model is allowed to use freely
        "read", "write", "resize", "data", "size", "clear", "find", "c_str", "substr", "push_back",
        "seekg", "tellg", "open", "close", "good", "eof", "make_unique", "make_shared", "move",
    };

    private static readonly HashSet<string> CommonCppKeywords = new(StringComparer.Ordinal)
    {
        "explicit", "static", "const", "void", "bool", "return", "override", "public", "private",
        "protected", "class", "struct", "sizeof", "reinterpret_cast", "static_cast", "if", "while", "for",
    };

    /// <summary>Strips a ```cpp / ``` markdown fence if the model wrapped its answer in one.</summary>
    private static string ExtractCodeBlock(string raw)
    {
        var match = Regex.Match(raw, "```(?:cpp|c\\+\\+|hpp)?\\s*\\r?\\n(.*?)```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.TrimEnd() + "\n" : raw.TrimEnd() + "\n";
    }

    private static string BuildSystemPrompt() => """
        You adapt proprietary C++ event-log parsers into a specific interface used by the evtx
        extraction service. You will be given the source of a proprietary parser. You must produce
        a single C++ header implementing IEventReader by wiring calls to the proprietary parser's
        REAL methods — never invent methods, fields, or types that were not present in the given
        source. If you are not confident how to map something, write a `// UNCERTAIN:` comment
        there instead of guessing.

        Target contract (fill in the TODOs; keep the class/method names and signatures exactly as
        shown; this is the ONLY shape the caller will use). The proprietary parser's own type(s)
        (e.g. its main class) are declared in the source you're given below, NOT in processing.hpp —
        you MUST add your own `#include "<TheProprietaryHeader.hpp>"` line for each proprietary
        header you reference, or the compiler will not know those types exist:

        ```cpp
        #pragma once
        #include "processing.hpp"
        #include "TheProprietaryParsersHeader.hpp"  // <- replace with the ACTUAL filename from the source given below
        #include <memory>

        namespace fileproc {

        class CustomEventReader : public IEventReader {
        public:
            // If the proprietary parser's class has NO default constructor (most don't — they take a
            // path/string), you MUST construct it in the MEMBER-INITIALIZER LIST, e.g.:
            //   explicit CustomEventReader(const std::filesystem::path& path) : parser_(path.string()) { }
            // NOT as a bare `Parser parser_;` member assigned inside the body — that default-constructs
            // it first and fails to compile if there is no default constructor.
            explicit CustomEventReader(const std::filesystem::path& path) {
                // open `path` using the proprietary parser's real constructor/open call
            }

            void set_requested_fields(const std::vector<std::string>& names) override {
                // remember which named fields to decode per-record, if the format is non-flat
            }
            bool decodes_named_fields() const override { return true; } // false = flat out.data + offsets instead

            bool next(Event& out, size_t max_depth) override {
                // read ONE record using the proprietary parser's real "read next" call.
                // out.t = timestamp (non-decreasing); out.type = type code;
                // out.data = raw payload bytes (flat formats) OR out.fields[name] = value (non-flat).
                // return false at end-of-stream.
                //
                // `out` is REUSED across calls by the caller (not freshly constructed each time) — on
                // every path that returns true, you MUST explicitly (re)assign every field you own,
                // including the empty/default case (e.g. `out.data.clear();` for a zero-length payload).
                // Never `return true` early without touching out.data/out.fields — a leftover value from
                // the PREVIOUS call will silently leak into this record.
                //
                // NEVER invent a byte layout that is not documented in the source you were given (e.g.
                // "assume the type is in the first byte of the payload") — that is a guess, not a fact,
                // and indexing into a payload buffer without checking it is non-empty first will CRASH
                // (out-of-bounds access) on any empty/short record. If the source doesn't give you a
                // real way to get a field's value, leave it at a safe default (0 / empty) — a safe
                // default is always better than a fabricated read that can crash.
            }

            bool     can_resume() const override { return true; }   // false if the format can't seek
            uint64_t position()   const override { return pos_; }   // byte/record offset of the NEXT record
            void     resume_at(uint64_t off) override { /* seek using the proprietary parser's real seek call */ }

        private:
            uint64_t pos_ = 0;
            // real stream/handle members from the proprietary parser go here — if the proprietary
            // type has no default constructor, initialize it in the constructor's initializer list
            // above (": member_(ctor_args)"), never as a bare declaration assigned in the body.
        };

        inline FileInfo inspect_custom(const std::filesystem::path& path) {
            // open the file just enough to report header metadata, using the proprietary parser's
            // real API: FileInfo fi; fi.event_count=...; fi.t_begin=...; fi.t_end=...;
            // fi.bytes=std::filesystem::file_size(path); fi.resumable=true; fi.types={"...", ...};
            //
            // If the proprietary API's read/peek call takes an OUT-PARAMETER (a `bool read(T& out)`
            // style method, not a return value), you MUST declare a real local variable and pass that
            // by reference — e.g. `T rec; parser.readRecord(rec);` — NEVER pass `nullptr` for it and
            // NEVER assign its (void) return to a field expecting a value.
            //
            // DO NOT hardcode fi.event_count/t_begin/t_end to 0, 1, or a single peeked record's value —
            // that is WRONG for any file with more than one record and will silently corrupt file
            // metadata. fi.t_end in particular is the LAST record's timestamp, never the first — do NOT
            // write `fi.t_end = <first record>` with an "assuming all timestamps are the same" comment;
            // that is only true by luck. This function is allowed to read the ENTIRE stream if that's
            // what it takes to find the true last record — it does not need to be O(1), and a slow-but-
            // correct t_end beats a fast-but-wrong one. Loop with the same read call `next()` uses,
            // keeping the LAST record's timestamp as you go, until end-of-stream. If the proprietary API
            // exposes a real count getter (e.g. declaredCount()), prefer that for event_count over
            // counting yourself — but still get t_end by scanning to the real last record.
            return FileInfo{};
        }

        inline void use_custom_reader(){
            reader_factory() = [](const std::filesystem::path& p){
                return std::unique_ptr<IEventReader>(new CustomEventReader(p));
            };
            inspect_factory() = inspect_custom;
        }

        } // namespace fileproc
        ```
        """;
}
