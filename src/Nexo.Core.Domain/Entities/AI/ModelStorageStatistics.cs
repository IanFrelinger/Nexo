using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Statistics about model storage usage
    /// </summary>
    public class ModelStorageStatistics
    {
        public long TotalSizeBytes { get; set; }
        public int TotalModels { get; set; }
        public int CachedModels { get; set; }
        public int AvailableModels { get; set; }
        public long AvailableSpaceBytes { get; set; }
        public Dictionary<PlatformType, PlatformStorageStats> PlatformStats { get; set; } = new();
        public Dictionary<ModelPrecision, PrecisionStorageStats> PrecisionStats { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Storage statistics for a specific platform
    /// </summary>
    public class PlatformStorageStats
    {
        public PlatformType Platform { get; set; }
        public long SizeBytes { get; set; }
        public int ModelCount { get; set; }
        public long AvailableSpaceBytes { get; set; }
        public double UsagePercentage { get; set; }
    }

    /// <summary>
    /// Storage statistics for a specific precision level
    /// </summary>
    public class PrecisionStorageStats
    {
        public ModelPrecision Precision { get; set; }
        public long SizeBytes { get; set; }
        public int ModelCount { get; set; }
        public double AverageSizeBytes { get; set; }
    }
}
