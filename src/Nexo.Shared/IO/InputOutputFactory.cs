using System;
using System.Collections.Generic;

namespace Nexo.Shared.IO
{
    /// <summary>
    /// Factory for creating standardized inputs and outputs
    /// </summary>
    public static class InputOutputFactory
    {
        /// <summary>
        /// Creates a standardized input with common properties
        /// </summary>
        public static TInput CreateInput<TInput>(Dictionary<string, object>? parameters = null, string? context = null) 
            where TInput : BaseInput, new()
        {
            var input = new TInput();
            input.Parameters = parameters ?? new Dictionary<string, object>();
            input.Context = context ?? string.Empty;
            input.Timestamp = DateTime.UtcNow;
            input.CorrelationId = Guid.NewGuid().ToString();
            return input;
        }

        /// <summary>
        /// Creates a standardized output with common properties
        /// </summary>
        public static TOutput CreateOutput<TOutput>(object? data = null, Dictionary<string, object>? metadata = null) 
            where TOutput : BaseOutput, new()
        {
            var output = new TOutput();
            output.Data = data;
            output.Metadata = metadata ?? new Dictionary<string, object>();
            output.Timestamp = DateTime.UtcNow;
            output.ExecutionId = Guid.NewGuid().ToString();
            return output;
        }
    }
}
