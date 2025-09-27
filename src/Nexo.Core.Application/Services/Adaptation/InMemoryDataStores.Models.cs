using System;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// Data models and summaries for in-memory data stores
    /// </summary>
    public partial class InMemoryDataStores
    {
        // Models are defined in this partial class
    }

    /// <summary>
    /// Performance data summary record
    /// </summary>
    public record PerformanceDataSummary
    {
        public int TotalRecords { get; init; }
        public double AverageValue { get; init; }
        public double MinValue { get; init; }
        public double MaxValue { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
    }
}
