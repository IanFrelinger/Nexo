namespace Nexo.Core.Application.Interfaces;

public interface IOrchestrator
{
    ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(
        ICommand<TIn, TOut> command, TIn input, CancellationToken ct);
}

public sealed record OrchestrationResult(bool Success, string? Message = null);
