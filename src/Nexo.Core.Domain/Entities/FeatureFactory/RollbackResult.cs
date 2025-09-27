using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public partial class RollbackResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
