using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Agents.TestKit;
using Nexo.Authoring;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Bricks.SqlProfile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

// DomainBrick is this project's established global alias for
// Nexo.Core.Domain.Bricks.Brick: the test namespace ends in .Bricks, which
// would otherwise shadow the type.

namespace Nexo.Tests.Application.Tests.Bricks;

/// <summary>
/// Runtime backstop for the same drift class the NEXO0001/NEXO0002 analyzer
/// guards at build time.
///
/// The analyzer is a static rule and is honest about its limits: it only judges
/// keys it can resolve to a compile-time constant, in the same compilation as
/// the brick that declares the contract. That leaves two live gaps this harness
/// closes — keys emitted from a BASE class helper (the brick's own source never
/// mentions them), and keys emitted through a non-literal expression. Reflection
/// over a real execution sees what was actually produced, whatever produced it.
/// </summary>
public sealed class BrickContractTests
{
    /// <summary>Assemblies whose bricks are covered by the reflection scan.</summary>
    private static readonly Assembly[] BrickAssemblies =
    [
        typeof(GenerativeArtifactBrick).Assembly,
        typeof(SqlAgentProfileFactory).Assembly,
        typeof(Nexo.Infrastructure.Execution.BrickRegistry).Assembly
    ];

    [Fact]
    public void Every_reachable_brick_declares_a_well_formed_interface()
    {
        var constructed = ConstructibleBricks().ToArray();

        // Guards vacuity: a scan that constructs nothing would otherwise "pass"
        // while asserting precisely nothing.
        constructed.Should().NotBeEmpty("the scan must actually reach some bricks");

        foreach (var brick in constructed)
        {
            var name = brick.GetType().Name;

            foreach (var input in brick.Interface.Inputs)
            {
                input.Name.Should().NotBeNullOrWhiteSpace($"{name} declares an input with no name");
                input.Type.Should().NotBeNullOrWhiteSpace($"{name} input '{input.Name}' declares no type");
            }

            foreach (var output in brick.Interface.Outputs)
            {
                output.Name.Should().NotBeNullOrWhiteSpace($"{name} declares an output with no name");
                output.Type.Should().NotBeNullOrWhiteSpace($"{name} output '{output.Name}' declares no type");
            }

            brick.Interface.Inputs.Select(i => i.Name).Should().OnlyHaveUniqueItems(
                $"{name} declares a duplicate input name, so one declaration is unreachable");
            brick.Interface.Outputs.Select(o => o.Name).Should().OnlyHaveUniqueItems(
                $"{name} declares a duplicate output name, so one declaration is unreachable");
        }
    }

    [Fact]
    public async Task Executed_brick_emits_only_outputs_it_declares_deterministic_path()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoGenerativeAgents();
        services.AddNexoSqlAgentProfile();
        await using var sp = services.BuildServiceProvider();

        var brick = sp.GetRequiredService<GenerativeArtifactBrick>();
        var input = new BrickInput();
        input.Set("target", SqlAgentProfileFactory.TargetId);
        input.Set("grounding", "events");

        var output = await brick.ExecuteAsync(
            input, ImplementationType.Deterministic, new ScanContext(), CancellationToken.None);

        // This is the check the analyzer structurally cannot make: the provenance
        // key is emitted by GenerativeBrick.EmitProvenance in a BASE class, so no
        // literal for it appears in GenerativeArtifactBrick's own source.
        AssertOutputsAreDeclared(brick, output);
        output.ToDictionary().Keys.Should().Contain(GenerativeBrick.ProvenanceOutputKey,
            "the base-class-emitted key must genuinely be present, or this test proves nothing");
    }

    [Fact]
    public async Task Executed_brick_emits_only_outputs_it_declares_model_path_with_deployment()
    {
        // The model path with deployment AND human review emits the widest key set
        // the brick can produce, so it is the one most likely to grow a key nobody
        // declared. Anything narrower would leave deploymentResult/humanApproved
        // unchecked — which is exactly where the last undeclared output appeared.
        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "contract-scan",
            Drafter = new FixedDrafter(),
            Tunables = new GenerationTunables { PreferDeterministic = false },
            Capabilities = new AgentProfileCapabilities { SupportsDeployment = true },
            Deployment = new FakeDeploymentTarget(
                new FakeManagedFileSet(),
                Scripted<DeploymentSmokeResult>.Always(DeploymentSmokeResult.Pass("ok")))
        });

        var brick = new GenerativeArtifactBrick(
            registry,
            logger: null,
            resilience: null,
            approvalGate: new RecordingApprovalGate(ApprovalResult.Approved));

        var input = new BrickInput();
        input.Set("target", "contract-scan");

        var output = await brick.ExecuteAsync(
            input, ImplementationType.Agentic, new ScanContext(), CancellationToken.None);

        var emitted = output.ToDictionary().Keys;
        emitted.Should().Contain("deploymentResult");
        emitted.Should().Contain("humanApproved",
            "the widest key set must actually be exercised, or this test proves nothing");

        AssertOutputsAreDeclared(brick, output);
    }

    /// <summary>
    /// Asserts every key the brick actually emitted appears in its declared
    /// <see cref="BrickInterface.Outputs"/>.
    /// </summary>
    private static void AssertOutputsAreDeclared(DomainBrick brick, BrickOutput output)
    {
        var declared = brick.Interface.Outputs.Select(o => o.Name).ToHashSet(StringComparer.Ordinal);
        var emitted = output.ToDictionary().Keys.ToArray();

        emitted.Should().OnlyContain(
            key => declared.Contains(key),
            $"{brick.GetType().Name} emitted {{{string.Join(", ", emitted)}}} but declares "
            + $"{{{string.Join(", ", declared.OrderBy(d => d, StringComparer.Ordinal))}}}; an "
            + "undeclared output is invisible to anything binding against the published contract");
    }

    /// <summary>
    /// Every concrete brick in <see cref="BrickAssemblies"/> that can be brought
    /// into existence — via DI where it is registered, otherwise via a
    /// parameterless constructor.
    /// </summary>
    private static IEnumerable<DomainBrick> ConstructibleBricks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoGenerativeAgents();
        services.AddNexoSqlAgentProfile();
        using var sp = services.BuildServiceProvider();

        foreach (var type in BrickAssemblies
                     .SelectMany(SafeTypes)
                     .Where(t => t is { IsAbstract: false, IsClass: true, ContainsGenericParameters: false })
                     .Where(t => typeof(DomainBrick).IsAssignableFrom(t))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            DomainBrick? brick = null;
            try
            {
                brick = sp.GetService(type) as DomainBrick
                        ?? (type.GetConstructor(Type.EmptyTypes) is not null
                            ? Activator.CreateInstance(type) as DomainBrick
                            : null);
            }
            catch (Exception)
            {
                // A brick that needs collaborators this harness cannot supply is
                // out of reach for the scan; the analyzer still covers it statically.
                brick = null;
            }

            if (brick is not null)
                yield return brick;
        }
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private sealed class FixedDrafter : IArtifactDrafter
    {
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedArtifact { Content = "DRAFTED" });
    }

    private sealed class ScanContext : IExecutionContext
    {
        public string AgentId => "contract-scan";
        public string BehaviorId => "contract-scan";
        public bool IsAirGapped => false;
        public bool AuditMode => false;
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
