using System;
using System.Threading.Tasks;
using Nexo.Demo.Scripts.Utils;

namespace Nexo.Demo.Scripts;

/// <summary>
/// Main entry point for the demo scripts
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            var remainingArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

            return command switch
            {
                "validate" => await RunValidationAsync(remainingArgs),
                "seed" => await RunSeedFixturesAsync(remainingArgs),
                "build" => await RunBuildAsync(remainingArgs),
                "run" => await RunDemoAsync(remainingArgs),
                "uninstall" => await RunUninstallAsync(remainingArgs),
                "list-workloads" => await RunListWorkloadsAsync(remainingArgs),
                "help" or "--help" or "-h" => ShowUsage(),
                _ => ShowUsage($"Unknown command: {command}")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Runs validation
    /// </summary>
    static async Task<int> RunValidationAsync(string[] args)
    {
        var projectRoot = args.Length > 0 ? args[0] : null;
        
        Console.WriteLine("🔍 NEXO FEATURE LAB VALIDATION");
        Console.WriteLine("===============================");
        
        var runner = new ValidationRunner();
        var result = await runner.RunValidationAsync(projectRoot);
        
        Console.WriteLine($"\nValidation completed in {result.Duration.TotalSeconds:F1} seconds");
        Console.WriteLine($"Overall result: {(result.Success ? "✅ PASS" : "❌ FAIL")}");
        
        if (!result.Success && !string.IsNullOrEmpty(result.Error))
        {
            Console.WriteLine($"Error: {result.Error}");
        }
        
        Console.WriteLine("\nDetailed Results:");
        Console.WriteLine("==================");
        
        foreach (var check in result.Checks)
        {
            var status = check.Value.Success ? "✅" : "❌";
            Console.WriteLine($"{status} {check.Value.Name}: {check.Value.Message}");
            
            if (!check.Value.Success && !string.IsNullOrEmpty(check.Value.Suggestion))
            {
                Console.WriteLine($"   💡 Suggestion: {check.Value.Suggestion}");
            }
        }
        
        return result.Success ? 0 : 1;
    }

    /// <summary>
    /// Seeds fixtures
    /// </summary>
    static async Task<int> RunSeedFixturesAsync(string[] args)
    {
        var projectRoot = args.Length > 0 ? args[0] : null;
        
        var seeder = new FixtureSeeder();
        var result = await seeder.SeedFixturesAsync(projectRoot);
        
        if (result.Success)
        {
            Console.WriteLine($"✅ Fixtures seeded successfully in {result.Duration.TotalSeconds:F1} seconds");
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ Failed to seed fixtures: {result.Error}");
            return 1;
        }
    }

    /// <summary>
    /// Builds the solution
    /// </summary>
    static async Task<int> RunBuildAsync(string[] args)
    {
        var projectRoot = args.Length > 0 ? args[0] : null;
        projectRoot ??= Environment.CurrentDirectory;
        
        var solutionPath = System.IO.Path.Combine(projectRoot, "demo/feature-lab/Playground.sln");
        
        if (!System.IO.File.Exists(solutionPath))
        {
            Console.WriteLine($"❌ Solution not found: {solutionPath}");
            return 1;
        }
        
        Console.WriteLine("🔨 BUILDING SOLUTION");
        Console.WriteLine("====================");
        
        var buildManager = new BuildManager();
        var result = await buildManager.BuildSolutionAsync(solutionPath);
        
        if (result.Success)
        {
            Console.WriteLine("✅ Build completed successfully");
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ Build failed: {result.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Runs the complete demo
    /// </summary>
    static async Task<int> RunDemoAsync(string[] args)
    {
        var projectRoot = args.Length > 0 ? args[0] : null;
        
        var options = new DemoOptions();
        
        // Parse additional arguments
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--maui":
                    options.UseMaui = true;
                    break;
                case "--blazor":
                    options.UseMaui = false;
                    break;
                case "--skip-validation":
                    options.SkipValidation = true;
                    break;
                case "--no-fixtures":
                    options.SeedFixtures = false;
                    break;
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                    {
                        options.Port = port;
                        i++; // Skip next argument
                    }
                    break;
            }
        }
        
        using var runner = new DemoRunner();
        var result = await runner.RunDemoAsync(projectRoot, options);
        
        if (result.Success)
        {
            Console.WriteLine($"✅ Demo completed successfully in {result.Duration.TotalSeconds:F1} seconds");
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ Demo failed: {result.Error}");
            return 1;
        }
    }

    /// <summary>
    /// Runs uninstall operations
    /// </summary>
    static async Task<int> RunUninstallAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("❌ Please specify what to uninstall");
            Console.WriteLine("Usage: uninstall <target>");
            Console.WriteLine("Targets: maui, docker, node, dotnet, <package-name>");
            return 1;
        }

        var target = args[0].ToLowerInvariant();
        var dependencyManager = new DependencyManager();

        Console.WriteLine($"🗑️  UNINSTALLING {target.ToUpper()}");
        Console.WriteLine("===============================");

        UninstallResult result = target switch
        {
            "maui" => await dependencyManager.UninstallMauiWorkloadsAsync(),
            "docker" => await dependencyManager.UninstallDependencyAsync("docker"),
            "node" => await dependencyManager.UninstallDependencyAsync("node"),
            "dotnet" => await dependencyManager.UninstallDependencyAsync("dotnet"),
            _ => await dependencyManager.UninstallDependencyAsync(target)
        };

        if (result.Success)
        {
            Console.WriteLine($"✅ {result.Message}");
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ {result.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Lists installed workloads
    /// </summary>
    static async Task<int> RunListWorkloadsAsync(string[] args)
    {
        var dependencyManager = new DependencyManager();

        Console.WriteLine("📋 INSTALLED WORKLOADS");
        Console.WriteLine("======================");

        var result = await dependencyManager.ListInstalledWorkloadsAsync();

        if (result.Success)
        {
            Console.WriteLine(result.Message);
            Console.WriteLine();
            
            if (result.Workloads.Count > 0)
            {
                foreach (var workload in result.Workloads)
                {
                    Console.WriteLine($"  • {workload}");
                }
            }
            else
            {
                Console.WriteLine("  No workloads installed");
            }
            
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ {result.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Shows usage information
    /// </summary>
    static int ShowUsage(string error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"❌ {error}");
            Console.WriteLine();
        }

        Console.WriteLine("NEXO FEATURE LAB DEMO SCRIPTS");
        Console.WriteLine("=============================");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- <command> [options] [project-root]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  validate [project-root]     Run validation checks");
        Console.WriteLine("  seed [project-root]         Seed demo fixtures");
        Console.WriteLine("  build [project-root]        Build the solution");
        Console.WriteLine("  run [project-root] [options] Run the complete demo");
        Console.WriteLine("  uninstall <target>          Uninstall dependencies");
        Console.WriteLine("  list-workloads              List installed workloads");
        Console.WriteLine("  help                        Show this help message");
        Console.WriteLine();
        Console.WriteLine("Uninstall Targets:");
        Console.WriteLine("  maui                        Uninstall MAUI workloads");
        Console.WriteLine("  docker                      Uninstall Docker");
        Console.WriteLine("  node                        Uninstall Node.js");
        Console.WriteLine("  dotnet                      Uninstall .NET workloads");
        Console.WriteLine("  <package-name>              Uninstall specific package");
        Console.WriteLine();
        Console.WriteLine("Demo Options:");
        Console.WriteLine("  --maui                      Use MAUI application (default)");
        Console.WriteLine("  --blazor                    Use Blazor application");
        Console.WriteLine("  --skip-validation           Skip validation checks");
        Console.WriteLine("  --no-fixtures               Skip fixture seeding");
        Console.WriteLine("  --port <number>             Specify port for Blazor app");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- validate");
        Console.WriteLine("  dotnet run -- seed /path/to/project");
        Console.WriteLine("  dotnet run -- run --blazor --port 8080");
        Console.WriteLine("  dotnet run -- run --maui --skip-validation");
        Console.WriteLine("  dotnet run -- uninstall maui");
        Console.WriteLine("  dotnet run -- list-workloads");
        
        return string.IsNullOrEmpty(error) ? 0 : 1;
    }
}
