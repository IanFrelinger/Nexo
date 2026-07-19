using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Rules.Combat;

namespace Nexo.GameDomain.Simulation;

/// <summary>Initial authoritative state for a combatant.</summary>
public readonly struct CombatantDefinition
{
    /// <summary>Creates a combatant definition.</summary>
    public CombatantDefinition(EntityId id, int maximumHealth, int armor)
    {
        if (maximumHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumHealth));
        if (armor < 0)
            throw new ArgumentOutOfRangeException(nameof(armor));

        Id = id;
        MaximumHealth = maximumHealth;
        Armor = armor;
    }

    /// <summary>Stable combatant id.</summary>
    public EntityId Id { get; }

    /// <summary>Initial and maximum health.</summary>
    public int MaximumHealth { get; }

    /// <summary>Flat armor value.</summary>
    public int Armor { get; }
}

/// <summary>Current authoritative combatant state.</summary>
public sealed class CombatantState
{
    internal CombatantState(CombatantDefinition definition)
    {
        Id = definition.Id;
        MaximumHealth = definition.MaximumHealth;
        Health = definition.MaximumHealth;
        Armor = definition.Armor;
    }

    /// <summary>Stable combatant id.</summary>
    public EntityId Id { get; }

    /// <summary>Maximum health.</summary>
    public int MaximumHealth { get; }

    /// <summary>Current health.</summary>
    public int Health { get; internal set; }

    /// <summary>Current flat armor.</summary>
    public int Armor { get; internal set; }

    /// <summary>Whether health has reached zero.</summary>
    public bool Eliminated => Health == 0;
}

/// <summary>A damage command assigned to an authoritative simulation tick.</summary>
public readonly struct ScheduledDamageCommand
{
    /// <summary>Creates a scheduled command.</summary>
    public ScheduledDamageCommand(uint tick, DamageCommand command)
    {
        Tick = tick;
        Command = command;
    }

    /// <summary>Tick on which the server processes the command.</summary>
    public uint Tick { get; }

    /// <summary>Typed command payload.</summary>
    public DamageCommand Command { get; }
}

/// <summary>Authoritative combat event emitted by the simulation.</summary>
public readonly struct CombatEvent
{
    /// <summary>Creates a combat event.</summary>
    public CombatEvent(
        uint tick,
        ulong sequence,
        EntityId source,
        EntityId target,
        DamageResult result)
    {
        Tick = tick;
        Sequence = sequence;
        Source = source;
        Target = target;
        Result = result;
    }

    /// <summary>Simulation tick that emitted the event.</summary>
    public uint Tick { get; }

    /// <summary>Monotonic server event sequence.</summary>
    public ulong Sequence { get; }

    /// <summary>Damage source.</summary>
    public EntityId Source { get; }

    /// <summary>Damage target.</summary>
    public EntityId Target { get; }

    /// <summary>Resolved authoritative result.</summary>
    public DamageResult Result { get; }
}

/// <summary>
/// Minimal fixed-tick authoritative combat simulation. Network transports submit
/// typed commands; the simulation owns state and emits ordered events.
/// </summary>
public sealed class CombatSimulation
{
    private readonly IReadOnlyList<CombatantDefinition> _initialDefinitions;
    private readonly Dictionary<EntityId, CombatantState> _combatants;
    private readonly SortedDictionary<uint, List<DamageCommand>> _commands = new();
    private readonly List<ScheduledDamageCommand> _recordedCommands = new();
    private readonly List<CombatEvent> _events = new();
    private ulong _nextEventSequence = 1;

    /// <summary>Creates a simulation at tick zero.</summary>
    public CombatSimulation(IEnumerable<CombatantDefinition> combatants)
    {
        if (combatants is null)
            throw new ArgumentNullException(nameof(combatants));

        var definitions = combatants.OrderBy(c => c.Id).ToArray();
        if (definitions.Length == 0)
            throw new ArgumentException("At least one combatant is required.", nameof(combatants));

        _initialDefinitions = definitions;
        _combatants = new Dictionary<EntityId, CombatantState>();
        foreach (var definition in definitions)
        {
            if (_combatants.ContainsKey(definition.Id))
                throw new ArgumentException($"Duplicate combatant '{definition.Id}'.", nameof(combatants));
            _combatants.Add(definition.Id, new CombatantState(definition));
        }
    }

    /// <summary>Tick that will be processed by the next <see cref="Step"/> call.</summary>
    public uint CurrentTick { get; private set; }

    /// <summary>Ordered events emitted so far.</summary>
    public IReadOnlyList<CombatEvent> Events => _events;

    /// <summary>Returns authoritative state for an entity.</summary>
    public CombatantState GetCombatant(EntityId id)
        => _combatants.TryGetValue(id, out var state)
            ? state
            : throw new KeyNotFoundException($"Combatant '{id}' is not registered.");

    /// <summary>Schedules a command for the given server tick.</summary>
    public void ScheduleDamage(uint tick, in DamageCommand command)
    {
        if (tick < CurrentTick)
            throw new InvalidOperationException($"Cannot schedule past tick {tick}; current tick is {CurrentTick}.");
        if (!_combatants.ContainsKey(command.Target))
            throw new KeyNotFoundException($"Target combatant '{command.Target}' is not registered.");

        if (!_commands.TryGetValue(tick, out var commands))
        {
            commands = new List<DamageCommand>();
            _commands.Add(tick, commands);
        }

        commands.Add(command);
        _recordedCommands.Add(new ScheduledDamageCommand(tick, command));
    }

    /// <summary>Processes one fixed simulation tick.</summary>
    public void Step()
    {
        if (_commands.TryGetValue(CurrentTick, out var commands))
        {
            foreach (var command in commands)
                ApplyDamage(command);
            _commands.Remove(CurrentTick);
        }

        CurrentTick = checked(CurrentTick + 1);
    }

    /// <summary>Processes ticks through and including <paramref name="tick"/>.</summary>
    public void RunThrough(uint tick)
    {
        while (CurrentTick <= tick)
            Step();
    }

    /// <summary>Captures replay inputs from the initial state.</summary>
    public CombatReplay CreateReplay()
        => new(_initialDefinitions, _recordedCommands, CurrentTick);

    internal IEnumerable<CombatantState> EnumerateCombatants()
        => _combatants.Values.OrderBy(c => c.Id);

    private void ApplyDamage(in DamageCommand command)
    {
        var target = _combatants[command.Target];
        var context = new DamageContext(target.Health, target.Armor);
        var result = DamageRules.Resolve(command, context);
        target.Health = result.RemainingHealth;
        _events.Add(new CombatEvent(
            CurrentTick,
            _nextEventSequence++,
            command.Source,
            command.Target,
            result));
    }
}
