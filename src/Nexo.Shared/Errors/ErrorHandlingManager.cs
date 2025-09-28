using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Shared.Errors
{
    /// <summary>
    /// Centralized error handling system
    /// </summary>
    public static class ErrorHandlingManager
    {
        private static readonly Dictionary<Type, Func<Exception, ErrorResult>> _errorHandlers = new();
        private static readonly List<IErrorHandler> _globalHandlers = new();

        /// <summary>
        /// Registers an error handler for a specific exception type
        /// </summary>
        public static void RegisterHandler<TException>(Func<TException, ErrorResult> handler) 
            where TException : Exception
        {
            _errorHandlers[typeof(TException)] = ex => handler((TException)ex);
        }

        /// <summary>
        /// Registers a global error handler
        /// </summary>
        public static void RegisterGlobalHandler(IErrorHandler handler)
        {
            _globalHandlers.Add(handler);
        }

        /// <summary>
        /// Handles an exception and returns a standardized error result
        /// </summary>
        public static ErrorResult HandleException(Exception exception, string? context = null, Dictionary<string, object>? metadata = null)
        {
            var errorResult = new ErrorResult
            {
                Exception = exception,
                ErrorMessage = exception.Message,
                ErrorType = exception.GetType().Name,
                Context = context ?? string.Empty,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Try to find a specific handler
            var exceptionType = exception.GetType();
            while (exceptionType != null)
            {
                if (_errorHandlers.TryGetValue(exceptionType, out var handler))
                {
                    var handledResult = handler(exception);
                    if (handledResult != null)
                    {
                        return handledResult;
                    }
                }
                exceptionType = exceptionType.BaseType;
            }

            // Apply global handlers
            foreach (var globalHandler in _globalHandlers)
            {
                try
                {
                    globalHandler.Handle(exception, errorResult);
                }
                catch
                {
                    // Don't let error handlers break the error handling
                }
            }

            return errorResult;
        }

        /// <summary>
        /// Handles an exception asynchronously
        /// </summary>
        public static async Task<ErrorResult> HandleExceptionAsync(Exception exception, string? context = null, Dictionary<string, object>? metadata = null)
        {
            var errorResult = HandleException(exception, context, metadata);

            // Apply async global handlers
            foreach (var globalHandler in _globalHandlers.OfType<IAsyncErrorHandler>())
            {
                try
                {
                    await globalHandler.HandleAsync(exception, errorResult);
                }
                catch
                {
                    // Don't let error handlers break the error handling
                }
            }

            return errorResult;
        }

        /// <summary>
        /// Clears all error handlers
        /// </summary>
        public static void ClearHandlers()
        {
            _errorHandlers.Clear();
            _globalHandlers.Clear();
        }
    }
}
