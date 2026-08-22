namespace Ashlar.Core.Application.Interfaces;

/// <summary>
/// Runs application commands through pre/post validation and loop orchestration.
/// </summary>
public interface IOrchestrator
{
    /// <summary>Executes a command, running registered validators before and after.</summary>
    ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(
        ICommand<TIn, TOut> command, TIn input, CancellationToken ct);
}
