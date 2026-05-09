using System.Runtime.InteropServices;

namespace Nexo.Infrastructure.ModelArtifacts;

internal static class DockerEngineUriResolver
{
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
