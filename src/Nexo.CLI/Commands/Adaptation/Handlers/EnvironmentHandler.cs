using Microsoft.Extensions.DependencyInjection;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles environment status functionality
    /// </summary>
    public class EnvironmentHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EnvironmentHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ShowEnvironmentStatus()
        {
            var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
            var environmentStatus = await dashboard.GetEnvironmentAdaptationStatusAsync();
            
            Console.WriteLine("🌍 Environment Adaptation Status");
            Console.WriteLine($"Last Check: {environmentStatus.LastEnvironmentCheck:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            
            var env = environmentStatus.CurrentEnvironment;
            Console.WriteLine("Current Environment:");
            Console.WriteLine($"  Context: {env.Context}");
            Console.WriteLine($"  Platform: {env.Platform}");
            Console.WriteLine($"  CPU Cores: {env.Resources.CpuCores}");
            Console.WriteLine($"  Total Memory: {env.Resources.TotalMemoryMB:N0} MB");
            Console.WriteLine($"  Available Memory: {env.Resources.AvailableMemoryMB:N0} MB");
            Console.WriteLine($"  CPU Utilization: {env.Resources.CpuUtilization:P}");
            Console.WriteLine($"  Memory Utilization: {env.Resources.MemoryUtilization:P}");
            Console.WriteLine($"  Resource Constrained: {(env.Resources.IsResourceConstrained ? "Yes" : "No")}");
            Console.WriteLine();
            
            if (environmentStatus.ActiveOptimizations.Any())
            {
                Console.WriteLine("Active Optimizations:");
                foreach (var optimization in environmentStatus.ActiveOptimizations)
                {
                    Console.WriteLine($"  • {optimization.Type}: {optimization.Description}");
                    Console.WriteLine($"    Priority: {optimization.Priority}");
                    Console.WriteLine($"    Enabled: {(optimization.IsEnabled ? "Yes" : "No")}");
                }
                Console.WriteLine();
            }
            
            if (environmentStatus.RecentChanges.Any())
            {
                Console.WriteLine("Recent Environment Changes:");
                foreach (var change in environmentStatus.RecentChanges.Take(5))
                {
                    Console.WriteLine($"  • {change.ChangeType}: {change.Description}");
                    Console.WriteLine($"    Changed: {change.ChangedAt:yyyy-MM-dd HH:mm:ss}");
                }
                Console.WriteLine();
            }
            
            var validation = environmentStatus.ValidationResult;
            Console.WriteLine("Environment Validation:");
            Console.WriteLine($"  Valid: {(validation.IsValid ? "SUCCESS:" : "ERROR:")}");
            
            if (validation.ValidationErrors.Any())
            {
                Console.WriteLine("  Errors:");
                foreach (var error in validation.ValidationErrors)
                {
                    Console.WriteLine($"    ERROR: {error}");
                }
            }
            
            if (validation.ValidationWarnings.Any())
            {
                Console.WriteLine("  Warnings:");
                foreach (var warning in validation.ValidationWarnings)
                {
                    Console.WriteLine($"    WARNING:  {warning}");
                }
            }
            
            if (validation.Recommendations.Any())
            {
                Console.WriteLine("  Recommendations:");
                foreach (var recommendation in validation.Recommendations)
                {
                    Console.WriteLine($"    Idea {recommendation}");
                }
            }
        }
    }
}
