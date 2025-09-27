using System;
using Nexo.Core.Domain.Enums.FeatureFactory;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public partial class Alert
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
