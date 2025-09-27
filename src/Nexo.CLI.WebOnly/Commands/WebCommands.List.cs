using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using System.Linq;
using System.IO;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// List command implementation for WebCommands.
    /// </summary>
    public static partial class WebCommands
    {
        /// <summary>
        /// Creates the list command
        /// </summary>
        private static Command CreateListCommand(IWebCodeGenerator webCodeGenerator, IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var listCommand = new Command("list", "List available frameworks, component types, and optimizations");

            var frameworksOption = new Option<bool>("--frameworks", "List supported frameworks") { IsRequired = false };
            var componentTypesOption = new Option<bool>("--types", "List supported component types") { IsRequired = false };
            var optimizationsOption = new Option<bool>("--optimizations", "List available optimizations") { IsRequired = false };

            listCommand.AddOption(frameworksOption);
            listCommand.AddOption(componentTypesOption);
            listCommand.AddOption(optimizationsOption);

            listCommand.SetHandler((frameworks, types, optimizations) =>
            {
                try
                {
                    logger.LogInformation("Listing available options");

                    if (frameworks)
                    {
                        Console.WriteLine("Running Supported Frameworks:");
                        var supportedFrameworks = webCodeGenerator.GetSupportedFrameworks();
                        foreach (var framework in supportedFrameworks)
                        {
                            Console.WriteLine($"   - {framework}");
                        }
                    }

                    if (types)
                    {
                        Console.WriteLine("\nTarget Supported Component Types:");
                        var componentTypes = Enum.GetNames<WebComponentType>();
                        foreach (var type in componentTypes)
                        {
                            Console.WriteLine($"   - {type}");
                        }
                    }

                    if (optimizations)
                    {
                        Console.WriteLine("\n⚡ Available Optimizations:");
                        var optimizationsList = wasmOptimizer.GetAvailableOptimizations();
                        foreach (var optimization in optimizationsList)
                        {
                            Console.WriteLine($"   - {optimization}");
                        }
                    }

                    if (!frameworks && !types && !optimizations)
                    {
                        // Show all if no specific option is selected
                        Console.WriteLine("Running Supported Frameworks:");
                        var supportedFrameworks = webCodeGenerator.GetSupportedFrameworks();
                        foreach (var framework in supportedFrameworks)
                        {
                            Console.WriteLine($"   - {framework}");
                        }

                        Console.WriteLine("\nTarget Supported Component Types:");
                        var componentTypes = Enum.GetNames<WebComponentType>();
                        foreach (var type in componentTypes)
                        {
                            Console.WriteLine($"   - {type}");
                        }

                        Console.WriteLine("\n⚡ Available Optimizations:");
                        var optimizationsList = wasmOptimizer.GetAvailableOptimizations();
                        foreach (var optimization in optimizationsList)
                        {
                            Console.WriteLine($"   - {optimization}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to list options");
                    Console.WriteLine($"Error: Failed to list options: {ex.Message}");
                }
            }, frameworksOption, componentTypesOption, optimizationsOption);

            return listCommand;
        }
    }
}
