using Ashlar.CLI.Output;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Output;

/// <summary>
/// THE FORGED ROW. A pull refusal prints two pieces of text the SENDER chose — the filename, and a
/// symlink's target quoted inside the reason — on the same line as the one thing a sender cannot
/// choose, the sealer fingerprint. A terminal ACTS on a CR or an ESC in that text, so without
/// escaping, a filename is enough to paint a counterfeit "admitted, sealed by someone you trust"
/// over the refusal that names it, or to erase the real fingerprint line above.
///
/// <para>The other half of the requirement is that the text still ARRIVES: the operator has to be
/// told which file was refused, so a hostile name is shown with its escapes defused, never dropped
/// and never grounds for printing nothing.</para>
/// </summary>
public sealed class UntrustedTextTests
{
    [Fact]
    public void HonestText_isReturnedUnchanged()
    {
        const string honest = "update-2026-09-01.ashpkg";

        UntrustedText.ForConsole(honest).Should().BeSameAs(honest,
            "an ordinary filename must cost nothing and must not be altered");
    }

    [Fact]
    public void ACarriageReturn_cannotRepaintTheRow()
    {
        var forged = "update.ashpkg\r  ✓ ADMITTED  update  · sealed by a-trusted-fingerprint";

        var safe = UntrustedText.ForConsole(forged);

        safe.Should().NotContain("\r", "a CR returns to column zero and overwrites the refusal");
        safe.Should().Contain("\ufffd", "the escape is replaced by something visible, not deleted");
        safe.Should().Contain("ADMITTED",
            "the text is still shown — the operator has to see what was refused, forged wording and all");
    }

    [Fact]
    public void ANewline_cannotSplitOneRowIntoTwo()
    {
        UntrustedText.ForConsole("first.ashpkg\n  second line").Should().NotContain("\n");
    }

    [Fact]
    public void AnAnsiSequence_cannotMoveTheCursorOrClearALine()
    {
        UntrustedText.ForConsole("x\u001b[2K\u001b[1Aevil.ashpkg")
            .Should().NotContain("\u001b", "ESC introduces every ANSI and OSC sequence");
    }

    [Fact]
    public void ABareCsi_isEscapedToo_becauseStrippingEscAloneWouldMissIt()
    {
        UntrustedText.ForConsole("x\u009b31mevil.ashpkg")
            .Should().NotContain("\u009b", "U+009B is CSI in its own right, with no ESC in front of it");
    }

    [Fact]
    public void ABidiOverride_cannotReorderAHostileNameIntoAnHonestOne()
    {
        UntrustedText.ForConsole("gpkhsa.\u202egpkhsa.ashpkg")
            .Should().NotContain("\u202e", "an override reorders what is already on the line");
    }

    [Fact]
    public void EveryCharacterIsAccountedFor_soNothingIsSilentlyDropped()
    {
        const string mixed = "a\rb\u001bc\u009bd\u202ee";

        var safe = UntrustedText.ForConsole(mixed);

        safe.Should().HaveLength(mixed.Length, "replacement is one-for-one, never a deletion");
        safe.Should().Be("a\ufffdb\ufffdc\ufffdd\ufffde");
    }

    [Fact]
    public void ALegitimateNonAsciiName_isReturnedUnchanged()
    {
        // The escaping must not be fail-closed against HONEST operators. A team that names packages
        // in its own language is ordinary, and a filter reaching for "non-ASCII" instead of
        // "terminal-actionable" would turn every one of their rows into a wall of U+FFFD — after
        // which the way out an operator finds on their own is to stop reading the refusals.
        const string honest = "ünïcode-日本語-café-Ω-→.ashpkg";

        UntrustedText.ForConsole(honest).Should().BeSameAs(honest,
            "nothing here is a character a terminal acts on, so not one byte may change");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmpty_isTheEmptyString(string? input)
    {
        UntrustedText.ForConsole(input).Should().BeEmpty();
    }

    [Fact]
    public void AMegabyteOfSenderChosenText_isBounded_notJustDefused()
    {
        // Escaping is one-for-one, so without a bound a hostile string is rendered at its full
        // length in U+FFFDs. A package's formatVersion is a required JSON string with no cap of its
        // own, quoted into a refusal, so "how many characters does the operator's terminal have to
        // render" is a number the SENDER picks — and the row naming the refused file scrolls away.
        var flood = new string('A', 1_000_000);

        var safe = UntrustedText.ForConsole(flood);

        safe.Length.Should().BeLessThan(UntrustedText.DefaultMaxChars + 100,
            "the bound is on the output, not a suggestion");
        safe.Should().Contain("truncated", "silently cutting it would read as the sender's actual text");
        safe.Should().Contain("1,000,000", "the operator is told how much there was");
    }

    [Fact]
    public void TruncationHappensBeforeEscaping_soAHostileStringIsStillDefused()
    {
        // Order matters for cost, not for correctness — but the escaping must survive the reorder.
        var flood = new string('\r', 1_000_000);

        var safe = UntrustedText.ForConsole(flood);

        safe.Should().NotContain("\r", "a bounded line that still carries a CR forges a row just as well");
        safe.Should().Contain("\ufffd");
    }

    [Fact]
    public void TextExactlyAtTheLimit_isNotTruncated()
    {
        var exact = new string('a', UntrustedText.DefaultMaxChars);

        UntrustedText.ForConsole(exact).Should().BeSameAs(exact,
            "the bound must not cost an allocation for text that fits");
    }

    [Fact]
    public void ASurrogatePairIsNeverSplitByTheBound()
    {
        // Half a surrogate pair renders as a replacement glyph, which would look like an escape this
        // function had defused — a lie about what the input contained.
        const int limit = 8;
        var text = "abcdefg" + "\U0001F600" + "hijkl";   // the pair straddles index 7-8

        var safe = UntrustedText.ForConsole(text, limit);

        safe.Should().StartWith("abcdefg", "the pair is dropped whole rather than halved");
        char.IsHighSurrogate(safe[6]).Should().BeFalse();
        safe.Should().Contain("truncated");
    }
}
