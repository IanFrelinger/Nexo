using System.Text;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// C++ profile prompt + grounding helpers extracted from the legacy brick.
/// Lives only in the DepExtract plugin (domain names stay out of Contracts).
/// </summary>
public static class CppAdapterPrompt
{
    /// <summary>System prompt for model-authored CustomEventReader drafts.</summary>
    public static string BuildSystemPrompt() => """
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
            explicit CustomEventReader(const std::filesystem::path& path) {
                // open `path` using the proprietary parser's real constructor/open call
            }

            void set_requested_fields(const std::vector<std::string>& names) override {
                // remember which named fields to decode per-record, if the format is non-flat
            }
            bool decodes_named_fields() const override { return true; }

            bool next(Event& out, size_t max_depth) override {
                // read ONE record using the proprietary parser's real "read next" call.
                // return false at end-of-stream.
            }

            bool     can_resume() const override { return true; }
            uint64_t position()   const override { return pos_; }
            void     resume_at(uint64_t off) override { /* seek */ }

        private:
            uint64_t pos_ = 0;
        };

        inline FileInfo inspect_custom(const std::filesystem::path& path) {
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

    /// <summary>Vocabulary belonging to the fixed IEventReader / processing.hpp contract.</summary>
    public static readonly HashSet<string> KnownContractVocabulary = new(StringComparer.Ordinal)
    {
        "IEventReader", "Event", "FileInfo", "reader_factory", "inspect_factory", "inspect_custom",
        "use_custom_reader", "next", "position", "resume_at", "can_resume", "set_requested_fields",
        "decodes_named_fields", "CustomEventReader",
        "read", "write", "resize", "data", "size", "clear", "find", "c_str", "substr", "push_back",
        "seekg", "tellg", "open", "close", "good", "eof", "make_unique", "make_shared", "move",
    };

    private static readonly HashSet<string> CommonCppKeywords = new(StringComparer.Ordinal)
    {
        "explicit", "static", "const", "void", "bool", "return", "override", "public", "private",
        "protected", "class", "struct", "sizeof", "reinterpret_cast", "static_cast", "if", "while", "for",
    };

    /// <summary>Bundles entry files under <paramref name="parserDir"/> into grounding text.</summary>
    public static string AssembleGrounding(string parserDir, IReadOnlyList<string> entryFiles)
    {
        if (string.IsNullOrWhiteSpace(parserDir))
            throw new ArgumentException("parserDir is required.", nameof(parserDir));
        if (entryFiles is null || entryFiles.Count == 0)
            throw new ArgumentException("entryFiles is required.", nameof(entryFiles));

        parserDir = Path.GetFullPath(parserDir);
        const long maxEntryBytes = 2_000_000;
        var sourceBundle = new StringBuilder();
        foreach (var f in entryFiles)
        {
            if (string.IsNullOrWhiteSpace(f) || f.Contains("..", StringComparison.Ordinal))
                throw new InvalidOperationException($"invalid entry file: {f}");
            var full = Path.Combine(parserDir, f.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(full);
            if (!info.Exists)
                throw new FileNotFoundException($"entry file not found: {f}", full);
            if (info.Length > maxEntryBytes)
                throw new InvalidOperationException($"entry file too large (>{maxEntryBytes} bytes): {f}");
            sourceBundle.AppendLine($"// ==== {f} ====");
            sourceBundle.AppendLine(File.ReadAllText(full));
            sourceBundle.AppendLine();
        }

        return sourceBundle.ToString();
    }

    /// <summary>Pulls Event/FileInfo structs from processing.hpp for exact field names.</summary>
    public static string? TryExtractContractStructs(string? pocoContext)
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

    /// <summary>Builds the user prompt for one draft/repair attempt.</summary>
    public static string BuildUserPrompt(
        string grounding,
        string? operatorGuidance,
        IReadOnlyList<string>? entryFiles,
        string? pocoContext,
        string? compileFeedback)
    {
        var guidanceBlock = string.IsNullOrWhiteSpace(operatorGuidance)
            ? ""
            : "\n\nApproved operator plan / feedback (honor these constraints):\n" + operatorGuidance.Trim() + "\n";

        var headerNames = (entryFiles ?? Array.Empty<string>())
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var includeReminder = headerNames.Length > 0
            ? "\nIMPORTANT: the type(s) declared in the source above (e.g. the proprietary parser class) are " +
              "NOT visible to your header unless you #include their file. Add " +
              string.Join(" and ", headerNames.Select(n => $"`#include \"{n}\"`")) +
              " near the top of your output, alongside `#include \"processing.hpp\"`.\n"
            : "";

        var contractStructs = TryExtractContractStructs(pocoContext);
        var contractBlock = contractStructs is null
            ? ""
            : "\nExact declarations of the Event/FileInfo types you must fill in above:\n\n```cpp\n"
              + contractStructs + "\n```\n";

        return
            "Proprietary parser source to adapt (this is the ONLY API you may call — do not invent methods):\n\n" +
            grounding +
            guidanceBlock +
            includeReminder +
            contractBlock +
            (string.IsNullOrWhiteSpace(compileFeedback) ? "" : "\n" + compileFeedback.Trim() + "\n") +
            "\nWrite the CustomEventReader implementation now. Output only the C++ header file content.";
    }

    /// <summary>Strips a markdown fence if the model wrapped its answer.</summary>
    public static string ExtractCodeBlock(string raw)
    {
        var match = Regex.Match(raw, "```(?:cpp|c\\+\\+|hpp)?\\s*\\r?\\n(.*?)```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.TrimEnd() + "\n" : raw.TrimEnd() + "\n";
    }

    /// <summary>True when the draft looks like a CustomEventReader / IEventReader adapter.</summary>
    public static bool LooksLikeCppAdapter(string code) =>
        code.Contains("CustomEventReader", StringComparison.Ordinal)
        || code.Contains("IEventReader", StringComparison.Ordinal)
        || code.Contains("use_custom_reader", StringComparison.Ordinal);

    /// <summary>Candidate class/method identifiers from proprietary source.</summary>
    public static HashSet<string> ExtractCandidateIdentifiers(string source)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(source, @"\b(?:class|struct)\s+(\w{3,})"))
            ids.Add(m.Groups[1].Value);
        foreach (Match m in Regex.Matches(source, @"\b(\w{4,})\s*\("))
            ids.Add(m.Groups[1].Value);
        ids.ExceptWith(CommonCppKeywords);
        return ids;
    }

    /// <summary>
    /// Method calls in the draft that appear nowhere in grounding and aren't
    /// known contract vocabulary — feeds <c>GenerativeProvenance.UnsupportedReferences</c>.
    /// </summary>
    public static string[] FindPossibleHallucinations(string code, string source)
    {
        var calls = Regex.Matches(code, @"[\w:]+[.\-][>]?(\w{3,})\s*\(").Select(m => m.Groups[1].Value)
            .Concat(Regex.Matches(code, @"fileproc::(\w+)").Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal);
        return calls
            .Where(c => !KnownContractVocabulary.Contains(c) && !source.Contains(c, StringComparison.Ordinal))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Source-API coverage in [0,1].</summary>
    public static double SourceApiCoverage(string code, string source)
    {
        var candidates = ExtractCandidateIdentifiers(source);
        if (candidates.Count == 0) return 1.0;
        var referenced = candidates.Count(id => code.Contains(id, StringComparison.Ordinal));
        return (double)referenced / candidates.Count;
    }
}
