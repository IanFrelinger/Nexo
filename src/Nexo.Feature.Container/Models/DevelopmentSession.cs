using System;
using System.Collections.Generic;
using Nexo.Core.Application.Enums;
using Nexo.Shared.Interfaces;

namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Represents a containerized development session, including its metadata and configuration details.
    /// </summary>
    public sealed class DevelopmentSession
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public SessionConfiguration Configuration { get; set; } = new SessionConfiguration();
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public SessionStatus Status { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<string> MountedVolumes { get; set; }
        public List<int> ExposedPorts { get; set; }

        public DevelopmentSession()
        {
            Metadata = new Dictionary<string, object>();
            MountedVolumes = new List<string>();
            ExposedPorts = new List<int>();
        }
    }
}