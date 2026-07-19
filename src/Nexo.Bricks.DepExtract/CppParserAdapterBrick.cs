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
/// routed to a LOCAL Ollama model (no cloud calls, no network egress) so the
/// whole extract-then-adapt pipeline stays air-gapped.
/// </summary>
public sealed class CppParserAdapterBrick : DomainBrick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<CppParserAdapterBrick> _logger;

    public CppParserAdapterBrick(IProviderFactory providerFactory, ILogger<CppParserAdapterBrick> logger)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "cpp-parser-adapter";
        Name = "C++ Parser -> IEventReader Adapter";
        Version = "1.0.0";
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
                new BrickInputDefinition("model", "string", "Local Ollama model tag", required: false, defaultValue: "codellama:7b"),
                new BrickInputDefinition("maxRetries", "number", "Retries on transient Ollama failures (connection refused / unavailable during a local restart), with exponential backoff", required: false, defaultValue: 3)
            ],
            Outputs =
            [
                new BrickOutputDefinition("adapterCode", "string", "The generated C++ source"),
                new BrickOutputDefinition("outputPath", "string", "Where the adapter was written"),
                new BrickOutputDefinition("rawModelResponse", "string", "Unprocessed model output, for auditing"),
                new BrickOutputDefinition("sourceApiCoverage", "number", "Fraction (0-1) of the extracted parser's own identifiers that the draft actually references — low values mean the model likely echoed the bare template instead of integrating"),
                new BrickOutputDefinition("looksLikeTemplateEcho", "bool", "True when sourceApiCoverage is low enough that this draft is probably not a real integration attempt"),
                new BrickOutputDefinition("possibleHallucinations", "string[]", "Method/constant names the draft calls that appear nowhere in the extracted source or the target contract — candidates for invented APIs; always review these before compiling")
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
                    Model = "codellama:7b",
                    SystemPrompt = BuildSystemPrompt(),
                    Temperature = 0.1,
                    MaxTokens = 4000
                },
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["ollama"] = new("codellama:7b")
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "10-60s on CPU, local model",
                    Deterministic = false,
                    RequiresNetwork = false,   // local Ollama only — this is the air-gapped path, not a cloud one
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

        var parserDir = input.Get<string>("parserDir");
        var entryFiles = input.Get<string[]>("entryFiles");
        var outputPath = input.Get<string>("outputPath");
        var model = input.Get<string?>("model", "codellama:7b") ?? "codellama:7b";
        var maxRetries = Math.Max(1, input.Get<int>("maxRetries", 3));

        if (entryFiles.Length == 0)
            throw new ArgumentException("At least one entry file is required.");
        if (!Directory.Exists(parserDir))
            throw new DirectoryNotFoundException($"parserDir not found: {parserDir}");

        var sourceBundle = new StringBuilder();
        foreach (var f in entryFiles)
        {
            var full = Path.Combine(parserDir, f);
            if (!File.Exists(full))
                throw new FileNotFoundException($"entry file not found under parserDir: {f}", full);
            sourceBundle.AppendLine($"// ==== {f} ====");
            sourceBundle.AppendLine(File.ReadAllText(full));
            sourceBundle.AppendLine();
        }

        var systemPrompt = BuildSystemPrompt();
        var userPrompt =
            "Proprietary parser source to adapt (this is the ONLY API you may call — do not invent methods):\n\n" +
            sourceBundle +
            "\nWrite the CustomEventReader implementation now. Output only the C++ header file content.";

        var llmConfig = new LLMConfig
        {
            Model = model,
            SystemPrompt = systemPrompt,
            Temperature = 0.1,
            MaxTokens = 4000
        };

        _logger.LogInformation("Drafting adapter for {Count} entry file(s) via local model {Model}", entryFiles.Length, model);
        var raw = await ExecuteWithRetryAsync(
            () => _providerFactory.ExecuteLLMAsync("ollama", systemPrompt, userPrompt, llmConfig, cancellationToken),
            maxRetries, cancellationToken).ConfigureAwait(false);

        var code = ExtractCodeBlock(raw);

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(outputPath, code, cancellationToken).ConfigureAwait(false);

        var sourceText = sourceBundle.ToString();
        var candidates = ExtractCandidateIdentifiers(sourceText);
        var referenced = candidates.Where(id => code.Contains(id, StringComparison.Ordinal)).ToArray();
        var coverage = candidates.Count > 0 ? (double)referenced.Length / candidates.Count : 1.0;
        var looksLikeTemplateEcho = coverage < 0.15;
        var hallucinations = FindPossibleHallucinations(code, sourceText);

        var warnings = new List<string>();
        if (looksLikeTemplateEcho)
            warnings.Add($"low source coverage ({coverage:P0}) — this looks like the bare template, not a real integration");
        if (hallucinations.Length > 0)
            warnings.Add($"{hallucinations.Length} possibly-invented symbol(s): {string.Join(", ", hallucinations)}");

        var output = new BrickOutput
        {
            Summary = $"Drafted a {code.Split('\n').Length}-line adapter at {outputPath}" +
                      (warnings.Count > 0 ? $" — WARNING: {string.Join("; ", warnings)}" : " (local model draft — review before compiling into the seam).")
        };
        output.Set("adapterCode", code);
        output.Set("outputPath", outputPath);
        output.Set("rawModelResponse", raw);
        output.Set("sourceApiCoverage", coverage);
        output.Set("looksLikeTemplateEcho", looksLikeTemplateEcho);
        output.Set("possibleHallucinations", hallucinations);
        if (warnings.Count > 0)
            _logger.LogWarning("Adapter draft for {OutputPath} has quality warnings: {Warnings}", outputPath, string.Join("; ", warnings));
        return output;
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
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Ollama call failed (attempt {Attempt}/{Max}); retrying in {DelaySeconds}s", attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception ex) =>
        ex is ModelUnavailableException or HttpRequestException or TaskCanceledException or IOException;

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
        shown; this is the ONLY shape the caller will use):

        ```cpp
        #pragma once
        #include "processing.hpp"
        #include <memory>

        namespace fileproc {

        class CustomEventReader : public IEventReader {
        public:
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
            }

            bool     can_resume() const override { return true; }   // false if the format can't seek
            uint64_t position()   const override { return pos_; }   // byte/record offset of the NEXT record
            void     resume_at(uint64_t off) override { /* seek using the proprietary parser's real seek call */ }

        private:
            uint64_t pos_ = 0;
            // real stream/handle members from the proprietary parser go here
        };

        inline FileInfo inspect_custom(const std::filesystem::path& path) {
            // open the file just enough to report header metadata, using the proprietary parser's
            // real API: FileInfo fi; fi.event_count=...; fi.t_begin=...; fi.t_end=...;
            // fi.bytes=std::filesystem::file_size(path); fi.resumable=true; fi.types={"...", ...};
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
