using FluentAssertions;
using Nexo.Core.Application.Generation;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Stage-1 context assembly (trust-loop R1.2/R1.4/R1.5): deterministic ordering and
/// hashing, explicit per-class budgets with truncation/omission records, provenance
/// tiers, canonicalization, and seed recording.
/// </summary>
public sealed class ProposalContextAssemblerTests
{
    [Fact]
    public void SameSources_InDifferentInputOrder_ProduceIdenticalContext()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);
        var a = Source("witness", "w1", "alpha", ProposalContextTier.MachineReadable);
        var b = Source("reference", "r1", "bravo", ProposalContextTier.Documentation);
        var c = Source("notes", "n1", "charlie", ProposalContextTier.ModelRecall);

        var first = assembler.Assemble(new[] { c, a, b });
        var second = assembler.Assemble(new[] { b, c, a });

        first.Hash.Should().Be(second.Hash, "same inputs must produce the same context (R1.2)");
        first.Elements.Select(e => e.Id).Should().Equal(second.Elements.Select(e => e.Id));
        first.RenderText().Should().Be(second.RenderText());
    }

    [Fact]
    public void MachineReadableGroundTruth_OrdersAheadOfDocsAndRecall()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);

        var context = assembler.Assemble(new[]
        {
            Source("k", "recall", "x", ProposalContextTier.ModelRecall),
            Source("k", "docs", "x", ProposalContextTier.Documentation),
            Source("k", "truth", "x", ProposalContextTier.MachineReadable)
        });

        context.Elements.Select(e => e.Id).Should().Equal("truth", "docs", "recall");
    }

    [Fact]
    public void ClassBudget_TruncatesAndRecordsTheCut()
    {
        var assembler = new ProposalContextAssembler(
            defaultMaxCharsPerKind: 1000,
            budgets: new[] { new ProposalContextBudget { Kind = "docs", MaxChars = 10 } });

        var context = assembler.Assemble(new[] { Source("docs", "d1", new string('x', 25)) });

        var element = context.Elements.Single();
        element.Truncated.Should().BeTrue();
        element.Content.Should().HaveLength(10);
        element.OriginalLength.Should().Be(25);
    }

    [Fact]
    public void SourceBeyondSpentClassBudget_IsOmittedAndRecorded()
    {
        var assembler = new ProposalContextAssembler(
            defaultMaxCharsPerKind: 1000,
            budgets: new[] { new ProposalContextBudget { Kind = "docs", MaxChars = 5 } });

        var context = assembler.Assemble(new[]
        {
            Source("docs", "a-first", "12345"),
            Source("docs", "b-second", "67890")
        });

        context.Elements.Select(e => e.Id).Should().Equal("a-first");
        context.OmittedSourceIds.Should().Equal("docs/b-second");
    }

    [Fact]
    public void ExplicitBudget_OverridesTheDefault()
    {
        var assembler = new ProposalContextAssembler(
            defaultMaxCharsPerKind: 3,
            budgets: new[] { new ProposalContextBudget { Kind = "wide", MaxChars = 100 } });

        var context = assembler.Assemble(new[]
        {
            Source("wide", "w", "1234567890"),
            Source("narrow", "n", "1234567890")
        });

        context.Elements.Single(e => e.Kind == "wide").Truncated.Should().BeFalse();
        context.Elements.Single(e => e.Kind == "narrow").Content.Should().HaveLength(3);
    }

    [Fact]
    public void LineEndings_AreCanonicalized_SoHashesMatchAcrossPlatforms()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);

        var crlf = assembler.Assemble(new[] { Source("k", "i", "line1\r\nline2\r") });
        var lf = assembler.Assemble(new[] { Source("k", "i", "line1\nline2\n") });

        crlf.Hash.Should().Be(lf.Hash);
        crlf.Elements.Single().ContentHash.Should().Be(lf.Elements.Single().ContentHash);
    }

    [Fact]
    public void Seed_IsRecordedAndChangesTheHash()
    {
        var sources = new[] { Source("k", "i", "content") };

        var unseeded = new ProposalContextAssembler(1000).Assemble(sources);
        var seeded = new ProposalContextAssembler(1000, seed: "42").Assemble(sources);

        seeded.Seed.Should().Be("42", "randomized steps must record their seed (R1.2)");
        seeded.Hash.Should().NotBe(unseeded.Hash);
    }

    [Fact]
    public void ContentChange_ChangesTheContextHash()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);

        var one = assembler.Assemble(new[] { Source("k", "i", "content-a") });
        var two = assembler.Assemble(new[] { Source("k", "i", "content-b") });

        one.Hash.Should().NotBe(two.Hash);
    }

    [Fact]
    public void EmptySources_ProduceAStableEmptyContext()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);

        var first = assembler.Assemble(Array.Empty<ProposalContextSource>());
        var second = assembler.Assemble(Array.Empty<ProposalContextSource>());

        first.Elements.Should().BeEmpty();
        first.Hash.Should().NotBeNullOrWhiteSpace();
        first.Hash.Should().Be(second.Hash);
    }

    [Fact]
    public void NonPositiveBudgets_AreRejected()
    {
        var defaultCtor = () => new ProposalContextAssembler(0);
        var classBudget = () => new ProposalContextAssembler(
            10, new[] { new ProposalContextBudget { Kind = "k", MaxChars = 0 } });

        defaultCtor.Should().Throw<ArgumentOutOfRangeException>();
        classBudget.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RenderText_TitlesEachElementWithKindIdAndTier()
    {
        var assembler = new ProposalContextAssembler(defaultMaxCharsPerKind: 1000);

        var text = assembler.Assemble(new[]
        {
            Source("witness", "w1", "the-content", ProposalContextTier.MachineReadable)
        }).RenderText();

        text.Should().Contain("== witness/w1 (machinereadable) ==");
        text.Should().Contain("the-content");
    }

    private static ProposalContextSource Source(
        string kind,
        string id,
        string content,
        ProposalContextTier tier = ProposalContextTier.Documentation) =>
        new() { Kind = kind, Id = id, Content = content, Tier = tier };
}
