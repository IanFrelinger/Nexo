using Nexo.Core.Application.Enums;
using Nexo.Shared.Interfaces;

namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Represents a volume mount configuration used for mapping host directories or files into a container.
    /// </summary>
    public sealed class VolumeMount
    {
        public string HostPath { get; set; } = string.Empty;
        public string ContainerPath { get; set; } = string.Empty;
        public MountType Type { get; set; }
        public bool ReadOnly { get; set; }

        public VolumeMount() { }
        public VolumeMount(string hostPath, string containerPath, MountType type, bool readOnly = false)
        {
            HostPath = hostPath;
            ContainerPath = containerPath;
            Type = type;
            ReadOnly = readOnly;
        }
    }
}