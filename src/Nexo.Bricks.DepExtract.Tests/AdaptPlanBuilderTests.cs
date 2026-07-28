using FluentAssertions;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdaptPlanBuilderTests
{
    [Fact]
    public void Build_includes_mermaid_risks_and_constraints()
    {
        const string src = """
            class SimpleCounter {
            public:
                bool advance(long& v) { v = 1; return true; }
            };
            """;
        var draft = AdaptPlanBuilder.Build(
            "/tmp/parser",
            ["Counter.hpp"],
            ["Types.hpp"],
            [],
            ["../vendor/x.hpp"],
            src);

        draft.Strategy.Should().Be("scaffold");
        draft.DiagramMermaid.Should().Contain("flowchart LR");
        draft.Diagram.Nodes.Should().Contain(n => n.Kind == "entry");
        draft.Risks.Should().Contain(r => r.Code == "external_deps");
        draft.Constraints.Should().Contain(c => c.Kind == "must" && c.Text.Contains("CustomEventReader"));
        draft.Constraints.Should().Contain(c => c.Kind == "must_not");
        draft.OperatorGuidance.Should().Contain("MUST");
        AdaptPlanBuilder.ToMarkdown(draft, "abc", "proposed").Should().Contain("## Risks");
    }

    [Fact]
    public void Revise_parses_must_and_must_not_feedback()
    {
        var first = AdaptPlanBuilder.Build("/tmp/p", ["a.hpp"], [], [], [], null);
        var revised = AdaptPlanBuilder.Revise(
            first, "Must put altitude in fields[alt]; must not invent seek()",
            "/tmp/p", ["a.hpp"], [], [], [], null,
            ["Must put altitude in fields[alt]; must not invent seek()"]);

        revised.Constraints.Should().Contain(c => c.Kind == "must" && c.Text.Contains("altitude"));
        revised.Constraints.Should().Contain(c => c.Kind == "must_not" && c.Text.Contains("seek"));
        revised.Risks.Should().Contain(r => r.Code == "empty_lower");
        revised.Risks.Should().Contain(r => r.Code == "needs_model");
    }

    [Fact]
    public void ParseFeedbackLines_classifies_constraints()
    {
        var parsed = AdaptPlanBuilder.ParseFeedbackLines("must map t from header\nmust not call fopen").ToArray();
        parsed.Should().Contain(c => c.Kind == "must" && c.Text.Contains("map t"));
        parsed.Should().Contain(c => c.Kind == "must_not" && c.Text.Contains("fopen"));
    }
}
