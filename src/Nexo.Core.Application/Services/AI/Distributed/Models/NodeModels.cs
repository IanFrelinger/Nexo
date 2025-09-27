using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.Distributed.Models
{
    /// <summary>
    /// Node registration request
    /// </summary>
    public class NodeRegistrationRequest
    {
        public string NodeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public NodeResourceInfo ResourceInfo { get; set; } = new();
        public NodeLocation Location { get; set; } = new();
    }

    /// <summary>
    /// Processing node
    /// </summary>
    public class ProcessingNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public NodeStatus Status { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public NodeResourceInfo ResourceInfo { get; set; } = new();
        public NodeLocation Location { get; set; } = new();
    }

    /// <summary>
    /// Node resource information
    /// </summary>
    public class NodeResourceInfo
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int AvailableCores { get; set; }
        public long AvailableMemory { get; set; }
        public long AvailableDisk { get; set; }
    }

    /// <summary>
    /// Node location
    /// </summary>
    public class NodeLocation
    {
        public string Region { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string DataCenter { get; set; } = string.Empty;
        public GeoLocation? GeoLocation { get; set; }
    }

    /// <summary>
    /// Geographic location
    /// </summary>
    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
