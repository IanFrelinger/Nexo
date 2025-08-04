using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.WebOnly
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds all Nexo Web-Only CLI services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddNexoWebOnlyCliServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add HTTP client factory
            services.AddHttpClient();

            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add Web feature services
            services.AddTransient<Nexo.Feature.Web.Interfaces.IWebCodeGenerator, Nexo.Feature.Web.Services.WebCodeGenerator>();
            services.AddTransient<Nexo.Feature.Web.Interfaces.IWebAssemblyOptimizer, Nexo.Feature.Web.Services.WebAssemblyOptimizer>();
            services.AddTransient<Nexo.Feature.Web.Interfaces.IFrameworkTemplateProvider, Nexo.Feature.Web.Services.FrameworkTemplateProvider>();
            services.AddTransient<Nexo.Feature.Web.UseCases.GenerateWebCodeUseCase>();

            return services;
        }
    }
} 