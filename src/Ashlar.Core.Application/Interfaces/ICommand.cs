namespace Ashlar.Core.Application.Interfaces;

/// <summary>
/// Application command with typed input and output.
/// Used by <see cref="IOrchestrator"/> to run use-case handlers with validation hooks.
/// </summary>
/// <typeparam name="TIn">Command input type.</typeparam>
/// <typeparam name="TOut">Command output type.</typeparam>
public interface ICommand<in TIn, TOut>
{
    /// <summary>Executes the command with the supplied input.</summary>
    ValueTask<TOut> ExecuteAsync(TIn input, CancellationToken ct);
}
