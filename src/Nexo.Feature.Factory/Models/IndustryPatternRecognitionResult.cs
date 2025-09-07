using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Industry pattern recognition result
/// </summary>
public record IndustryPatternRecognitionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<IndustryPattern> RecognizedPatterns { get; init; } = new();
    public List<IndustryTrend> IndustryTrends { get; init; } = new();
    public int PatternsRecognized { get; init; }
    public int NewPatternsDiscovered { get; init; }
    public double RecognitionAccuracy { get; init; }
    public TimeSpan RecognitionDuration { get; init; }
    public Dictionary<string, double> RecognitionMetrics { get; init; } = new();
    public DateTime RecognizedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 