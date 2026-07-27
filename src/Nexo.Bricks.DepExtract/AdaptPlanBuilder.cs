using System.Text;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Builds an operator-facing adapt plan: dependency structure + how it maps onto Poco's CustomEventReader seam.
/// Deterministic (no model) so planning works when Ollama is down.
/// </summary>
public static class AdaptPlanBuilder
{
    public sealed record ClassSurface(string Name, string[] Methods);
    public sealed record DiagramNode(string Id, string Label, string Kind);
    public sealed record DiagramEdge(string From, string To, string? Label);
    public sealed record DiagramModel(IReadOnlyList<DiagramNode> Nodes, IReadOnlyList<DiagramEdge> Edges);
    public sealed record PlanRisk(string Code, string Severity, string Message);
    /// <param name="Kind">must | must_not | note</param>
    public sealed record PlanConstraint(string Kind, string Text);

    public sealed record PlanDraft(
        string Title,
        string StructureSummary,
        string AdaptationSummary,
        IReadOnlyList<string> Steps,
        string Strategy,
        string DiagramMermaid,
        DiagramModel Diagram,
        IReadOnlyList<ClassSurface> Classes,
        IReadOnlyList<PlanRisk> Risks,
        IReadOnlyList<PlanConstraint> Constraints,
        string OperatorGuidance);

    public static PlanDraft Build(
        string srcDir,
        string[] entries,
        string[] lower,
        string[] upper,
        string[] external,
        string? sourceBundle = null,
        IEnumerable<string>? priorFeedback = null,
        bool includeUpper = false,
        IEnumerable<PlanConstraint>? extraConstraints = null)
    {
        entries ??= [];
        lower ??= [];
        upper ??= [];
        external ??= [];

        var classes = string.IsNullOrWhiteSpace(sourceBundle)
            ? new List<ClassSurface>()
            : AdapterScaffold.ExtractClasses(sourceBundle)
                .Select(c => new ClassSurface(c.Name, c.Methods))
                .ToList();

        var scaffoldable = !string.IsNullOrWhiteSpace(sourceBundle) && AdapterScaffold.LooksSimple(sourceBundle!);
        var strategy = scaffoldable ? "scaffold" : "model";

        var pullHint = classes
            .SelectMany(c => c.Methods)
            .FirstOrDefault(m => m is "advance" or "next" or "nextEvent" or "read" or "pull" or "readNext");

        var primaryClass = classes.FirstOrDefault()?.Name;

        var title = entries.Length == 1
            ? $"Adapt plan for {Path.GetFileName(entries[0])}"
            : $"Adapt plan for {entries.Length} entry files";

        var structure = new StringBuilder();
        structure.AppendLine($"Source tree: `{srcDir}`");
        structure.AppendLine();
        structure.AppendLine($"**Entries** ({entries.Length}) — start of the include graph the extractor walks:");
        foreach (var e in entries.Take(12))
            structure.AppendLine($"- `{e}`");
        if (entries.Length > 12) structure.AppendLine($"- … +{entries.Length - 12} more");
        structure.AppendLine();
        structure.AppendLine($"**Lower** ({lower.Length}) — headers/sources needed to compile the entries (copied into the extract duplicate):");
        foreach (var e in lower.Take(16))
            structure.AppendLine($"- `{e}`");
        if (lower.Length > 16) structure.AppendLine($"- … +{lower.Length - 16} more");
        structure.AppendLine();
        if (upper.Length > 0)
        {
            structure.AppendLine($"**Upper** ({upper.Length}) — dependents under the scan dir (not required for the Poco reader unless you asked to include them):");
            foreach (var e in upper.Take(8))
                structure.AppendLine($"- `{e}`");
            if (upper.Length > 8) structure.AppendLine($"- … +{upper.Length - 8} more");
            structure.AppendLine();
        }
        if (external.Length > 0)
        {
            structure.AppendLine($"**External** ({external.Length}) — includes that resolve *outside* the source directory (risk: may not be in the duplicate):");
            foreach (var e in external.Take(8))
                structure.AppendLine($"- `{e}`");
            if (external.Length > 8) structure.AppendLine($"- … +{external.Length - 8} more");
            structure.AppendLine();
        }
        if (classes.Count > 0)
        {
            structure.AppendLine("**Observed C++ surface** (from entry sources):");
            foreach (var c in classes.Take(6))
            {
                var methods = c.Methods.Length == 0 ? "(no methods detected)" : string.Join(", ", c.Methods.Take(10).Select(m => $"`{m}()`"));
                structure.AppendLine($"- class `{c.Name}` — {methods}");
            }
        }
        else
        {
            structure.AppendLine("**Observed C++ surface:** no `class … { … };` blocks detected in the entry files (structs/templates/macros may still exist).");
        }

        var adapt = new StringBuilder();
        adapt.AppendLine("Poco’s parser app loads a single seam header: `common/adapted_reader.hpp`, compiled with `-DNEXO_USE_CUSTOM`.");
        adapt.AppendLine("That header must define, in namespace `fileproc`:");
        adapt.AppendLine();
        adapt.AppendLine("1. `class CustomEventReader : public IEventReader` — open the proprietary file, implement `next(Event&, size_t)`, optional resume/`position`.");
        adapt.AppendLine("2. `FileInfo inspect_custom(const path&)` — cheap metadata for the Source list (bytes, types, time span when known).");
        adapt.AppendLine("3. `void use_custom_reader()` — register `reader_factory` + `inspect_factory`.");
        adapt.AppendLine();
        if (scaffoldable && primaryClass is not null)
        {
            var pull = pullHint is not null ? $" (drive `next()` via `{pullHint}()`)" : "";
            adapt.AppendLine(
                $"**Proposed strategy: deterministic scaffold.** The entry surface looks like a small pull API around `{primaryClass}`{pull}. Ollama is skipped unless the compile gate rejects the scaffold.");
        }
        else
        {
            adapt.AppendLine("**Proposed strategy: local model draft.** The surface is too irregular for the scaffold; a local Ollama model will draft `CustomEventReader` from the extracted sources only (no network).");
        }
        adapt.AppendLine();
        adapt.AppendLine("After you approve, the agent will:");
        adapt.AppendLine("- reuse the extracted `duplicate/` tree (already prepared during this plan);");
        adapt.AppendLine("- draft `adapted_reader.hpp` (scaffold and/or model);");
        adapt.AppendLine("- optionally g++-check against Poco’s `common/` seam;");
        adapt.AppendLine("- stop for Install into Poco (writes `common/adapted_reader.hpp`, optional `evtx:custom` rebuild).");

        var feedback = priorFeedback?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToArray() ?? [];
        if (feedback.Length > 0)
        {
            adapt.AppendLine();
            adapt.AppendLine("**Operator feedback incorporated into this revision:**");
            foreach (var f in feedback)
                adapt.AppendLine($"- {f}");
        }

        var risks = BuildRisks(lower, upper, external, classes, scaffoldable, includeUpper);
        var constraints = BuildConstraints(classes, pullHint, scaffoldable, feedback, extraConstraints);

        var steps = new List<string>
        {
            "Extract dependency closure (lower/upper/external) and materialize duplicate tree",
            scaffoldable
                ? "Draft CustomEventReader via deterministic scaffold (fallback to local model if needed)"
                : "Draft CustomEventReader via local Ollama against the duplicate sources only",
            "Map proprietary events → `fileproc::Event` (`t`, `type`, `fields`)",
            "Implement `inspect_custom` for the Event Extract Source list",
            "Optional compile gate against Poco `common/processing.hpp`",
            "On approval of the draft: Install into `common/adapted_reader.hpp` (+ optional image rebuild)",
        };

        var diagram = BuildDiagram(entries, lower, upper, external, primaryClass, pullHint);
        var mermaid = ToMermaid(diagram);
        var guidance = BuildGuidance(constraints, classes, pullHint, scaffoldable);

        return new PlanDraft(
            title,
            structure.ToString().TrimEnd(),
            adapt.ToString().TrimEnd(),
            steps,
            strategy,
            mermaid,
            diagram,
            classes,
            risks,
            constraints,
            guidance);
    }

    public static PlanDraft Revise(
        PlanDraft current,
        string feedback,
        string srcDir,
        string[] entries,
        string[] lower,
        string[] upper,
        string[] external,
        string? sourceBundle,
        IEnumerable<string> allFeedback,
        bool includeUpper = false,
        IEnumerable<PlanConstraint>? extraConstraints = null)
    {
        _ = current;
        _ = feedback;
        return Build(srcDir, entries, lower, upper, external, sourceBundle, allFeedback, includeUpper, extraConstraints);
    }

    public static string ToMarkdown(PlanDraft draft, string planId, string status, IEnumerable<string>? feedback = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {draft.Title}");
        sb.AppendLine();
        sb.AppendLine($"- Plan id: `{planId}`");
        sb.AppendLine($"- Status: `{status}`");
        sb.AppendLine($"- Strategy: `{draft.Strategy}`");
        sb.AppendLine();
        if (draft.Risks.Count > 0)
        {
            sb.AppendLine("## Risks");
            foreach (var r in draft.Risks)
                sb.AppendLine($"- **{r.Severity}** `{r.Code}` — {r.Message}");
            sb.AppendLine();
        }
        if (draft.Constraints.Count > 0)
        {
            sb.AppendLine("## Constraints");
            foreach (var c in draft.Constraints)
                sb.AppendLine($"- **{c.Kind}**: {c.Text}");
            sb.AppendLine();
        }
        sb.AppendLine("## Structure");
        sb.AppendLine();
        sb.AppendLine(draft.StructureSummary);
        sb.AppendLine();
        sb.AppendLine("## Adaptation to Poco");
        sb.AppendLine();
        sb.AppendLine(draft.AdaptationSummary);
        sb.AppendLine();
        sb.AppendLine("## Steps");
        foreach (var s in draft.Steps)
            sb.AppendLine($"1. {s}");
        sb.AppendLine();
        sb.AppendLine("## Diagram (Mermaid)");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine(draft.DiagramMermaid);
        sb.AppendLine("```");
        var fb = feedback?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? [];
        if (fb.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Feedback history");
            foreach (var f in fb)
                sb.AppendLine($"- {f}");
        }
        return sb.ToString();
    }

    static List<PlanRisk> BuildRisks(
        string[] lower, string[] upper, string[] external,
        List<ClassSurface> classes, bool scaffoldable, bool includeUpper)
    {
        var risks = new List<PlanRisk>();
        if (lower.Length == 0)
        {
            risks.Add(new PlanRisk(
                "empty_lower",
                "warn",
                "Lower closure is empty — either the entry is self-contained, or includes were not resolved. Confirm the duplicate has everything needed to compile."));
        }
        if (external.Length > 0)
        {
            risks.Add(new PlanRisk(
                "external_deps",
                "high",
                $"{external.Length} EXTERNAL include(s) resolve outside the source directory and may be missing from the extract duplicate."));
        }
        if (upper.Length > 0 && includeUpper)
        {
            risks.Add(new PlanRisk(
                "upper_included",
                "warn",
                $"Include-upper is on — {upper.Length} UPPER dependent(s) will be copied into the duplicate (larger tree, usually unnecessary for a reader)."));
        }
        else if (upper.Length > 0)
        {
            risks.Add(new PlanRisk(
                "upper_present",
                "info",
                $"{upper.Length} UPPER dependent(s) found but not included in the duplicate (good default for a Poco reader)."));
        }
        if (!scaffoldable)
        {
            risks.Add(new PlanRisk(
                "needs_model",
                "warn",
                "Surface is not scaffoldable — implement step needs a reachable local Ollama model."));
        }
        if (classes.Count == 0)
        {
            risks.Add(new PlanRisk(
                "no_class_surface",
                "warn",
                "No C++ class blocks detected in entry sources — field/method mapping will rely on operator constraints and model judgment."));
        }
        return risks;
    }

    static List<PlanConstraint> BuildConstraints(
        List<ClassSurface> classes,
        string? pullHint,
        bool scaffoldable,
        IReadOnlyList<string> feedback,
        IEnumerable<PlanConstraint>? extra)
    {
        var list = new List<PlanConstraint>
        {
            new("must", "Define fileproc::CustomEventReader : IEventReader with next(Event&, size_t)"),
            new("must", "Define inspect_custom + use_custom_reader registering reader_factory and inspect_factory"),
            new("must_not", "Invent proprietary APIs that do not appear in the extracted entry/lower sources"),
        };
        if (scaffoldable && pullHint is not null)
            list.Add(new("must", $"Drive CustomEventReader::next via proprietary `{pullHint}()`"));
        if (classes.Count > 0)
            list.Add(new("note", "Known classes: " + string.Join(", ", classes.Select(c => c.Name))));

        foreach (var c in extra ?? [])
        {
            if (string.IsNullOrWhiteSpace(c.Text)) continue;
            var kind = NormalizeKind(c.Kind);
            list.Add(new PlanConstraint(kind, c.Text.Trim()));
        }

        foreach (var f in feedback)
        {
            foreach (var parsed in ParseFeedbackLines(f))
                list.Add(parsed);
        }

        // De-dupe by kind+text
        return list
            .GroupBy(c => c.Kind + "\0" + c.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public static IEnumerable<PlanConstraint> ParseFeedbackLines(string feedback)
    {
        foreach (var raw in feedback.Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (Regex.IsMatch(line, @"^(must\s*not|mustn't|dont|don't|do\s+not)\b", RegexOptions.IgnoreCase))
            {
                var text = Regex.Replace(line, @"^(must\s*not|mustn't|dont|don't|do\s+not)\s*:?\s*", "", RegexOptions.IgnoreCase).Trim();
                yield return new PlanConstraint("must_not", string.IsNullOrEmpty(text) ? line : text);
            }
            else if (Regex.IsMatch(line, @"^must\b", RegexOptions.IgnoreCase))
            {
                var text = Regex.Replace(line, @"^must\s*:?\s*", "", RegexOptions.IgnoreCase).Trim();
                yield return new PlanConstraint("must", string.IsNullOrEmpty(text) ? line : text);
            }
            else
            {
                yield return new PlanConstraint("must", line);
            }
        }
    }

    static string NormalizeKind(string? kind) =>
        kind?.Trim().ToLowerInvariant() switch
        {
            "must_not" or "must-not" or "mustnot" or "dont" or "don't" => "must_not",
            "note" or "info" => "note",
            _ => "must",
        };

    static string BuildGuidance(
        IReadOnlyList<PlanConstraint> constraints,
        IReadOnlyList<ClassSurface> classes,
        string? pullHint,
        bool scaffoldable)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Follow the approved adapt plan constraints exactly.");
        if (scaffoldable)
            sb.AppendLine("Prefer a thin wrapper: CustomEventReader owns the proprietary parser and calls its pull/advance API inside next().");
        if (pullHint is not null)
            sb.AppendLine($"Primary pull method to drive next(): {pullHint}()");
        if (classes.Count > 0)
            sb.AppendLine("Known classes: " + string.Join(", ", classes.Select(c => c.Name)));
        foreach (var c in constraints.Where(c => c.Kind is "must" or "must_not"))
            sb.AppendLine($"{c.Kind.ToUpperInvariant()}: {c.Text}");
        return sb.ToString().TrimEnd();
    }

    static DiagramModel BuildDiagram(
        string[] entries, string[] lower, string[] upper, string[] external,
        string? primaryClass, string? pullHint)
    {
        var nodes = new List<DiagramNode>();
        var edges = new List<DiagramEdge>();

        nodes.Add(new DiagramNode("src", "Source tree", "root"));
        nodes.Add(new DiagramNode("dup", "Extract duplicate", "extract"));
        nodes.Add(new DiagramNode("seam", "Poco adapted_reader.hpp", "poco"));
        nodes.Add(new DiagramNode("gui", "Event Extract GUI", "poco"));

        edges.Add(new DiagramEdge("src", "dup", "dep-extract"));
        edges.Add(new DiagramEdge("dup", "seam", "adapt"));
        edges.Add(new DiagramEdge("seam", "gui", "use_custom_reader"));

        var entryIds = new List<string>();
        for (var i = 0; i < Math.Min(entries.Length, 4); i++)
        {
            var id = "e" + i;
            entryIds.Add(id);
            nodes.Add(new DiagramNode(id, ShortName(entries[i]), "entry"));
            edges.Add(new DiagramEdge("src", id, "entry"));
            edges.Add(new DiagramEdge(id, "dup", null));
        }
        if (entries.Length > 4)
        {
            nodes.Add(new DiagramNode("eMore", $"+{entries.Length - 4} entries", "meta"));
            edges.Add(new DiagramEdge("src", "eMore", null));
        }

        for (var i = 0; i < Math.Min(lower.Length, 5); i++)
        {
            var id = "l" + i;
            nodes.Add(new DiagramNode(id, ShortName(lower[i]), "lower"));
            var from = entryIds.Count > 0 ? entryIds[0] : "src";
            edges.Add(new DiagramEdge(from, id, "include"));
            edges.Add(new DiagramEdge(id, "dup", null));
        }
        if (lower.Length > 5)
        {
            nodes.Add(new DiagramNode("lMore", $"+{lower.Length - 5} lower", "meta"));
            edges.Add(new DiagramEdge(entryIds.Count > 0 ? entryIds[0] : "src", "lMore", "include"));
        }

        for (var i = 0; i < Math.Min(upper.Length, 2); i++)
        {
            var id = "u" + i;
            nodes.Add(new DiagramNode(id, ShortName(upper[i]), "upper"));
            edges.Add(new DiagramEdge(id, entryIds.Count > 0 ? entryIds[0] : "src", "uses"));
        }

        for (var i = 0; i < Math.Min(external.Length, 2); i++)
        {
            var id = "x" + i;
            nodes.Add(new DiagramNode(id, ShortName(external[i]), "external"));
            edges.Add(new DiagramEdge(entryIds.Count > 0 ? entryIds[0] : "src", id, "outside"));
        }

        if (primaryClass is not null)
        {
            nodes.Add(new DiagramNode("api", primaryClass + (pullHint is null ? "" : $".{pullHint}()"), "api"));
            nodes.Add(new DiagramNode("reader", "CustomEventReader.next()", "seam"));
            edges.Add(new DiagramEdge("dup", "api", "wrap"));
            edges.Add(new DiagramEdge("api", "reader", "maps to"));
            edges.Add(new DiagramEdge("reader", "seam", null));
        }

        return new DiagramModel(nodes, edges);
    }

    static string ToMermaid(DiagramModel d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart LR");
        foreach (var n in d.Nodes)
        {
            var label = EscapeMermaid(n.Label);
            var shape = n.Kind switch
            {
                "poco" or "seam" => $"[{label}]",
                "extract" or "root" => $"[{label}]",
                "api" or "entry" => $"({label})",
                "external" => $"{{{label}}}",
                _ => $"({label})",
            };
            sb.AppendLine($"  {n.Id}{shape}");
        }
        foreach (var e in d.Edges)
        {
            if (string.IsNullOrWhiteSpace(e.Label))
                sb.AppendLine($"  {e.From} --> {e.To}");
            else
                sb.AppendLine($"  {e.From} -- {EscapeMermaid(e.Label!)} --> {e.To}");
        }
        return sb.ToString().TrimEnd();
    }

    static string ShortName(string path)
    {
        var n = path.Replace('\\', '/');
        var leaf = n.Contains('/') ? n[(n.LastIndexOf('/') + 1)..] : n;
        return leaf.Length > 28 ? leaf[..25] + "…" : leaf;
    }

    static string EscapeMermaid(string s) =>
        Regex.Replace(s, @"[""\[\]]", " ").Trim();

    /// <summary>Load entry file text from an absolute source directory for surface analysis.</summary>
    public static string? TryLoadSourceBundle(string srcDir, string[] entries, long maxBytesPerFile = 400_000)
    {
        if (string.IsNullOrWhiteSpace(srcDir) || entries is null || entries.Length == 0)
            return null;
        var sb = new StringBuilder();
        foreach (var e in entries.Take(8))
        {
            var full = Path.IsPathRooted(e) ? e : Path.Combine(srcDir, e);
            if (!File.Exists(full)) continue;
            var info = new FileInfo(full);
            if (info.Length > maxBytesPerFile) continue;
            sb.AppendLine($"// ==== {e} ====");
            sb.AppendLine(File.ReadAllText(full));
            sb.AppendLine();
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}
