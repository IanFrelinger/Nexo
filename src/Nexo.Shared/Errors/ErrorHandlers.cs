using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Logging;

namespace Nexo.Shared.Errors
{
    /// <summary>
    /// Error handler interface
    /// </summary>
    public interface IErrorHandler
    {
        void Handle(Exception exception, ErrorResult errorResult);
    }

    /// <summary>
    /// Async error handler interface
    /// </summary>
    public interface IAsyncErrorHandler : IErrorHandler
    {
        Task HandleAsync(Exception exception, ErrorResult errorResult);
    }

    /// <summary>
    /// Logging error handler
    /// </summary>
    public class LoggingErrorHandler : IErrorHandler
    {
        public void Handle(Exception exception, ErrorResult errorResult)
        {
            // Log the error using the centralized logging system
            LoggingManager.LogError(
                errorResult.PrimaryMessage,
                exception,
                errorResult.Metadata,
                errorResult.CorrelationId
            );
        }
    }

    /// <summary>
    /// Metrics error handler
    /// </summary>
    public class MetricsErrorHandler : IErrorHandler
    {
        public void Handle(Exception exception, ErrorResult errorResult)
        {
            // Record error metrics
            errorResult.Metadata["ErrorCount"] = 1;
            errorResult.Metadata["ErrorTimestamp"] = errorResult.Timestamp;
        }
    }
}
