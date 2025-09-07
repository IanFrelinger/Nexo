using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Industry pattern recognition request
/// </summary>
public record IndustryPatternRecognitionRequest
{
    public string Industry { get; init; } = string.Empty;
    public List<string> PatternTypes { get; init; } = new();
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool EnableRealTimeRecognition { get; init; }
    public Dictionary<string, object> RecognitionParameters { get; init; } = new();
} 