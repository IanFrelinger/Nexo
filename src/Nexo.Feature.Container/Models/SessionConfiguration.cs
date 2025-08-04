using System;
using System.Collections.Generic;

namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Represents configuration settings for initializing and managing a development session within a containerized environment.
    /// </summary>
    public sealed class SessionConfiguration
    {
        public string ProjectPath { get; set; }
        public string ContainerImage { get; set; }
        public string WorkingDirectory { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
        public List<VolumeMount> VolumeMounts { get; set; }
        public List<PortMapping> PortMappings { get; set; }
        public List<string> AdditionalPackages { get; set; }
        public SessionOptions Options { get; set; }
        public string SessionName { get; set; }

        public SessionConfiguration()
        {
            EnvironmentVariables = new Dictionary<string, string>();
            VolumeMounts = new List<VolumeMount>();
            PortMappings = new List<PortMapping>();
            AdditionalPackages = new List<string>();
        }
    }
}