using System.Text;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Deterministic adapter drafts for simple parser surfaces (few classes, obvious pull APIs).
/// Used before Ollama when the source looks scaffoldable; still a starting draft for review.
/// </summary>
public static class AdapterScaffold
{
    public sealed record ClassApi(string Name, string[] Methods);

    public static bool LooksSimple(string sourceText)
    {
        var classes = ExtractClasses(sourceText);
        if (classes.Count is < 1 or > 2) return false;
        var methods = classes.SelectMany(c => c.Methods).ToArray();
        // Accessors/metadata don't count against the budget — real complexity is
        // the pull surface plus a handful of helpers (was hard-capped at 8).
        var substantive = methods.Where(m => !IsMetadataAccessor(m)).ToArray();
        if (substantive.Length is < 1 or > 12) return false;
        // Prefer surfaces that look like pull/advance/next rather than deep hierarchies.
        return methods.Any(m => m is "advance" or "next" or "nextEvent" or "read" or "pull" or "readNext");
    }

    static bool IsMetadataAccessor(string method) =>
        method is "last_type" or "lastType" or "event_count" or "eventCount"
            or "t_begin" or "t_end" or "tBegin" or "tEnd"
            or "position" or "can_resume" or "canResume"
            or "size" or "count" or "empty" or "good" or "eof";

    /// <param name="entryIncludeName">Header to #include — prefer the path relative to the
    /// extract root (e.g. <c>core/NestedStream.hpp</c>) so nested layouts resolve under
    /// <c>-Icommon</c>; basename-only still works when the install flattens.</param>
    public static string? TryBuild(string sourceText, string? entryIncludeName = null)
    {
        if (!LooksSimple(sourceText)) return null;
        var primary = ExtractClasses(sourceText)[0];
        var pull = primary.Methods.FirstOrDefault(m => m is "advance" or "next" or "nextEvent" or "read" or "pull" or "readNext")
            ?? primary.Methods[0];

        var pathCtor = Regex.IsMatch(
            sourceText,
            $@"\b{Regex.Escape(primary.Name)}\s*\(\s*const\s+(?:std::)?(?:filesystem::path|string)\s*&",
            RegexOptions.CultureInvariant);

        var hasLastType = primary.Methods.Any(m => m is "last_type" or "lastType");
        string? includeName = null;
        if (!string.IsNullOrWhiteSpace(entryIncludeName))
        {
            includeName = entryIncludeName.Replace('\\', '/').TrimStart('/');
            if (includeName.Contains("..", StringComparison.Ordinal))
                includeName = includeName.Split('/').Last();
        }

        var sb = new StringBuilder();
        sb.AppendLine("#pragma once");
        sb.AppendLine("// Deterministic scaffold from DepExtract (review before production use).");
        sb.AppendLine("#include \"processing.hpp\"");
        if (includeName is not null)
            sb.AppendLine($"#include \"{includeName}\"");
        sb.AppendLine("#include <memory>");
        sb.AppendLine();
        sb.AppendLine("namespace fileproc {");
        sb.AppendLine();
        sb.AppendLine("class CustomEventReader : public IEventReader {");
        sb.AppendLine("public:");
        sb.AppendLine("    explicit CustomEventReader(const std::filesystem::path& path) {");
        if (pathCtor)
        {
            sb.AppendLine($"        parser_ = std::make_unique<{primary.Name}>(path);");
        }
        else
        {
            sb.AppendLine("        // UNCERTAIN: wire ctor args from path / open proprietary handle");
            sb.AppendLine($"        (void)path; parser_ = std::make_unique<{primary.Name}>(0);");
        }
        sb.AppendLine("    }");
        sb.AppendLine("    void set_requested_fields(const std::vector<std::string>& names) override { (void)names; }");
        sb.AppendLine("    bool decodes_named_fields() const override { return true; }");
        sb.AppendLine("    bool next(Event& out, size_t max_depth) override {");
        sb.AppendLine("        (void)max_depth;");
        sb.AppendLine("        long v = 0;");
        sb.AppendLine($"        if (!parser_ || !parser_->{pull}(v)) return false;");
        if (pathCtor)
        {
            // File-backed streams often expose millisecond ticks via advance(long&).
            sb.AppendLine("        out.t = static_cast<double>(v) / 1000.0;");
        }
        else
        {
            sb.AppendLine("        out.t = static_cast<double>(v);");
        }
        if (hasLastType)
            sb.AppendLine("        out.type = static_cast<int>(parser_->last_type());");
        else
            sb.AppendLine("        out.type = 0;");
        sb.AppendLine("        out.fields[\"value\"] = std::to_string(v);");
        sb.AppendLine("        pos_++;");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine("    bool can_resume() const override { return true; }");
        sb.AppendLine("    uint64_t position() const override { return pos_; }");
        sb.AppendLine("    void resume_at(uint64_t off) override { pos_ = off; /* UNCERTAIN: seek proprietary stream */ }");
        sb.AppendLine("private:");
        sb.AppendLine($"    std::unique_ptr<{primary.Name}> parser_;");
        sb.AppendLine("    uint64_t pos_ = 0;");
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine("inline FileInfo inspect_custom(const std::filesystem::path& path) {");
        sb.AppendLine("    FileInfo fi;");
        sb.AppendLine("    fi.bytes = std::filesystem::file_size(path);");
        sb.AppendLine("    fi.resumable = true;");
        var hasEventCount = primary.Methods.Any(m => m is "event_count" or "eventCount");
        var hasTBegin = primary.Methods.Any(m => m is "t_begin" or "tBegin");
        var hasTEnd = primary.Methods.Any(m => m is "t_end" or "tEnd");
        if (pathCtor)
        {
            // Match DemoQueueReader / EVTQ taxonomy when the proprietary type exposes it.
            sb.AppendLine("    for (auto* n : kTypeNames) fi.types.push_back(n);");
            if (hasEventCount || hasTBegin || hasTEnd)
            {
                sb.AppendLine("    try {");
                sb.AppendLine($"        {primary.Name} probe(path);");
                if (hasEventCount)
                    sb.AppendLine("        fi.event_count = probe.event_count();");
                if (hasTBegin)
                    sb.AppendLine("        fi.t_begin = probe.t_begin();");
                if (hasTEnd)
                    sb.AppendLine("        fi.t_end = probe.t_end();");
                sb.AppendLine("    } catch (...) { /* leave defaults */ }");
            }
            // process() early-exits when e.t > t1; a default t_end=0 keeps only the
            // first zero-timestamp event. Open the window when the parser didn't probe it.
            sb.AppendLine("    if (fi.t_end <= fi.t_begin) fi.t_end = 1e300;");
        }
        else
        {
            sb.AppendLine("    fi.types = {\"VALUE\"};");
            sb.AppendLine("    // UNCERTAIN: fill event_count / t_begin / t_end from proprietary header");
            sb.AppendLine("    if (fi.t_end <= fi.t_begin) fi.t_end = 1e300;");
        }
        sb.AppendLine("    return fi;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("inline void use_custom_reader(){");
        sb.AppendLine("    reader_factory() = [](const std::filesystem::path& p){");
        sb.AppendLine("        return std::unique_ptr<IEventReader>(new CustomEventReader(p));");
        sb.AppendLine("    };");
        sb.AppendLine("    inspect_factory() = inspect_custom;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("} // namespace fileproc");
        sb.AppendLine();
        return sb.ToString();
    }

    public static List<ClassApi> ExtractClasses(string source)
    {
        var list = new List<ClassApi>();
        // Match the class head allowing an optional base-class list
        // ("class Foo : public Bar, private Baz {"), then brace-match the body so inner
        // struct/enum "{ ... };" blocks don't terminate the scan early (the old non-greedy
        // "[\s\S]*?\n\s*};" stopped at the first inner "};" and dropped later methods).
        foreach (Match head in Regex.Matches(source, @"\bclass\s+(\w+)\s*(?::[^{;]*)?\{"))
        {
            var name = head.Groups[1].Value;
            var openBrace = head.Index + head.Length - 1;   // index of the opening '{'
            var body = ExtractBracedBody(source, openBrace);
            if (body is null) continue;
            var methods = Regex.Matches(body, @"\b(\w+)\s*\([^)]*\)\s*(?:const\s*)?(?:override\s*)?\{")
                .Select(x => x.Groups[1].Value)
                .Where(x => x is not ("if" or "while" or "for" or "switch" or "return" or "catch"))
                .Where(x => !string.Equals(x, name, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            // Out-of-line declarations: "bool advance(long& v);" / "explicit Foo(const path& p);"
            foreach (Match dm in Regex.Matches(
                         body,
                         @"^\s*(?:explicit\s+)?(?:[\w:]+\s+)+\*?&?(\w+)\s*\([^;]*\)\s*(?:const\s*)?;\s*$",
                         RegexOptions.Multiline))
            {
                var mname = dm.Groups[1].Value;
                if (mname is "if" or "while" or "for" or "switch" or "return" or "catch") continue;
                if (string.Equals(mname, name, StringComparison.Ordinal)) continue;
                if (!methods.Contains(mname, StringComparer.Ordinal))
                    methods.Add(mname);
            }
            if (methods.Count > 0)
                list.Add(new ClassApi(name, methods.ToArray()));
        }
        return list;
    }

    /// <summary>
    /// Returns the text between the given opening '{' and its matching '}', counting nested
    /// braces so inner struct/enum/union bodies don't end the scan early. Null if unbalanced.
    /// Brace counting is literal (no string/comment awareness) — adequate for the header API
    /// surfaces this scaffolder inspects, matching the heuristic level of the rest of the file.
    /// </summary>
    static string? ExtractBracedBody(string s, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return s.Substring(openBraceIndex + 1, i - openBraceIndex - 1);
            }
        }
        return null;
    }
}
