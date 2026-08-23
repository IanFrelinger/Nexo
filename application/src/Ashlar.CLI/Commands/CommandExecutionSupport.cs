using Microsoft.Extensions.Logging;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.CLI.Commands;

/// <summary>Command execution support.</summary>
internal static class CommandExecutionSupport
{
    internal static Progress<ProgressReport>? CreateProgressReporter(
        bool verbose,
        bool json,
        ILogger logger,
        IConsoleRenderer renderer)
    {
        if (!verbose && json)
            return null;

        return new Progress<ProgressReport>(report =>
        {
            if (json)
            {
                logger.LogInformation(
                    "Progress: {Percentage}% - {Message}",
                    report.Percentage,
                    report.Message);
                return;
            }

            renderer.RenderProgress(report);
        });
    }

    internal static int RenderDomainFailure(
        ILogger logger,
        IConsoleRenderer renderer,
        Exception exception,
        string logMessage,
        int exitCode)
    {
        logger.LogError(exception, logMessage);
        var code = exception switch
        {
            Ashlar.Core.Domain.Exceptions.AnalysisException analysis => analysis.ErrorCode,
            Ashlar.Core.Domain.Exceptions.ValidationException validation => validation.ErrorCode,
            _ => null
        };
        var suggestion = exception switch
        {
            Ashlar.Core.Domain.Exceptions.AnalysisException analysis => analysis.Suggestion,
            Ashlar.Core.Domain.Exceptions.ValidationException validation => validation.Suggestion,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(code))
            renderer.RenderErrorWithCode(exception.Message, code!, suggestion);
        else
            renderer.RenderError(exception.Message);

        return exitCode;
    }
}
