using FluentAssertions;
using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Rules.Combat;
using Nexo.GameDomain.Simulation;

namespace Nexo.Tests.GameDomain;

public sealed class CombatSimulationTests
{
    private static readonly EntityId Attacker = new(1);
    private static readonly EntityId Target = new(2);

    [Fact]
    public void Step_ProcessesCommandsAtTheirAuthoritativeTick()
    {
        var simulation = CreateSimulation();
        simulation.ScheduleDamage(2, Command(baseDamage: 100));

        simulation.Step();
        simulation.GetCombatant(Target).Health.Should().Be(100);
        simulation.Step();
        simulation.GetCombatant(Target).Health.Should().Be(100);
        simulation.Step();

        simulation.CurrentTick.Should().Be(3);
        simulation.GetCombatant(Target).Health.Should().Be(20);
        simulation.Events.Should().ContainSingle();
        simulation.Events[0].Tick.Should().Be(2);
        simulation.Events[0].Sequence.Should().Be(1);
    }

    [Fact]
    public void CommandsOnSameTick_PreserveSubmissionOrder()
    {
        var simulation = CreateSimulation();
        simulation.ScheduleDamage(0, Command(baseDamage: 100));
        simulation.ScheduleDamage(0, Command(baseDamage: 100));

        simulation.Step();

        simulation.GetCombatant(Target).Eliminated.Should().BeTrue();
        simulation.Events.Should().HaveCount(2);
        simulation.Events[0].Result.AppliedDamage.Should().Be(80);
        simulation.Events[1].Result.AppliedDamage.Should().Be(20);
        simulation.Events[1].Result.Eliminated.Should().BeTrue();
    }

    [Fact]
    public void Replay_ReproducesEventsAndAuthoritativeStateHash()
    {
        var simulation = CreateSimulation();
        simulation.ScheduleDamage(1, Command(baseDamage: 50));
        simulation.ScheduleDamage(4, Command(baseDamage: 100, critical: true));
        simulation.RunThrough(5);

        var originalHash = CombatStateHasher.Compute(simulation);
        var replayed = simulation.CreateReplay().Run();

        CombatStateHasher.Compute(replayed).Should().Be(originalHash);
        replayed.CurrentTick.Should().Be(simulation.CurrentTick);
        replayed.Events.Select(e => new
            {
                e.Tick,
                e.Sequence,
                e.Result.AppliedDamage,
                e.Result.RemainingHealth
            })
            .Should()
            .Equal(simulation.Events.Select(e => new
            {
                e.Tick,
                e.Sequence,
                e.Result.AppliedDamage,
                e.Result.RemainingHealth
            }));
    }

    [Fact]
    public void ScheduleDamage_RejectsPastCommands()
    {
        var simulation = CreateSimulation();
        simulation.Step();

        var act = () => simulation.ScheduleDamage(0, Command(10));

        act.Should().Throw<InvalidOperationException>().WithMessage("*past tick*");
    }

    private static CombatSimulation CreateSimulation()
        => new(
        [
            new CombatantDefinition(Attacker, maximumHealth: 100, armor: 0),
            new CombatantDefinition(Target, maximumHealth: 100, armor: 20)
        ]);

    private static DamageCommand Command(int baseDamage, bool critical = false)
        => new(Attacker, Target, baseDamage, criticalMultiplierPercent: 150, critical);
}
