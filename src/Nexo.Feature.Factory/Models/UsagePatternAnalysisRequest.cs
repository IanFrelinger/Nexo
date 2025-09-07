using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Usage pattern analysis request
/// </summary>
public record UsagePatternAnalysisRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? UserId { get; init; }
    public List<string> PatternTypes { get; init; } = new();
    public bool EnablePredictiveAnalysis { get; init; }
    public bool EnableAnomalyDetection { get; init; }
    public Dictionary<string, object> AnalysisParameters { get; init; } = new();
} 