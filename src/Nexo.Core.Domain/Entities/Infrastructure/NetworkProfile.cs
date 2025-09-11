using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    public class NetworkProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public double Bandwidth { get; set; }
        public double Latency { get; set; }
        
        /// <summary>
        /// Whether the network connection is reliable
        /// </summary>
        public bool IsReliable { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
