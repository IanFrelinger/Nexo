using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Main demo runner that orchestrates the entire demo process
/// </summary>
public class DemoRunner : IDisposable
{
    private readonly ValidationRunner _validationRunner;
    private readonly BuildManager _buildManager;
    private readonly ProcessManager _processManager;
    private readonly FixtureSeeder _fixtureSeeder;

    public DemoRunner()
    {
        _validationRunner = new ValidationRunner();
        _buildManager = new BuildManager();
        _processManager = new ProcessManager();
        _fixtureSeeder = new FixtureSeeder();
    }

    /// <summary>
    /// Runs the complete demo process
    /// </summary>
    public async Task<DemoResult> RunDemoAsync(string projectRoot = null, DemoOptions options = null)
    {
        projectRoot ??= Directory.GetCurrentDirectory();
        options ??= new DemoOptions();

        var result = new DemoResult
        {
            ProjectRoot = projectRoot,
            StartTime = DateTime.UtcNow
        };

        try
        {
            Console.WriteLine("🚀 NEXO FEATURE LAB DEMO RUNNER");
            Console.WriteLine("===============================");

            // Step 1: Validation
            Console.WriteLine("\nStep 1: Running validation pass...");
            var validationResult = await _validationRunner.RunValidationAsync(projectRoot);
            result.ValidationResult = validationResult;

            if (!validationResult.Success && !options.SkipValidation)
            {
                result.Success = false;
                result.Error = "Validation failed";
                return result;
            }

            // Step 2: Seed fixtures
            if (options.SeedFixtures)
            {
                Console.WriteLine("\nStep 2: Seeding fixtures...");
                var fixtureResult = await _fixtureSeeder.SeedFixturesAsync(projectRoot);
                result.FixtureResult = fixtureResult;

                if (!fixtureResult.Success)
                {
                    Console.WriteLine($"⚠️  Warning: Fixture seeding failed: {fixtureResult.Error}");
                }
            }

            // Step 3: Build
            Console.WriteLine("\nStep 3: Building application...");
            var buildResult = await _buildManager.BuildSolutionAsync(
                Path.Combine(projectRoot, "demo/feature-lab/Playground.sln"));

            if (!buildResult.Success)
            {
                result.Success = false;
                result.Error = $"Build failed: {buildResult.Message}";
                return result;
            }

            // Step 4: Start application
            Console.WriteLine("\nStep 4: Starting application...");
            var appResult = await StartApplicationAsync(projectRoot, options);
            result.ApplicationResult = appResult;

            if (!appResult.Success)
            {
                result.Success = false;
                result.Error = $"Application failed to start: {appResult.Message}";
                return result;
            }

            result.Success = true;
            result.EndTime = DateTime.UtcNow;

            Console.WriteLine("\n✅ Demo completed successfully!");
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        finally
        {
            // Cleanup
            _processManager.Dispose();
        }
    }

    /// <summary>
    /// Starts the appropriate application based on options
    /// </summary>
    private async Task<ApplicationResult> StartApplicationAsync(string projectRoot, DemoOptions options)
    {
        try
        {
            if (options.UseMaui)
            {
                return await StartMauiAppAsync(projectRoot, options);
            }
            else
            {
                return await StartBlazorAppAsync(projectRoot, options);
            }
        }
        catch (Exception ex)
        {
            return new ApplicationResult
            {
                Success = false,
                Message = $"Failed to start application: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Starts the MAUI application
    /// </summary>
    private async Task<ApplicationResult> StartMauiAppAsync(string projectRoot, DemoOptions options)
    {
        try
        {
            var mauiProject = Path.Combine(projectRoot, "demo/feature-lab/Playground.Maui/Playground.Maui.csproj");
            
            if (!File.Exists(mauiProject))
            {
                return new ApplicationResult
                {
                    Success = false,
                    Message = "MAUI project not found"
                };
            }

            Console.WriteLine("🌐 Starting Nexo Feature Lab MAUI Demo...");
            Console.WriteLine("The application will open in a new window.");

            var process = await _processManager.StartDotNetAppAsync(
                mauiProject,
                "",
                options.Framework,
                options.Configuration);

            return new ApplicationResult
            {
                Success = true,
                Message = "MAUI application started successfully",
                Process = process
            };
        }
        catch (Exception ex)
        {
            return new ApplicationResult
            {
                Success = false,
                Message = $"Failed to start MAUI app: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Starts the Blazor application
    /// </summary>
    private async Task<ApplicationResult> StartBlazorAppAsync(string projectRoot, DemoOptions options)
    {
        try
        {
            var blazorProject = Path.Combine(projectRoot, "demo/feature-lab/Playground.Server/Playground.Server.csproj");
            
            if (!File.Exists(blazorProject))
            {
                return new ApplicationResult
                {
                    Success = false,
                    Message = "Blazor project not found"
                };
            }

            var port = options.Port ?? 5000;
            
            // Check if port is available
            if (!SystemUtils.IsPortAvailable(port))
            {
                port = SystemUtils.FindAvailablePort(5000, 9000);
                Console.WriteLine($"Port {options.Port} is in use, using port {port} instead");
            }

            Console.WriteLine("🌐 Starting Nexo Feature Lab Playground...");
            Console.WriteLine($"The playground will be available at: http://localhost:{port}");

            var process = await _processManager.StartWebAppAsync(
                blazorProject,
                port,
                options.Framework,
                options.Configuration);

            return new ApplicationResult
            {
                Success = true,
                Message = $"Blazor application started successfully on port {port}",
                Process = process,
                Url = $"http://localhost:{port}"
            };
        }
        catch (Exception ex)
        {
            return new ApplicationResult
            {
                Success = false,
                Message = $"Failed to start Blazor app: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Runs only validation
    /// </summary>
    public async Task<ValidationResult> RunValidationOnlyAsync(string projectRoot = null)
    {
        projectRoot ??= Directory.GetCurrentDirectory();
        return await _validationRunner.RunValidationAsync(projectRoot);
    }

    /// <summary>
    /// Seeds fixtures only
    /// </summary>
    public async Task<FixtureResult> SeedFixturesOnlyAsync(string projectRoot = null)
    {
        projectRoot ??= Directory.GetCurrentDirectory();
        return await _fixtureSeeder.SeedFixturesAsync(projectRoot);
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        _processManager?.Dispose();
    }
}

/// <summary>
/// Options for running the demo
/// </summary>
public class DemoOptions
{
    public bool UseMaui { get; set; } = true;
    public bool SkipValidation { get; set; } = false;
    public bool SeedFixtures { get; set; } = true;
    public string Framework { get; set; } = "net8.0-maccatalyst";
    public string Configuration { get; set; } = "Release";
    public int? Port { get; set; } = null;
}

/// <summary>
/// Result of running the demo
/// </summary>
public class DemoResult
{
    public string ProjectRoot { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public ValidationResult ValidationResult { get; set; }
    public FixtureResult FixtureResult { get; set; }
    public ApplicationResult ApplicationResult { get; set; }
    
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Result of starting an application
/// </summary>
public class ApplicationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ManagedProcess Process { get; set; }
}
