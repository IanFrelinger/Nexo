using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Nexo.PluginHost
{
    /// <summary>
    /// A hardened, collectible AssemblyLoadContext for loading plugins with controlled dependency resolution.
    /// </summary>
    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginPath;
        private readonly string _pluginDependenciesPath;
        private readonly string _curatedLibsPath;
        private readonly ILogger<PluginAssemblyLoadContext> _logger;
        private readonly HashSet<string> _allowedAssemblyNames;
        private readonly Dictionary<string, Assembly> _loadedAssemblies;

        public PluginAssemblyLoadContext(
            string pluginPath, 
            string pluginDependenciesPath, 
            string curatedLibsPath = null,
            ILogger<PluginAssemblyLoadContext> logger = null) 
            : base(isCollectible: true)
        {
            _pluginPath = pluginPath ?? throw new ArgumentNullException(nameof(pluginPath));
            _pluginDependenciesPath = pluginDependenciesPath ?? throw new ArgumentNullException(nameof(pluginDependenciesPath));
            _curatedLibsPath = curatedLibsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "curated-libs");
            _logger = logger;
            _loadedAssemblies = new Dictionary<string, Assembly>();
            
            // Initialize allowed assembly names for security
            _allowedAssemblyNames = InitializeAllowedAssemblies();
            
            // Ensure directories exist
            Directory.CreateDirectory(_pluginDependenciesPath);
            Directory.CreateDirectory(_curatedLibsPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            try
            {
                // Check if already loaded
                if (_loadedAssemblies.TryGetValue(assemblyName.FullName, out var loadedAssembly))
                {
                    return loadedAssembly;
                }

                // Security check - only allow specific assemblies
                if (!_allowedAssemblyNames.Contains(assemblyName.Name))
                {
                    _logger?.LogWarning("Assembly {AssemblyName} is not in the allowed list, denying load", assemblyName.Name);
                    return null;
                }

                // Try to load from plugin dependencies first
                var assemblyPath = Path.Combine(_pluginDependenciesPath, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    var assembly = LoadFromAssemblyPath(assemblyPath);
                    _loadedAssemblies[assemblyName.FullName] = assembly;
                    _logger?.LogDebug("Loaded assembly {AssemblyName} from plugin dependencies", assemblyName.Name);
                    return assembly;
                }

                // Try to load from curated libs
                assemblyPath = Path.Combine(_curatedLibsPath, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    var assembly = LoadFromAssemblyPath(assemblyPath);
                    _loadedAssemblies[assemblyName.FullName] = assembly;
                    _logger?.LogDebug("Loaded assembly {AssemblyName} from curated libs", assemblyName.Name);
                    return assembly;
                }

                // For system assemblies, try default loading
                if (IsSystemAssembly(assemblyName))
                {
                    var assembly = Assembly.Load(assemblyName);
                    _loadedAssemblies[assemblyName.FullName] = assembly;
                    _logger?.LogDebug("Loaded system assembly {AssemblyName}", assemblyName.Name);
                    return assembly;
                }

                _logger?.LogWarning("Assembly {AssemblyName} not found in allowed locations", assemblyName.Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load assembly {AssemblyName}", assemblyName.Name);
                return null;
            }
        }

        /// <summary>
        /// Loads the plugin assembly from the specified path.
        /// </summary>
        public Assembly LoadPluginAssembly()
        {
            try
            {
                var assembly = LoadFromAssemblyPath(_pluginPath);
                _loadedAssemblies[assembly.GetName().FullName] = assembly;
                _logger?.LogInformation("Loaded plugin assembly from {PluginPath}", _pluginPath);
                return assembly;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load plugin assembly from {PluginPath}", _pluginPath);
                throw;
            }
        }

        /// <summary>
        /// Unloads the context and waits for finalization.
        /// </summary>
        public void UnloadAndWait()
        {
            try
            {
                _logger?.LogDebug("Unloading PluginAssemblyLoadContext");
                Unload();
                
                // Wait for finalization with timeout
                var weakRef = new WeakReference(this);
                var timeout = TimeSpan.FromSeconds(10);
                var startTime = DateTime.UtcNow;
                
                while (weakRef.IsAlive && DateTime.UtcNow - startTime < timeout)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    System.Threading.Thread.Sleep(100);
                }
                
                if (weakRef.IsAlive)
                {
                    _logger?.LogWarning("AssemblyLoadContext did not unload within timeout period");
                }
                else
                {
                    _logger?.LogDebug("AssemblyLoadContext successfully unloaded");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during AssemblyLoadContext unload");
            }
        }

        private HashSet<string> InitializeAllowedAssemblies()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // System assemblies
                "System.Private.CoreLib",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Text",
                "System.Text.Json",
                "System.Threading",
                "System.Threading.Tasks",
                "System.IO",
                "System.IO.FileSystem",
                "System.Reflection",
                "System.Reflection.Metadata",
                "System.ComponentModel",
                "System.ComponentModel.Annotations",
                "System.Diagnostics",
                "System.Diagnostics.Debug",
                "System.Globalization",
                "System.Net",
                "System.Net.Http",
                "System.Security",
                "System.Security.Cryptography",
                "System.Xml",
                "System.Xml.Linq",
                
                // Microsoft Extensions
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Extensions.Hosting",
                "Microsoft.Extensions.Hosting.Abstractions",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.Configuration.Abstractions",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Options",
                "Microsoft.Extensions.Primitives",
                
                // Nexo contracts
                "Nexo.Core.Contracts",
                "Nexo.Core.Contracts.Capabilities",
                
                // OpenTelemetry
                "OpenTelemetry",
                "OpenTelemetry.Api",
                "OpenTelemetry.Extensions.Hosting",
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Exporter.OpenTelemetryProtocol",
                "OpenTelemetry.Instrumentation.Runtime"
            };
        }

        private bool IsSystemAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
        }
    }
}
