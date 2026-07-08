using Nexo.VisualVerification;

namespace Nexo.ToyRenderer;

public sealed class ToyEntity
{
    public ToyEntity(int id, double x, double y, byte r, byte g, byte b)
    {
        Id = id;
        X = x;
        Y = y;
        R = r;
        G = g;
        B = b;
        Alive = true;
    }

    public int Id { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public bool Alive { get; set; }
}

/// <summary>Deterministic toy scene driven by scenario seed and scripted inputs.</summary>
public sealed class ToyScene
{
    private readonly Xoshiro256StarStar _rng;
    private readonly Dictionary<int, List<ScriptedInputEvent>> _inputsByStep;
    private readonly List<ToyEntity> _entities = new();
    private int _nextEntityId;
    private bool _paused;

    public ToyScene(ScenarioDefinition scenario)
    {
        FixedTimestepSeconds = scenario.FixedTimestepSeconds;
        TotalSteps = scenario.TotalSteps;
        _rng = new Xoshiro256StarStar(scenario.Seed);
        _inputsByStep = scenario.InputScript
            .GroupBy(e => e.Step)
            .ToDictionary(g => g.Key, g => g.ToList());

        SpawnInitialEntities(3);
    }

    public double FixedTimestepSeconds { get; }

    public int TotalSteps { get; }

    public bool HasExited { get; private set; }

    public IReadOnlyList<ToyEntity> Entities => _entities;

    public void Step(int step)
    {
        if (HasExited || _paused)
            return;

        ApplyInputs(step);
        UpdateEntities();
    }

    private void SpawnInitialEntities(int count)
    {
        for (var i = 0; i < count; i++)
            SpawnEntity();
    }

    private void SpawnEntity()
    {
        var x = _rng.NextDouble();
        var y = _rng.NextDouble();
        var hue = (byte)(_rng.NextULong() % 256);
        _entities.Add(new ToyEntity(_nextEntityId++, x, y, hue, (byte)(255 - hue), (byte)((hue * 3) % 256)));
    }

    private void ApplyInputs(int step)
    {
        if (!_inputsByStep.TryGetValue(step, out var events))
            return;

        foreach (var input in events)
        {
            switch (input.Channel)
            {
                case "spawn":
                    SpawnEntity();
                    break;
                case "despawn":
                    var target = _entities.LastOrDefault(e => e.Alive);
                    if (target is not null)
                        target.Alive = false;
                    break;
                case "pause":
                    _paused = string.Equals(input.Payload, "on", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(input.Payload, "true", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrEmpty(input.Payload);
                    break;
            }
        }
    }

    private void UpdateEntities()
    {
        foreach (var entity in _entities.Where(e => e.Alive))
        {
            var angle = _rng.NextDouble() * Math.PI * 2;
            var speed = 0.05 + (_rng.NextDouble() * 0.02);
            entity.X = Wrap01(entity.X + (Math.Cos(angle) * speed * FixedTimestepSeconds * 60));
            entity.Y = Wrap01(entity.Y + (Math.Sin(angle) * speed * FixedTimestepSeconds * 60));
        }
    }

    private static double Wrap01(double value)
    {
        while (value < 0)
            value += 1;
        while (value >= 1)
            value -= 1;
        return value;
    }
}
