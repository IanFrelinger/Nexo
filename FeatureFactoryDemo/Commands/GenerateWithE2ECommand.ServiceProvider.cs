using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FeatureFactoryDemo.Services;
using FeatureFactoryDemo.Data;
using Nexo.Feature.Analysis;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Service provider functionality for E2E generation command.
    /// </summary>
    public partial class GenerateWithE2ECommand
    {
        private IServiceProvider GetServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddAnalysisFeature();
            services.AddDbContext<FeatureFactoryDbContext>(options =>
                options.UseSqlite("Data Source=featurefactory.db"));
            services.AddScoped<CommandHistoryService>();
            services.AddScoped<CodebaseAnalysisService>();
            return services.BuildServiceProvider();
        }
    }
}
