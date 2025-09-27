using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Caching.Advanced;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// API key management functionality for AI operations commands
    /// </summary>
    public partial class AIOperationsCommands
    {
        /// <summary>
        /// Creates API key management commands.
        /// </summary>
        private Command CreateApiKeyManagementCommand()
        {
            var apiKeyCommand = new Command("apikey", "API key management and security");

            // Generate API key
            var generateCommand = new Command("generate", "Generate a new API key");
            var nameOption = new Option<string>("--name", "Name for the API key");
            var descriptionOption = new Option<string>("--description", "Description of the API key");
            var expirationOption = new Option<string>("--expiration", "Expiration time (e.g., '7d', '30d', '1y')");
            var permissionsOption = new Option<string[]>("--permissions", "Permissions for the API key");

            generateCommand.AddOption(nameOption);
            generateCommand.AddOption(descriptionOption);
            generateCommand.AddOption(expirationOption);
            generateCommand.AddOption(permissionsOption);

            generateCommand.SetHandler(async (string name, string description, string expiration, string[] permissions) =>
            {
                try
                {
                    var expirationTime = ParseExpiration(expiration);
                    var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                        name, 
                        description, 
                        expirationTime, 
                        permissions);

                    Console.WriteLine("🔑 API Key Generated Successfully");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"ID: {apiKey.Id}");
                    Console.WriteLine($"Name: {apiKey.Name}");
                    Console.WriteLine($"Description: {apiKey.Description}");
                    Console.WriteLine($"Created: {apiKey.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Expires: {apiKey.ExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}");
                    Console.WriteLine($"Permissions: {string.Join(", ", apiKey.Permissions)}");
                    Console.WriteLine();
                    Console.WriteLine("WARNING:  IMPORTANT: Store this API key securely. It cannot be retrieved again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate API key: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate API key");
                }
            }, nameOption, descriptionOption, expirationOption, permissionsOption);

            // List API keys
            var listCommand = new Command("list", "List all API keys");
            listCommand.SetHandler(async () =>
            {
                try
                {
                    var apiKeys = await _apiKeyManager.ListApiKeysAsync();
                    var stats = await _apiKeyManager.GetUsageStatisticsAsync();

                    Console.WriteLine("🔑 API Keys");
                    Console.WriteLine(new string('=', 20));
                    Console.WriteLine($"Total Keys: {stats.TotalKeys}");
                    Console.WriteLine($"Active Keys: {stats.ActiveKeys}");
                    Console.WriteLine($"Expired Keys: {stats.ExpiredKeys}");
                    Console.WriteLine($"Revoked Keys: {stats.RevokedKeys}");
                    Console.WriteLine();

                    foreach (var key in apiKeys)
                    {
                        var status = key.IsActive ? "SUCCESS: Active" : "ERROR: Inactive";
                        var expiration = key.ExpiresAt?.ToString("yyyy-MM-dd") ?? "Never";
                        var lastUsed = key.LastUsedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";

                        Console.WriteLine($"{status} {key.Name}");
                        Console.WriteLine($"  ID: {key.Id}");
                        Console.WriteLine($"  Description: {key.Description}");
                        Console.WriteLine($"  Created: {key.CreatedAt:yyyy-MM-dd}");
                        Console.WriteLine($"  Expires: {expiration}");
                        Console.WriteLine($"  Last Used: {lastUsed}");
                        Console.WriteLine($"  Usage Count: {key.UsageCount}");
                        Console.WriteLine($"  Permissions: {string.Join(", ", key.Permissions)}");
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to list API keys: {ex.Message}");
                    _logger.LogError(ex, "Failed to list API keys");
                }
            });

            // Revoke API key
            var revokeCommand = new Command("revoke", "Revoke an API key");
            var keyIdOption = new Option<string>("--key-id", "ID of the key to revoke");
            var reasonOption = new Option<string>("--reason", "Reason for revocation");

            revokeCommand.AddOption(keyIdOption);
            revokeCommand.AddOption(reasonOption);

            revokeCommand.SetHandler(async (string keyId, string reason) =>
            {
                try
                {
                    var success = await _apiKeyManager.RevokeApiKeyAsync(keyId);
                    if (success)
                    {
                        Console.WriteLine($"SUCCESS: API key {keyId} revoked successfully");
                        if (!string.IsNullOrEmpty(reason))
                        {
                            Console.WriteLine($"Reason: {reason}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: Failed to revoke API key {keyId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to revoke API key: {ex.Message}");
                    _logger.LogError(ex, "Failed to revoke API key");
                }
            }, keyIdOption, reasonOption);

            // Rotate API key
            var rotateCommand = new Command("rotate", "Rotate an API key (generate new, revoke old)");
            var rotateKeyIdOption = new Option<string>("--key-id", "ID of the key to rotate");

            rotateCommand.AddOption(rotateKeyIdOption);

            rotateCommand.SetHandler(async (string keyId) =>
            {
                try
                {
                    var newKey = await _apiKeyManager.RotateApiKeyAsync(keyId);
                    Console.WriteLine("Processing API Key Rotated Successfully");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Old Key ID: {keyId}");
                    Console.WriteLine($"New Key ID: {newKey.Id}");
                    Console.WriteLine($"Name: {newKey.Name}");
                    Console.WriteLine($"Description: {newKey.Description}");
                    Console.WriteLine();
                    Console.WriteLine("WARNING:  IMPORTANT: Update your applications with the new API key.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to rotate API key: {ex.Message}");
                    _logger.LogError(ex, "Failed to rotate API key");
                }
            }, rotateKeyIdOption);

            apiKeyCommand.AddCommand(generateCommand);
            apiKeyCommand.AddCommand(listCommand);
            apiKeyCommand.AddCommand(revokeCommand);
            apiKeyCommand.AddCommand(rotateCommand);

            return apiKeyCommand;
        }
    }
}
