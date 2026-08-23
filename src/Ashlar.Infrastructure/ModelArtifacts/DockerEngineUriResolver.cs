using System.Runtime.InteropServices;

namespace Ashlar.Infrastructure.ModelArtifacts;

/// <summary>Resolves the Docker engine URI for the current operating system.</summary>
internal static class DockerEngineUriResolver
{
    /// <summary>Returns the platform-specific Docker engine URI.</summary>
    public static Uri Resolve()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new Uri("npipe://./pipe/docker_engine");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new Uri("unix:///var/run/docker.sock");
        }

        throw new PlatformNotSupportedException("Docker engine URI is not defined for this platform.");
    }
}
