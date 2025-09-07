using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Trend forecast
/// </summary>
public record TrendForecast
{
    public DateTime ForecastDate { get; init; }
    public double PredictedProductivityMultiplier { get; init; }
    public double ConfidenceLevel { get; init; }
    public string ForecastMethod { get; init; } = string.Empty;
} 