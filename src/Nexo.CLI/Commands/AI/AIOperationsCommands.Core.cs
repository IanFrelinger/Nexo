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
    /// Core functionality for AI operations commands
    /// </summary>
    public partial class AIOperationsCommands
    {
        /// <summary>
        /// Creates the main AI operations command with all subcommands.
        /// </summary>
        public Command CreateAIOperationsCommand()
        {
            var aiCommand = new Command("ai", "AI operations and management");

            // API Key Management
            aiCommand.AddCommand(CreateApiKeyManagementCommand());

            // Cache Management
            aiCommand.AddCommand(CreateCacheManagementCommand());

            // Performance Monitoring
            aiCommand.AddCommand(CreatePerformanceMonitoringCommand());

            // Security Compliance
            aiCommand.AddCommand(CreateSecurityComplianceCommand());

            // AI Model Management
            aiCommand.AddCommand(CreateModelManagementCommand());

            // Analytics and Reporting
            aiCommand.AddCommand(CreateAnalyticsCommand());

            return aiCommand;
        }

        /// <summary>
        /// Parses expiration string to TimeSpan.
        /// </summary>
        private static TimeSpan? ParseExpiration(string? expiration)
        {
            if (string.IsNullOrEmpty(expiration))
                return null;

            var trimmed = expiration.Trim().ToLowerInvariant();
            var number = int.Parse(trimmed[..^1]);
            var unit = trimmed[^1];

            return unit switch
            {
                'd' => TimeSpan.FromDays(number),
                'w' => TimeSpan.FromDays(number * 7),
                'm' => TimeSpan.FromDays(number * 30),
                'y' => TimeSpan.FromDays(number * 365),
                _ => throw new ArgumentException($"Invalid expiration format: {expiration}")
            };
        }
    }
}
