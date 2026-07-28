using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Agents.TestKit;

/// <summary>
/// Conformance suite every agent profile must pass. Subclass it once per profile:
/// the convention stops being prose in a design doc and becomes a test the profile
/// either passes or fails.
///
/// Every case runs OFFLINE — <see cref="OfflineAgentEnvironment"/> is in force and
/// the model edge is a <see cref="FakeProviderFactory"/> that throws rather than
/// reaching a socket, so a profile that secretly needs a live model fails loudly.
/// </summary>
/// <typeparam name="TProfileFactory">The profile factory under test.</typeparam>
public abstract class AgentProfileConformanceTests<TProfileFactory>
    where TProfileFactory : IAgentProfileFactory, new()
{
    /// <summary>Tokens that must never appear on the domain-neutral contract surface.</summary>
    private static readonly string[] ForbiddenSurfaceTokens =
    {
        "cpp", "cplusplus", "gxx", "gplusplus", "docker", "container", "poco", "evtx",
        "evtq", "qt", "ollama", "sql", "typescript", "python", "csv"
    };

    /// <summary>The target id the profile is expected to register.</summary>
    protected abstract string ExpectedTargetId { get; }

    /// <summary>
    /// Grounding that should be enough for the profile's deterministic path to
    /// produce something real.
    /// </summary>
    protected abstract GenerationRequest OfflineRequest(OfflineAgentEnvironment environment);

    /// <summary>Builds the factory. Override when it needs constructor arguments.</summary>
    protected virtual TProfileFactory CreateFactory() => new();

    /// <summary>
    /// Services handed to the factory. The default registers offline fakes; override
    /// to add profile-specific registrations, calling <see cref="AddOfflineDefaults"/>
    /// to keep the offline guarantees.
    /// </summary>
    protected virtual IServiceProvider BuildServices(OfflineAgentEnvironment environment)
    {
        var services = new ServiceCollection();
        AddOfflineDefaults(services);
        return services.BuildServiceProvider();
    }

    /// <summary>Registers the offline model edge shared by every conformance run.</summary>
    protected static void AddOfflineDefaults(IServiceCollection services)
    {
        services.AddSingleton<IProviderFactory>(new FakeProviderFactory());
    }

    /// <summary>Creates the profile under an offline environment.</summary>
    protected AgentProfile CreateProfile(OfflineAgentEnvironment environment) =>
        CreateFactory().Create(BuildServices(environment));

    /// <summary>Conformance: Profile_registers_its_declared_target_id.</summary>
    [Fact]
    public void Profile_registers_its_declared_target_id()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        profile.TargetId.Should().Be(ExpectedTargetId);
        profile.Drafter.Should().NotBeNull("every profile needs a primary drafter");
    }

    /// <summary>Conformance: Profile_supplies_a_deterministic_drafter.</summary>
    [Fact]
    public void Profile_supplies_a_deterministic_drafter()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        // The convention: a profile must be able to produce an artifact with no model
        // in the loop. Without this, offline CI can only test the plumbing.
        profile.Capabilities.SupportsDeterministic.Should().BeTrue(
            "a profile must be able to draft without a model");
        profile.DeterministicDrafter.Should().NotBeNull(
            "SupportsDeterministic is a promise that DeterministicDrafter keeps");
    }

    /// <summary>Conformance: Offline_run_produces_a_valid_artifact_and_provenance.</summary>
    [Fact]
    public async Task Offline_run_produces_a_valid_artifact_and_provenance()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        profile.DeterministicDrafter!.TryDraft(OfflineRequest(env), out var artifact)
            .Should().BeTrue("the deterministic path must handle the profile's own offline request");
        artifact.Should().NotBeNull();
        artifact!.Content.Should().NotBeNullOrWhiteSpace();

        // Whatever the profile's own validators say must hold for its own draft.
        foreach (var validator in profile.Validators.OfType<IPostValidator<GeneratedArtifact>>())
        {
            var (ok, reason) = await validator.ValidateAsync(artifact, CancellationToken.None);
            ok.Should().BeTrue($"the profile's deterministic draft must satisfy its own validators: {reason}");
        }
    }

    /// <summary>Conformance: Repair_from_an_empty_first_draft_converges_within_bound.</summary>
    [Fact]
    public async Task Repair_from_an_empty_first_draft_converges_within_bound()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        profile.DeterministicDrafter!.TryDraft(OfflineRequest(env), out var good).Should().BeTrue();

        // First attempt returns nothing; the loop must feed that back and recover.
        var drafter = new FakeArtifactDrafter(new GeneratedArtifact { Content = "" }, good!);
        var bound = Math.Max(2, profile.Tunables.MaxRepairAttempts);

        var loop = new GenerationRepairLoop<GeneratedArtifact>(
            async (prior, ct) => await drafter.DraftAsync(
                OfflineRequest(env), prior.Select(p => p.Reason).ToArray(), ct),
            profile.Validators.OfType<IPostValidator<GeneratedArtifact>>().Concat(
                new IPostValidator<GeneratedArtifact>[] { new NonEmptyArtifactValidator() }).ToArray(),
            new RepairOptions(bound));

        var result = await loop.RunAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue("repair from an empty draft must converge");
        AgentFlowAssert.ConvergedWithin(drafter, bound);
        AgentFlowAssert.FedFailureBack(drafter);
    }

    /// <summary>Conformance: Scripted_install_failure_rolls_back_through_the_managed_file_set.</summary>
    [Fact]
    public async Task Scripted_install_failure_rolls_back_through_the_managed_file_set()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        profile.DeterministicDrafter!.TryDraft(OfflineRequest(env), out var artifact).Should().BeTrue();

        var files = new FakeManagedFileSet().Seed("dest", "artifact", "PRIOR_ARTIFACT");
        var target = new FakeDeploymentTarget(
            files,
            Scripted<DeploymentSmokeResult>.Always(DeploymentSmokeResult.Fail("scripted install failure")));

        // The profile's OWN evaluator decides — this is the profile's rollback story,
        // not the harness's.
        var acceptance = profile.Acceptance ?? DefaultAcceptanceEvaluator.Instance;
        var result = await target.ApplyAsync(artifact!, acceptance);

        result.Succeeded.Should().BeFalse();
        AgentFlowAssert.RollbackRestoredPriorState(
            target,
            files,
            new Dictionary<string, string> { ["dest/artifact"] = "PRIOR_ARTIFACT" });
    }

    /// <summary>Conformance: Profile_supplies_an_acceptance_evaluator_that_is_pure.</summary>
    [Fact]
    public void Profile_supplies_an_acceptance_evaluator_that_is_pure()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        profile.Acceptance.Should().NotBeNull(
            "a profile owns its own ship/rollback verdict rather than inheriting a guess");

        var context = new AcceptanceContext(
            new GeneratedArtifact { Content = "x" },
            GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true),
            DeploymentSmokeResult.Fail("nothing produced"));

        var first = profile.Acceptance!.Evaluate(context);
        var second = profile.Acceptance.Evaluate(context);

        second.Decision.Should().Be(first.Decision, "an evaluator must be a pure function of its context");
        second.Reason.Should().Be(first.Reason);
        first.Decision.Should().Be(AcceptanceDecision.Rollback, "a failed smoke is never acceptable");
    }

    /// <summary>Conformance: No_domain_names_leak_into_the_contract_surface.</summary>
    [Fact]
    public void No_domain_names_leak_into_the_contract_surface()
    {
        using var env = new OfflineAgentEnvironment();
        var profile = CreateProfile(env);

        foreach (var port in PortImplementations(profile))
        {
            foreach (var contractType in ContractInterfacesOf(port))
            {
                AssertNeutral(contractType.Name, $"port interface {contractType.FullName}");

                foreach (var member in contractType.GetMembers())
                {
                    AssertNeutral(member.Name, $"member {contractType.Name}.{member.Name}");
                    if (member is MethodInfo method)
                    {
                        AssertNeutral(method.ReturnType.Name, $"return type of {contractType.Name}.{method.Name}");
                        foreach (var parameter in method.GetParameters())
                        {
                            AssertNeutral(
                                parameter.ParameterType.Name,
                                $"parameter '{parameter.Name}' of {contractType.Name}.{method.Name}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>Conformance: Contracts_assembly_names_nothing_domain_specific.</summary>
    [Fact]
    public void Contracts_assembly_names_nothing_domain_specific()
    {
        var exported = typeof(AgentProfile).Assembly.GetExportedTypes();

        // Guard against a vacuous pass: if the scan ever stops seeing types, this
        // test would "pass" while checking nothing at all.
        exported.Should().HaveCountGreaterThan(20, "the contracts assembly should expose a real surface to scan");
        exported.Select(t => t.Name).Should().Contain(nameof(AgentProfile));

        foreach (var type in exported)
        {
            AssertNeutral(type.Name, $"exported contract type {type.FullName}");
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                AssertNeutral(member.Name, $"exported member {type.Name}.{member.Name}");
        }
    }

    private static IEnumerable<object> PortImplementations(AgentProfile profile)
    {
        if (profile.Drafter is { } drafter) yield return drafter;
        if (profile.DeterministicDrafter is { } deterministic) yield return deterministic;
        if (profile.Sandbox is { } sandbox) yield return sandbox;
        if (profile.Deployment is { } deployment) yield return deployment;
        if (profile.Acceptance is { } acceptance) yield return acceptance;
        foreach (var validator in profile.Validators)
            yield return validator;
    }

    private static IEnumerable<Type> ContractInterfacesOf(object port)
    {
        var contracts = typeof(AgentProfile).Assembly;
        var application = typeof(IPostValidator<>).Assembly;
        return port.GetType().GetInterfaces()
            .Where(i => i.Assembly == contracts || i.Assembly == application);
    }

    private static void AssertNeutral(string name, string what)
    {
        var lowered = name.ToLowerInvariant();
        foreach (var token in ForbiddenSurfaceTokens)
        {
            // Whole-word-ish match: avoids flagging "Description" for "script".
            if (ContainsToken(lowered, token))
            {
                throw new AgentFlowAssertionException(
                    $"Domain name '{token}' leaked into the neutral surface via {what}. "
                    + "Concrete toolchain vocabulary belongs in profile plugins, not in contracts or ports.");
            }
        }
    }

    private static bool ContainsToken(string lowered, string token)
    {
        var index = lowered.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeOk = index == 0 || !char.IsLetter(lowered[index - 1]);
            var after = index + token.Length;
            var afterOk = after >= lowered.Length || !char.IsLetter(lowered[after]);
            if (beforeOk && afterOk)
                return true;
            index = lowered.IndexOf(token, index + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private sealed class NonEmptyArtifactValidator : IPostValidator<GeneratedArtifact>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(
            GeneratedArtifact output,
            CancellationToken ct) =>
            ValueTask.FromResult<(bool, string?)>(
                string.IsNullOrWhiteSpace(output.Content)
                    ? (false, "artifact is empty")
                    : (true, null));
    }
}
