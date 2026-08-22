using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Orchestration.Architect;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>
/// Kernel-side coverage for <see cref="DomainRecognizer"/> and the
/// <see cref="IDomainPatternProvider"/> seam.
///
/// <para>Before these existed the pattern table had NO direct test coverage at all — nothing
/// in the repository called RecognizeDomains or ExtractKeywords. It was reachable only
/// indirectly through RAG similarity scoring, which degrades silently rather than failing, so
/// a broken table would not have surfaced.</para>
///
/// <para>These use a local fake provider rather than the real GameDomainPatternProvider, so
/// this file stays in the kernel when the game layer is extracted. Game vocabulary is
/// asserted in GameDomainPatternTests, which moves.</para>
/// </summary>
public class DomainRecognizerTests
{
    private static DomainRecognizer Recognizer(params IDomainPatternProvider[] providers) =>
        new(NullLogger<DomainRecognizer>.Instance, providers);

    private sealed class FakeProvider : IDomainPatternProvider
    {
        private readonly Dictionary<string, IReadOnlyList<Regex>> _patterns;

        public FakeProvider(string domain, string pattern) =>
            _patterns = new Dictionary<string, IReadOnlyList<Regex>>(StringComparer.OrdinalIgnoreCase)
            {
                [domain] = new List<Regex> { new(pattern, RegexOptions.IgnoreCase) },
            };

        public IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns => _patterns;
    }

    [Theory]
    [InlineData("we need to harden the authentication token flow", "Security")]
    [InlineData("scale the kubernetes deployment behind the load balancer", "Infrastructure")]
    [InlineData("the agent should improve its decision behavior", "AI")]
    public void Kernel_recognises_its_own_domains_with_no_providers(string request, string expected)
    {
        Recognizer().RecognizeDomains(request).Should().Contain(expected);
    }

    [Theory]
    [InlineData("rebalance the combat damage and weapon armor values")]
    [InlineData("players should be able to buy and sell loot at the vendor")]
    [InlineData("add a multiplayer matchmaking lobby for the quest")]
    public void Kernel_alone_does_not_recognise_game_vocabulary(string request)
    {
        // The whole point of the extraction: with no game layer installed the kernel declines
        // to guess at vocabulary it does not own. Not a failure — domain recognition feeds
        // RAG scoring and architect hints, both of which degrade rather than break.
        Recognizer().RecognizeDomains(request)
            .Should().NotContain(new[] { "Combat", "Economy", "Gameplay" });
    }

    [Fact]
    public void A_provider_can_add_a_domain_the_kernel_has_never_heard_of()
    {
        var sut = Recognizer(new FakeProvider("Underwriting", @"\b(premium|actuarial)\b"));

        sut.RecognizeDomains("recalculate the premium").Should().Contain("Underwriting");
    }

    [Fact]
    public void A_provider_extending_an_existing_domain_merges_rather_than_replaces()
    {
        // The failure this pins: if the merge assigned instead of appending, contributing to
        // "AI" would silently delete the kernel's own AI patterns, and the kernel would stop
        // recognising its own vocabulary the moment any package extended that domain.
        var sut = Recognizer(new FakeProvider("AI", @"\b(pathfinding|steering)\b"));

        sut.RecognizeDomains("tune the steering behaviour").Should().Contain("AI");   // provider's
        sut.RecognizeDomains("the agent made a decision").Should().Contain("AI");     // kernel's own
    }

    [Fact]
    public void Domain_keys_are_matched_case_insensitively_when_merging()
    {
        var sut = Recognizer(new FakeProvider("ai", @"\b(pathfinding)\b"));

        // Contributed under lowercase "ai"; must land in the kernel's "AI" bucket rather than
        // creating a second, separate domain that differs only by case.
        sut.RecognizeDomains("fix the pathfinding").Should().ContainSingle().Which.Should().Be("AI");
    }

    [Fact]
    public void Recognizer_resolved_from_the_container_receives_registered_providers()
    {
        // DomainRecognizer's provider parameter is optional, so every hand-constructed test
        // above would pass even if the container never populated it. Production resolves it
        // from DI; this is the only test that covers that path.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainPatternProvider>(new FakeProvider("Underwriting", @"\b(actuarial)\b"));
        services.AddSingleton<DomainRecognizer>();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<DomainRecognizer>()
            .RecognizeDomains("run the actuarial model")
            .Should().Contain("Underwriting");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Blank_requests_recognise_nothing(string? request)
    {
        Recognizer().RecognizeDomains(request!).Should().BeEmpty();
    }
}
