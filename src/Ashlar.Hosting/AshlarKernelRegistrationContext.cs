using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ashlar.Hosting;

/// <summary>Tuple-style context passed sequentially through <see cref="AshlarKernelRegistrar"/> phase methods.</summary>
internal readonly record struct AshlarKernelRegistrationContext(
    IServiceCollection Services,
    AshlarHostingOptions Options,
    ModuleSelection Modules,
    IConfiguration Configuration);
