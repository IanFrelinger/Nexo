using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nexo.Feature.Analysis;
using Nexo.Feature.Analysis.Interfaces;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Services;

namespace FeatureFactoryDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Nexo Feature Factory Demo ===");
            Console.WriteLine("Demonstrating AI-powered feature generation with coding standards validation");
            Console.WriteLine();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Get required services
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var demoService = serviceProvider.GetRequiredService<FeatureFactoryDemoService>();

            try
            {
                // Run the demo
                await demoService.RunDemoAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running Feature Factory Demo");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                serviceProvider.Dispose();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Database
            services.AddDbContext<FeatureFactoryDbContext>(options =>
                options.UseSqlite("Data Source=featurefactory.db"));

            // Analysis services
            services.AddScoped<ICodingStandardAnalyzer, MockCodingStandardAnalyzer>();
            services.AddScoped<ICodebaseAnalyzer, MockCodebaseAnalyzer>();
            services.AddScoped<IIterationStrategy, MockIterationStrategy>();

            // Demo services
            services.AddScoped<FeatureFactoryDemoService>();
            services.AddScoped<FeatureGenerationService>();
            services.AddScoped<CodebaseAnalysisService>();
            services.AddScoped<CommandHistoryService>();
            services.AddScoped<ValidationService>();
            services.AddScoped<DemoDataService>();
        }
    }
}
