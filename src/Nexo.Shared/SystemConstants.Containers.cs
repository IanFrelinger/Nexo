using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Container runtime names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class ContainerRuntimes
        {
            public const string Docker = "docker";
            public const string Podman = "podman";
            public const string Containerd = "containerd";
            public const string CRIO = "cri-o";
            public const string LXC = "lxc";
            public const string LXD = "lxd";
            public const string Runc = "runc";
            public const string None = "none";

            /// <summary>
            /// Gets all container runtime variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Docker, Podman, Containerd, CRIO, LXC, LXD, Runc, None,
                "DOCKER", "PODMAN", "CONTAINERD", "CRI-O", "LXC", "LXD", "RUNC"
            };

            /// <summary>
            /// Tries to match a container runtime name case-insensitively.
            /// </summary>
            /// <param name="runtimeName">The container runtime name to match.</param>
            /// <returns>The standardized container runtime name or None if not found.</returns>
            public static string MatchContainerRuntime(string runtimeName)
            {
                if (string.IsNullOrWhiteSpace(runtimeName))
                    return None;

                var normalizedName = runtimeName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return None;
            }
        }
    }
}
