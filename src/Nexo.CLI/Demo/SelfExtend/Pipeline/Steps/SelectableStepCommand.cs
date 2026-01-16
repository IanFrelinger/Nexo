using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// A step that can swap implementations at runtime (e.g., prefer LLM, fall back to deterministic).
/// Never throws unless all implementations fail.
/// </summary>
public sealed class SelectableStepCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly string _stepId;
    private readonly IReadOnlyList<(string kind, ICommand<SelfExtendContext, SelfExtendContext> cmd)> _impls;
    private readonly ILogger<SelectableStepCommand> _logger;

    public SelectableStepCommand(
        string stepId,
        IReadOnlyList<(string kind, ICommand<SelfExtendContext, SelfExtendContext> cmd)> implementations,
        ILogger<SelectableStepCommand> logger)
    {
        _stepId = stepId;
        _impls = implementations;
        _logger = logger;
    }

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        Exception? last = null;

        foreach (var (kind, cmd) in _impls)
        {
            try
            {
                input.History.Add(new { step = _stepId, impl = kind, phase = "attempt" });
                var result = await cmd.ExecuteAsync(input, ct);
                input.History.Add(new { step = _stepId, impl = kind, phase = "ok" });
                return result;
            }
            catch (Exception ex)
            {
                last = ex;
                _logger.LogWarning(ex, "Step {StepId} impl {Impl} failed; trying next impl", _stepId, kind);
                input.History.Add(new { step = _stepId, impl = kind, phase = "failed", error = ex.Message });
                // continue to next impl
            }
        }

        // No fallback succeeded.
        throw last ?? new InvalidOperationException($"No implementations available for step '{_stepId}'");
    }
}

