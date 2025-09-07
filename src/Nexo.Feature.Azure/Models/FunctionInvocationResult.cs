using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function invocation result
/// </summary>
public record FunctionInvocationResult
{
    public string FunctionName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public object? Response { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public TimeSpan ExecutionTime { get; init; }
    public DateTime InvokedAt { get; init; }
} 