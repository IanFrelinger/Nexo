using System;
using System.Collections.Generic;
using System.Linq;
using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Shared.Interfaces;

namespace Nexo.CLI.Commands
{

/// <summary>
/// CLI commands for configuration management.
/// </summary>
public static class ConfigurationCommands
{
    /// <summary>
    /// Creates the configuration command with all subcommands.
    /// </summary>
    /// <param name="aiConfigurationService">AI configuration service.</param>
    /// <param name="configurationProvider">Configuration provider.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>The configuration command.</returns>
    public static Command CreateConfigurationCommand(
        IAIConfigurationService aiConfigurationService,
        // IConfigurationProvider configurationProvider, // commented out
        ILogger logger)
    {
        var configCommand = new Command("config", "Configuration management commands");
        
        // Show configuration command
        var showCommand = new Command("show", "Show current configuration");
        var showFormatOption = new Option<string>("--format", "Output format (json, text)") { IsRequired = false };
        showCommand.AddOption(showFormatOption);
        showCommand.SetHandler(async (format) =>
        {
            try
            {
                var config = await aiConfigurationService.GetConfigurationAsync();
                var outputFormat = format?.ToLowerInvariant() ?? "text";
                
                if (outputFormat == "json")
                {
                    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine("=== AI Configuration ===");
                    Console.WriteLine($"Mode: {config.Mode}");
                    Console.WriteLine($"Default Model: {config.Model?.Name ?? "Not set"}");
                    Console.WriteLine($"Max Tokens: {config.Model?.MaxInputTokens ?? 0}");
                    Console.WriteLine($"Temperature: {config.Model?.Temperature ?? 0}");
                    Console.WriteLine($"Max Concurrent Requests: {config.Resources?.MaxConcurrentRequests ?? 0}");
                    Console.WriteLine($"Request Timeout: {config.Resources?.EnableResourceMonitoring ?? false}");
                    Console.WriteLine($"Caching Enabled: {config.Performance?.EnableResponseCaching ?? false}");
                    Console.WriteLine($"Cache Expiration: {config.Performance?.CacheExpirationSeconds ?? 0} seconds");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to show configuration");
                Console.WriteLine("Error: Failed to retrieve configuration");
            }
        }, showFormatOption);
        configCommand.AddCommand(showCommand);
        
        // Set mode command
        var setModeCommand = new Command("set-mode", "Set AI operation mode");
        var modeArgument = new Argument<string>("mode", "AI mode (development, production, ai-heavy)");
        setModeCommand.AddArgument(modeArgument);
        setModeCommand.SetHandler(async (mode) =>
        {
            try
            {
                if (!Enum.TryParse<AIMode>(mode, true, out var aiMode))
                {
                    Console.WriteLine("Error: Invalid mode. Valid modes are: development, production, ai-heavy");
                    return;
                }
                
                var config = await aiConfigurationService.LoadForModeAsync(aiMode);
                await aiConfigurationService.SaveConfigurationAsync(config);
                
                Console.WriteLine($"AI mode set to: {aiMode}");
                Console.WriteLine($"Configuration updated with {aiMode} defaults");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set AI mode");
                Console.WriteLine("Error: Failed to set AI mode");
            }
        }, modeArgument);
        configCommand.AddCommand(setModeCommand);
        
        // Validate configuration command
        var validateCommand = new Command("validate", "Validate current configuration");
        validateCommand.SetHandler(async () =>
        {
            try
            {
                var config = await aiConfigurationService.GetConfigurationAsync();
                var validationResult = await aiConfigurationService.ValidateAsync(config);
                
                if (validationResult.IsValid)
                {
                    Console.WriteLine("✓ Configuration is valid");
                    
                    if (validationResult.Warnings.Any())
                    {
                        Console.WriteLine("\nWarnings:");
                        foreach (var warning in validationResult.Warnings)
                        {
                            Console.WriteLine($"  ⚠ {warning.Message} ({warning.FieldPath})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("✗ Configuration has errors:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"  ✗ {error.Message} ({error.FieldPath})");
                    }
                    
                    if (validationResult.Warnings.Any())
                    {
                        Console.WriteLine("\nWarnings:");
                        foreach (var warning in validationResult.Warnings)
                        {
                            Console.WriteLine($"  ⚠ {warning.Message} ({warning.FieldPath})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate configuration");
                Console.WriteLine("Error: Failed to validate configuration");
            }
        });
        configCommand.AddCommand(validateCommand);
        
        // Reset configuration command
        var resetCommand = new Command("reset", "Reset configuration to defaults");
        var resetModeOption = new Option<string>("--mode", "Mode to reset to (development, production, ai-heavy)") { IsRequired = false };
        resetCommand.AddOption(resetModeOption);
        resetCommand.SetHandler(async (mode) =>
        {
            try
            {
                var aiMode = AIMode.Development;
                if (!string.IsNullOrEmpty(mode))
                {
                    if (!Enum.TryParse<AIMode>(mode, true, out aiMode))
                    {
                        Console.WriteLine("Error: Invalid mode. Valid modes are: development, production, ai-heavy");
                        return;
                    }
                }
                
                var defaultConfig = aiConfigurationService.GetDefaultConfiguration(aiMode);
                await aiConfigurationService.SaveConfigurationAsync(defaultConfig);
                
                Console.WriteLine($"Configuration reset to {aiMode} defaults");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reset configuration");
                Console.WriteLine("Error: Failed to reset configuration");
            }
        }, resetModeOption);
        configCommand.AddCommand(resetCommand);
        
        // Configuration path command
        var pathCommand = new Command("path", "Show configuration file path");
        pathCommand.SetHandler(() =>
        {
            try
            {
                // var configPath = configurationProvider.GetConfigurationPath();
                // Console.WriteLine($"Configuration path: {configPath}");
                Console.WriteLine("Configuration path: [unavailable - IConfigurationProvider removed]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get configuration path");
                Console.WriteLine("Error: Failed to get configuration path");
            }
        });
        configCommand.AddCommand(pathCommand);
        
        return configCommand;
    }
}
} 