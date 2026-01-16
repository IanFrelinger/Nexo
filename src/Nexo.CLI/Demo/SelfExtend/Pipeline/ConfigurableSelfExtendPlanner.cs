using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

/// <summary>
/// Planner driven by a runtime spec (stages + step IDs), with optional LLM decision.
/// </summary>
public sealed class ConfigurableSelfExtendPlanner : ISelfExtendPlanner
{
    private readonly SelfExtendRuntimeConfig _cfg;
    private readonly ISelfExtendStepFactory _steps;
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ConfigurableSelfExtendPlanner> _logger;
    private int _stageIndex;

    public ConfigurableSelfExtendPlanner(
        SelfExtendRuntimeConfig cfg,
        ISelfExtendStepFactory steps,
        IProviderFactory providerFactory,
        ILogger<ConfigurableSelfExtendPlanner> logger)
    {
        _cfg = cfg;
        _steps = steps;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public IReadOnlyList<ICommand<SelfExtendContext, SelfExtendContext>> Next(SelfExtendContext ctx)
    {
        if (_cfg.Stages.Count == 0) return Array.Empty<ICommand<SelfExtendContext, SelfExtendContext>>();
        if (_stageIndex >= _cfg.Stages.Count) return Array.Empty<ICommand<SelfExtendContext, SelfExtendContext>>();

        var chosen = _cfg.PlannerUsesLlm
            ? ChooseStageWithLlm(ctx) ?? ChooseNextStageDeterministic(ctx)
            : ChooseNextStageDeterministic(ctx);

        if (chosen == null) return Array.Empty<ICommand<SelfExtendContext, SelfExtendContext>>();
        _stageIndex = chosen.Value + 1;

        var stage = _cfg.Stages[chosen.Value];
        var cmds = new List<ICommand<SelfExtendContext, SelfExtendContext>>();
        foreach (var id in stage.StepIds)
        {
            if (!_cfg.Steps.TryGetValue(id, out var spec))
            {
                throw new InvalidOperationException($"Stage '{stage.Name}' references unknown step id '{id}'");
            }
            cmds.Add(_steps.Create(id, spec));
        }
        return cmds;
    }

    private int? ChooseNextStageDeterministic(SelfExtendContext ctx)
    {
        for (var i = _stageIndex; i < _cfg.Stages.Count; i++)
        {
            var s = _cfg.Stages[i];
            var when = (s.When ?? "always").Trim().ToLowerInvariant();
            var ok = when switch
            {
                "always" => true,
                "if_last_test_failed" => ctx.LastTestOk == false,
                "if_last_test_ok" => ctx.LastTestOk == true,
                _ => true
            };
            if (ok) return i;
        }
        return null;
    }

    private int? ChooseStageWithLlm(SelfExtendContext ctx)
    {
        try
        {
            var provider = (_cfg.Provider ?? "offline").Trim();
            if (!_providerFactory.IsProviderAvailable(provider)) return null;

            var system = """
You decide which stage index (0-based) the self-extend demo should run next.
Return ONLY a single integer.
""";

            var user = $"""
Stages (0-based, in order):
{string.Join("\n", _cfg.Stages.Select((s, i) => $"{i}: {s.Name} (when={s.When})"))}

Context:
- lastTestOk: {ctx.LastTestOk?.ToString() ?? "null"}

Pick the next stage index to run, or -1 to stop.
""";

            // Blocking call is acceptable here because planner is not performance critical.
            var raw = _providerFactory.ExecuteLLMAsync(provider, system, user, config: new { }, CancellationToken.None)
                .GetAwaiter().GetResult();

            if (!int.TryParse(raw.Trim(), out var idx)) return null;
            if (idx < 0) return null;
            if (idx >= _cfg.Stages.Count) return null;
            return idx;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM planner decision failed; falling back to deterministic planner");
            return null;
        }
    }
}

