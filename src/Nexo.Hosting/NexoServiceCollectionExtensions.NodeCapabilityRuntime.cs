using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Infrastructure.ModelArtifacts;
using Nexo.Infrastructure.NodeCapabilityRuntime;

namespace Nexo.Hosting;

public static partial class NexoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-specific NCR (Node Capability Runtime) module.
    /// NCR probes local hardware (GPU, RAM, accelerators) and exposes
    /// capabilities used by <c>ICapabilityRouter</c> to decide
    /// whether a job can run locally or must be routed to a peer/cloud.
    /// Falls back to Linux when the OS is not recognised.
    /// Also wires model artifact catalog sources used during NCR/model discovery.
    /// Invoked from <see cref="NexoKernelRegistrar"/> phase 01 when the deployment profile includes NCR.
    /// </summary>
    internal static void RegisterNodeCapabilityRuntime(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNodeCapabilityRuntimeCore(configuration);
        if (OperatingSystem.IsWindows())
        {
            services.AddNodeCapabilityRuntimeWindows(configuration);
        }
        else if (OperatingSystem.IsMacOS())
        {
            services.AddNodeCapabilityRuntimeMacOS(configuration);
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddNodeCapabilityRuntimeLinux(configuration);
        }
        else if (OperatingSystem.IsIOS())
        {
            services.AddNodeCapabilityRuntimeiOS(configuration);
        }
        else if (OperatingSystem.IsAndroid())
        {
            services.AddNodeCapabilityRuntimeAndroid(configuration);
        }
        else
        {
            services.AddNodeCapabilityRuntimeLinux(configuration);
        }

        services.AddModelArtifactCatalog(configuration);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            services.AddDockerOllamaModelArtifactCatalogSource();
        }
    }
}
