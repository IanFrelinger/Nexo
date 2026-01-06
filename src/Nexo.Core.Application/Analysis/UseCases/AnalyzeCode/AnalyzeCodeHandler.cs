using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// MediatR handler for analyzing code and assemblies.
/// 
/// Responsibilities:
/// - Executes code analysis via IAnalysisService
/// - Records execution metrics (duration, violation counts)
/// - Handles errors and exceptions
/// - Logs analysis progress and results
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class AnalyzeCodeHandler : IRequestHandler<AnalyzeCodeCommand, AnalysisResult>
{
    private readonly IAnalysisService _analysisService;
    private readonly ILogger<AnalyzeCodeHandler> _logger;
    private readonly IMetricsCollector? _metricsCollector;

    public AnalyzeCodeHandler(
        IAnalysisService analysisService,
        ILogger<AnalyzeCodeHandler> logger,
        IMetricsCollector? metricsCollector = null)
    {
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Handles the AnalyzeCodeCommand by analyzing the specified directory.
    /// </summary>
    /// <param name="request">Command containing path to analyze and progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with violations and metrics</returns>
    public async Task<AnalysisResult> Handle(
        AnalyzeCodeCommand request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting analysis for path: {Path}",
            request.Path.FullName);

        try
        {
            var result = await _analysisService.AnalyzeAsync(
                request.Path,
                request.Progress,
                cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _metricsCollector?.RecordExecutionTime("Analysis", duration);
            _metricsCollector?.IncrementCounter("Analysis.Executed");
            _metricsCollector?.IncrementCounter("Analysis.Violations", result.TotalViolations);

            _logger.LogInformation(
                "Analysis completed. Found {Count} violation(s) in {Duration}ms",
                result.TotalViolations,
                duration.TotalMilliseconds);

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            _metricsCollector?.IncrementCounter("Analysis.Errors");
            _logger.LogError(ex, "Unauthorized access during analysis");
            throw new AnalysisException(
                $"Unauthorized access to path: {request.Path.FullName}",
                ex);
        }
        catch (Exception ex) when (ex is not AnalysisException)
        {
            _metricsCollector?.IncrementCounter("Analysis.Errors");
            _logger.LogError(ex, "Unexpected error during analysis");
            throw new AnalysisException(
                $"Analysis failed for path: {request.Path.FullName}",
                ex);
        }
    }
}

