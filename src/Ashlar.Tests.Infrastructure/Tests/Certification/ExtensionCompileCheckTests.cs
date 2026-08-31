using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// A2 (executed evidence): an autonomous admission must rest on a REAL compile, not a self-reported
/// course. These pin (1) the Roslyn compile check itself and (2) that the admission gate rejects a
/// proposal whose <c>build</c> course failed when the policy requires it — so a brick that does not
/// compile can never be autonomously admitted.
///
/// <para>In <c>...Tests.Certification</c> so it rides cert-gate. Hermetic: in-process Roslyn, no SDK.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class ExtensionCompileCheckTests
{
    private static readonly RoslynExtensionCompileCheck Check = new();

    [Fact]
    public async Task Compiles_cleanCode_passes()
    {
        var files = new[] { new ProposedFileContent("src/Greeter.cs",
            "namespace Demo; public sealed class Greeter { public string Hi() => \"hello\"; }") };

        var r = await Check.CheckAsync(files);

        r.Passed.Should().BeTrue(r.Detail);
        r.Detail.Should().Contain("compiled clean");
    }

    [Fact]
    public async Task Compiles_brokenCode_fails_withDiagnostics()
    {
        var files = new[] { new ProposedFileContent("src/Broken.cs",
            "namespace Demo; public sealed class Broken { public int Oops() => ; }") };

        var r = await Check.CheckAsync(files);

        r.Passed.Should().BeFalse();
        r.Detail.Should().Contain("compile error");
    }

    [Fact]
    public async Task Compiles_referencingUnknownType_fails()
    {
        var files = new[] { new ProposedFileContent("src/Uses.cs",
            "namespace Demo; public sealed class Uses { public object Make() => new ThisTypeDoesNotExist(); }") };

        (await Check.CheckAsync(files)).Passed.Should().BeFalse("an unresolved type must not pass the build course");
    }

    [Fact]
    public async Task Compiles_noCodeFiles_passesTrivially()
    {
        var files = new[] { new ProposedFileContent("docs/readme.md", "# just docs") };

        (await Check.CheckAsync(files)).Passed.Should().BeTrue("a docs-only change has nothing to compile");
    }

    [Fact]
    public void Gate_rejects_whenRequiredBuildCourseFailed()
    {
        var policy = ProposingPolicyRequiring("sandbox", "build");
        var proposal = ProposalWith(
            new CourseResult { Name = "sandbox", Passed = true, Detail = "confined" },
            new CourseResult { Name = "build", Passed = false, Detail = "2 compile error(s): ..." });

        var outcome = AdmissionGate.Decide(policy, proposal, admittedInWindow: 0);

        outcome.State.Should().Be(ProposalState.Rejected);
        outcome.Reason.Should().Contain("build");
    }

    [Fact]
    public void Gate_rejects_whenRequiredBuildCourseMissing()
    {
        // A required course that never ran is a failure — the pre-A2 self-extend proposal (sandbox
        // only) can no longer slip past a build-requiring policy.
        var policy = ProposingPolicyRequiring("sandbox", "build");
        var proposal = ProposalWith(new CourseResult { Name = "sandbox", Passed = true, Detail = "confined" });

        var outcome = AdmissionGate.Decide(policy, proposal, admittedInWindow: 0);

        outcome.State.Should().Be(ProposalState.Rejected);
        outcome.Reason.Should().Contain("did not run");
    }

    [Fact]
    public void Gate_holds_whenBuildCoursePasses()
    {
        var policy = ProposingPolicyRequiring("sandbox", "build");
        var proposal = ProposalWith(
            new CourseResult { Name = "sandbox", Passed = true, Detail = "confined" },
            new CourseResult { Name = "build", Passed = true, Detail = "1 file(s) compiled clean" });

        var outcome = AdmissionGate.Decide(policy, proposal, admittedInWindow: 0);

        outcome.State.Should().Be(ProposalState.Held, "proposing mode holds a fully-passing proposal for a person");
    }

    private static AshlarPolicy ProposingPolicyRequiring(params string[] gates) => new()
    {
        SelfExtend = new PolicySelfExtend
        {
            Mode = SelfExtendMode.Proposing,
            MayAdd = ["brick"],
            GatesRequired = gates.ToList(),
            Budget = new PolicyBudget { Extensions = 3 },
        },
    };

    private static ExtensionProposal ProposalWith(params CourseResult[] courses) => new()
    {
        Id = "ext-test",
        Kind = "brick",
        Summary = "test",
        ProposedBy = "test",
        ProposedAt = DateTimeOffset.UtcNow,
        Diff = "~ src/x.cs",
        Courses = courses.ToList(),
    };
}
