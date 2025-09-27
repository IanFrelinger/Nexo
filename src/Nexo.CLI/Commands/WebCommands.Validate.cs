using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Enums;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Web command validation functionality
    /// </summary>
    public static partial class WebCommands
    {
        private static Command CreateValidateCommand(IWebCodeGenerator webCodeGenerator, IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var validateCommand = new Command("validate", "Validate web code generation configuration");

            var configFileOption = new Option<string>("--config", "Configuration file path") { IsRequired = true };

            validateCommand.AddOption(configFileOption);

            validateCommand.SetHandler((config) =>
            {
                try
                {
                    logger.LogInformation("Validating web configuration from: {Config}", config);

                    if (!File.Exists(config))
                    {
                        Console.WriteLine($"Error: Configuration file not found: {config}");
                        return;
                    }

                    var configContent = File.ReadAllText(config);
                    var configData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configContent);

                    if (configData == null)
                    {
                        Console.WriteLine("Error: Invalid configuration file format");
                        return;
                    }

                    Console.WriteLine("Search Validating configuration...");

                    var isValid = true;
                    var errors = new List<string>();

                    // Validate framework
                    if (configData.TryGetValue("framework", out var frameworkValue))
                    {
                        var framework = frameworkValue?.ToString();
                        if (!string.IsNullOrEmpty(framework))
                        {
                            if (!Enum.TryParse<WebFrameworkType>(framework, true, out _))
                            {
                                errors.Add($"Invalid framework: {framework}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"SUCCESS: Framework: {framework}");
                            }
                        }
                    }

                    // Validate component type
                    if (configData.TryGetValue("componentType", out var componentTypeValue))
                    {
                        var componentType = componentTypeValue?.ToString();
                        if (!string.IsNullOrEmpty(componentType))
                        {
                            if (!Enum.TryParse<WebComponentType>(componentType, true, out _))
                            {
                                errors.Add($"Invalid component type: {componentType}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"SUCCESS: Component type: {componentType}");
                            }
                        }
                    }

                    // Validate optimization
                    if (configData.TryGetValue("optimization", out var optimizationValue))
                    {
                        var optimization = optimizationValue?.ToString();
                        if (!string.IsNullOrEmpty(optimization))
                        {
                            if (!Enum.TryParse<WebAssemblyOptimization>(optimization, true, out _))
                            {
                                errors.Add($"Invalid optimization: {optimization}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"SUCCESS: Optimization: {optimization}");
                            }
                        }
                    }

                    // Validate required fields
                    if (!configData.ContainsKey("componentName"))
                    {
                        errors.Add("Missing required field: componentName");
                        isValid = false;
                    }
                    else
                    {
                        Console.WriteLine($"SUCCESS: Component name: {configData["componentName"]}");
                    }

                    if (!configData.ContainsKey("targetPath"))
                    {
                        errors.Add("Missing required field: targetPath");
                        isValid = false;
                    }
                    else
                    {
                        Console.WriteLine($"SUCCESS: Target path: {configData["targetPath"]}");
                    }

                    if (isValid)
                    {
                        Console.WriteLine("\nSUCCESS: Configuration is valid!");
                    }
                    else
                    {
                        Console.WriteLine("\nERROR: Configuration validation failed:");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"   - {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to validate configuration");
                    Console.WriteLine($"Error: Failed to validate configuration: {ex.Message}");
                }
            }, configFileOption);

            return validateCommand;
        }
    }
}
