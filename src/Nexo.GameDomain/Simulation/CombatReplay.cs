using Nexo.GameDomain.Rules.Combat;

namespace Nexo.GameDomain.Simulation;

/// <summary>
/// Portable deterministic replay inputs. Presentation and physics transforms are
/// deliberately excluded from the authoritative rules replay.
/// </summary>
public sealed class CombatReplay
{
    /// <summary>Creates a replay capture.</summary>
    public CombatReplay(
        IEnumerable<CombatantDefinition> combatants,
        IEnumerable<ScheduledDamageCommand> commands,
        uint finalTickExclusive)
    {
        if (combatants is null)
            throw new ArgumentNullException(nameof(combatants));
        if (commands is null)
            throw new ArgumentNullException(nameof(commands));

        Combatants = combatants.OrderBy(c => c.Id).ToArray();
        Commands = commands
            .Where(c => c.Tick < finalTickExclusive)
            .ToArray();
        FinalTickExclusive = finalTickExclusive;
    }

    /// <summary>Initial simulation entities.</summary>
    public IReadOnlyList<CombatantDefinition> Combatants { get; }

    /// <summary>Commands in their original submission order.</summary>
    public IReadOnlyList<ScheduledDamageCommand> Commands { get; }

    /// <summary>Tick reached by the captured simulation.</summary>
    public uint FinalTickExclusive { get; }

    /// <summary>Runs the replay to its captured tick.</summary>
    public CombatSimulation Run()
    {
        var simulation = new CombatSimulation(Combatants);
        foreach (var scheduled in Commands)
        {
            DamageCommand command = scheduled.Command;
            simulation.ScheduleDamage(scheduled.Tick, command);
        }

        while (simulation.CurrentTick < FinalTickExclusive)
            simulation.Step();

        return simulation;
    }
}
