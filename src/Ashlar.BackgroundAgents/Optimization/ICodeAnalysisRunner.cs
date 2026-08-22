namespace Ashlar.BackgroundAgents.Optimization;

/// <summary>
/// Abstraction for running code analysis from a background agent (e.g. post-deployment optimizer).
/// The host supplies an implementation that calls the application's analysis pipeline (e.g. IAnalysisService).
/// Used to dog-food: the framework runs optimizer agents that analyze its own codebase.
/// </summary>
public interface ICodeAnalysisRunner
{
    /// <summary>
    /// Run code analysis on the given path.
    /// </summary>
    /// <param name="path">Directory path to analyze (e.g. repo root or project path).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary result for logging and agent use.</returns>
    Task<CodeAnalysisRunResult> RunAsync(string path, CancellationToken cancellationToken = default);
}
