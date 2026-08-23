using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Optimization;
using Ashlar.Core.Application.Analysis.Ports;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Dog-fooded implementation of ICodeAnalysisRunner: runs the application's own analysis pipeline
/// (IAnalysisService / AnalyzeCodeCommand) so optimizer background agents can analyze the codebase.
/// </summary>
public sealed class CodeAnalysisRunnerAdapter : ICodeAnalysisRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CodeAnalysisRunnerAdapter> _logger;

    public CodeAnalysisRunnerAdapter(
        IServiceScopeFactory scopeFactory,
        ILogger<CodeAnalysisRunnerAdapter> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CodeAnalysisRunResult> RunAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!BackgroundAgentAdapterValidation.TryResolveDirectory(path, "Path", out var errorMessage))
        {
            return new CodeAnalysisRunResult(false, 0, errorMessage!);
        }

        var dir = new DirectoryInfo(path!);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var analysisService = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
            var result = await analysisService.AnalyzeAsync(dir, null, cancellationToken).ConfigureAwait(false);

            var success = !result.HasViolations;
            var summary = result.TotalViolations == 0
                ? "No violations."
                : $"{result.TotalViolations} violation(s) found.";
            _logger.LogDebug("Code analysis at {Path}: {Summary}", path, summary);
            return new CodeAnalysisRunResult(success, result.TotalViolations, summary);
        }
        catch (Exception ex)
        {
            return new CodeAnalysisRunResult(false, 0, BackgroundAgentAdapterFailure.LogAndMessage(_logger, ex, $"Analysis failed: {ex.Message}"));
        }
    }
}
