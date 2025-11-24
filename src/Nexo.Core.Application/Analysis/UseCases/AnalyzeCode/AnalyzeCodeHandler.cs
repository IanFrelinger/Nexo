using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// Handler for analyzing code and assemblies.
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

