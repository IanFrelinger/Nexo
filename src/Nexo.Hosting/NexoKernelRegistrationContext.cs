using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Hosting;

/// <summary>Tuple-style context passed sequentially through <see cref="NexoKernelRegistrar"/> phase methods.</summary>
internal readonly record struct NexoKernelRegistrationContext(
    IServiceCollection Services,
    NexoHostingOptions Options,
    ModuleSelection Modules,
    IConfiguration Configuration);
