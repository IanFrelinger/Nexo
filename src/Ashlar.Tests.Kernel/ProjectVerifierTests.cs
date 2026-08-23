using FluentAssertions;
using Ashlar.Manifest;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>
/// Pins the courses <c>ashlar verify</c> runs and, as importantly, what it refuses to claim:
/// there is no provenance course and no signature output until real signing exists.
/// </summary>
public sealed class ProjectVerifierTests : IDisposable
{
    private readonly string _dir;

    public ProjectVerifierTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private (string manifest, string policy) Scaffolded()
    {
        ProjectScaffold.TryScaffold("verify-demo", out var m, out var p, out var reason)
            .Should().BeTrue(reason);
        return (m, p);
    }

    [Fact]
    public void A_freshly_scaffolded_project_verifies()
    {
        // The init -> verify loop must hold: what init writes, verify accepts.
        var (m, p) = Scaffolded();

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Verified.Should().BeTrue(string.Join(" | ", result.Courses.Select(c => c.Detail)));
        result.Courses.Select(c => c.Name).Should().Equal("contract", "composition", "envelope");
    }

    [Fact]
    public void No_course_claims_provenance_or_a_signature_yet()
    {
        // Honesty pin: until real keys exist, verify must not pretend to check signatures.
        var (m, p) = Scaffolded();

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Courses.Should().NotContain(c => c.Name == "provenance");
        result.Courses.Should().OnlyContain(c => !c.Detail.Contains("ed25519"));
    }

    [Fact]
    public void Broken_contract_fails_fast_with_the_loader_reason()
    {
        var (_, p) = Scaffolded();

        var result = ProjectVerifier.Verify("kind: Nonsense", p, _dir);

        result.Verified.Should().BeFalse();
        result.Courses.Should().ContainSingle();
        result.Courses[0].Name.Should().Be("contract");
        result.Courses[0].Detail.Should().Contain("REJECTED");
    }

    [Fact]
    public void An_ungated_agent_fails_composition()
    {
        var (m, p) = Scaffolded();
        m = m.Replace("gates: [tests]", "gates: []");

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Verified.Should().BeFalse();
        result.Courses.Single(c => c.Name == "composition").Detail.Should().Contain("no gates");
    }

    [Fact]
    public void A_missing_sandbox_root_fails_the_envelope()
    {
        var (m, p) = Scaffolded();
        p = p.Replace("root: .", "root: does-not-exist");

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Verified.Should().BeFalse();
        result.Courses.Single(c => c.Name == "envelope").Detail.Should().Contain("does not exist");
    }

    [Fact]
    public void A_writable_path_escaping_the_root_fails_the_envelope()
    {
        var (m, p) = Scaffolded();
        p = p.Replace("writable: []", "writable: [../outside]");

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Verified.Should().BeFalse();
        result.Courses.Single(c => c.Name == "envelope").Detail.Should().Contain("escapes");
    }

    [Fact]
    public void An_admitting_mode_with_a_zero_budget_fails_the_envelope()
    {
        var (m, p) = Scaffolded();
        // Scaffold is sealed with extensions: 0 — valid. Flip the mode without funding it.
        p = p.Replace("mode: sealed", "mode: proposing")
             .Replace("gatesRequired: []", "gatesRequired: [tests]");

        var result = ProjectVerifier.Verify(m, p, _dir);

        result.Verified.Should().BeFalse(
            "a mode that can admit extensions with budget 0 can never admit anything");
        result.Courses.Single(c => c.Name == "envelope").Detail.Should().Contain("seal it or fund it");
    }

    [Fact]
    public void Sealed_with_zero_budget_is_fine_because_it_admits_nothing()
    {
        var (m, p) = Scaffolded();

        ProjectVerifier.Verify(m, p, _dir).Verified.Should().BeTrue();
    }
}
