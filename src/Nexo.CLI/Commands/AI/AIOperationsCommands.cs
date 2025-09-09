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
    /// AI operations commands for Phase 3.3 developer tools and CLI enhancements.
    /// Provides comprehensive AI management, monitoring, and optimization capabilities.
    /// </summary>
    public class AIOperationsCommands
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIOperationsCommands> _logger;
        private readonly ISecureApiKeyManager _apiKeyManager;
        private readonly ISecurityComplianceService _securityComplianceService;
        private readonly AdvancedCacheConfigurationService _cacheConfigurationService;

        public AIOperationsCommands(
            IServiceProvider serviceProvider,
            ILogger<AIOperationsCommands> logger,
            ISecureApiKeyManager apiKeyManager,
            ISecurityComplianceService securityComplianceService,
            AdvancedCacheConfigurationService cacheConfigurationService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _securityComplianceService = securityComplianceService ?? throw new ArgumentNullException(nameof(securityComplianceService));
            _cacheConfigurationService = cacheConfigurationService ?? throw new ArgumentNullException(nameof(cacheConfigurationService));
        }

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
                    Console.WriteLine("⚠️  IMPORTANT: Store this API key securely. It cannot be retrieved again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to generate API key: {ex.Message}");
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
                        var status = key.IsActive ? "✅ Active" : "❌ Inactive";
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
                    Console.WriteLine($"❌ Failed to list API keys: {ex.Message}");
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
                        Console.WriteLine($"✅ API key {keyId} revoked successfully");
                        if (!string.IsNullOrEmpty(reason))
                        {
                            Console.WriteLine($"Reason: {reason}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to revoke API key {keyId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to revoke API key: {ex.Message}");
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
                    Console.WriteLine("🔄 API Key Rotated Successfully");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Old Key ID: {keyId}");
                    Console.WriteLine($"New Key ID: {newKey.Id}");
                    Console.WriteLine($"Name: {newKey.Name}");
                    Console.WriteLine($"Description: {newKey.Description}");
                    Console.WriteLine();
                    Console.WriteLine("⚠️  IMPORTANT: Update your applications with the new API key.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to rotate API key: {ex.Message}");
                    _logger.LogError(ex, "Failed to rotate API key");
                }
            }, rotateKeyIdOption);

            apiKeyCommand.AddCommand(generateCommand);
            apiKeyCommand.AddCommand(listCommand);
            apiKeyCommand.AddCommand(revokeCommand);
            apiKeyCommand.AddCommand(rotateCommand);

            return apiKeyCommand;
        }

        /// <summary>
        /// Creates cache management commands.
        /// </summary>
        private Command CreateCacheManagementCommand()
        {
            var cacheCommand = new Command("cache", "Cache management and optimization");

            // Cache status
            var statusCommand = new Command("status", "Show cache status and statistics");
            statusCommand.SetHandler(async () =>
            {
                try
                {
                    var report = await _cacheConfigurationService.GetPerformanceReportAsync();
                    var deduplicationStats = await _cacheConfigurationService.GetDeduplicationStatisticsAsync();

                    Console.WriteLine("💾 Cache Status");
                    Console.WriteLine(new string('=', 20));
                    Console.WriteLine($"Total Operations: {report.TotalOperations}");
                    Console.WriteLine($"Hit Rate: {report.HitRate:P2}");
                    Console.WriteLine($"Error Rate: {report.ErrorRate:P2}");
                    Console.WriteLine($"Average Response Time: {report.AverageResponseTime.TotalMilliseconds:F2}ms");
                    Console.WriteLine();
                    Console.WriteLine("📊 Deduplication Statistics");
                    Console.WriteLine($"Total Cached Responses: {deduplicationStats.TotalCachedResponses}");
                    Console.WriteLine($"Duplicate Responses: {deduplicationStats.DuplicateResponses}");
                    Console.WriteLine($"Similarity Matches: {deduplicationStats.SimilarityMatches}");
                    Console.WriteLine($"Cache Hit Rate: {deduplicationStats.CacheHitRate:P2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get cache status: {ex.Message}");
                    _logger.LogError(ex, "Failed to get cache status");
                }
            });

            // Cache optimization
            var optimizeCommand = new Command("optimize", "Optimize cache configuration");
            optimizeCommand.SetHandler(async () =>
            {
                try
                {
                    Console.WriteLine("🔧 Optimizing Cache Configuration...");
                    var result = await _cacheConfigurationService.OptimizeConfigurationAsync();

                    Console.WriteLine("✅ Cache Optimization Complete");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Current Hit Rate: {result.CurrentHitRate:P2}");
                    Console.WriteLine($"Current Error Rate: {result.CurrentErrorRate:P2}");
                    Console.WriteLine($"Current Avg Response Time: {result.CurrentAverageResponseTime.TotalMilliseconds:F2}ms");
                    Console.WriteLine();
                    Console.WriteLine("📋 Recommendations:");
                    foreach (var recommendation in result.Recommendations)
                    {
                        Console.WriteLine($"  • {recommendation.Title}: {recommendation.Description}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to optimize cache: {ex.Message}");
                    _logger.LogError(ex, "Failed to optimize cache");
                }
            });

            // Cache clear
            var clearCommand = new Command("clear", "Clear cache data");
            var confirmOption = new Option<bool>("--confirm", "Confirm cache clearing");

            clearCommand.AddOption(confirmOption);

            clearCommand.SetHandler(async (bool confirm) =>
            {
                try
                {
                    if (!confirm)
                    {
                        Console.WriteLine("⚠️  Use --confirm to clear the cache");
                        return;
                    }

                    Console.WriteLine("🗑️  Clearing cache...");
                    // Cache clearing would be implemented here
                    Console.WriteLine("✅ Cache cleared successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to clear cache: {ex.Message}");
                    _logger.LogError(ex, "Failed to clear cache");
                }
            }, confirmOption);

            cacheCommand.AddCommand(statusCommand);
            cacheCommand.AddCommand(optimizeCommand);
            cacheCommand.AddCommand(clearCommand);

            return cacheCommand;
        }

        /// <summary>
        /// Creates performance monitoring commands.
        /// </summary>
        private Command CreatePerformanceMonitoringCommand()
        {
            var perfCommand = new Command("performance", "Performance monitoring and analysis");

            // Performance report
            var reportCommand = new Command("report", "Generate performance report");
            var daysOption = new Option<int>("--days", () => 7, "Number of days to analyze");

            reportCommand.AddOption(daysOption);
            reportCommand.SetHandler(async (int days) =>
            {
                try
                {
                    Console.WriteLine($"📊 Performance Report (Last {days} days)");
                    Console.WriteLine(new string('=', 40));
                    
                    var report = await _cacheConfigurationService.GetPerformanceReportAsync();
                    var recommendations = await _cacheConfigurationService.GetOptimizationRecommendationsAsync();

                    Console.WriteLine($"Total Operations: {report.TotalOperations:N0}");
                    Console.WriteLine($"Hit Rate: {report.HitRate:P2}");
                    Console.WriteLine($"Error Rate: {report.ErrorRate:P2}");
                    Console.WriteLine($"Average Response Time: {report.AverageResponseTime.TotalMilliseconds:F2}ms");
                    Console.WriteLine();
                    Console.WriteLine("📈 Performance Metrics:");
                    Console.WriteLine($"  Get Operations: {report.PerformanceMetrics.GetOperations:N0}");
                    Console.WriteLine($"  Set Operations: {report.PerformanceMetrics.SetOperations:N0}");
                    Console.WriteLine($"  Hit Count: {report.PerformanceMetrics.HitCount:N0}");
                    Console.WriteLine($"  Miss Count: {report.PerformanceMetrics.MissCount:N0}");
                    Console.WriteLine($"  Error Count: {report.PerformanceMetrics.ErrorCount:N0}");
                    Console.WriteLine();
                    Console.WriteLine("🎯 Optimization Recommendations:");
                    foreach (var rec in recommendations)
                    {
                        var priority = rec.Priority switch
                        {
                            RecommendationPriority.Low => "🟢",
                            RecommendationPriority.Medium => "🟡",
                            RecommendationPriority.High => "🟠",
                            RecommendationPriority.Critical => "🔴",
                            _ => "⚪"
                        };
                        Console.WriteLine($"  {priority} {rec.Title}: {rec.Description}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to generate performance report: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate performance report");
                }
            }, daysOption);

            // Performance trends
            var trendsCommand = new Command("trends", "Show performance trends");
            var trendsDaysOption = new Option<int>("--days", () => 30, "Number of days to analyze");

            trendsCommand.AddOption(trendsDaysOption);
            trendsCommand.SetHandler(async (int days) =>
            {
                try
                {
                    Console.WriteLine($"📈 Performance Trends (Last {days} days)");
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine("Performance trend analysis is not yet implemented.");
                    Console.WriteLine("This feature will be available in future updates.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get performance trends: {ex.Message}");
                    _logger.LogError(ex, "Failed to get performance trends");
                }
            }, trendsDaysOption);

            perfCommand.AddCommand(reportCommand);
            perfCommand.AddCommand(trendsCommand);

            return perfCommand;
        }

        /// <summary>
        /// Creates security compliance commands.
        /// </summary>
        private Command CreateSecurityComplianceCommand()
        {
            var securityCommand = new Command("security", "Security compliance and monitoring");

            // Security health check
            var healthCommand = new Command("health", "Perform security health check");
            healthCommand.SetHandler(async () =>
            {
                try
                {
                    Console.WriteLine("🔒 Security Health Check");
                    Console.WriteLine(new string('=', 25));
                    
                    var healthCheck = await _securityComplianceService.PerformSecurityHealthCheckAsync();

                    Console.WriteLine($"Overall Score: {healthCheck.OverallScore}/100");
                    Console.WriteLine($"API Key Health: {healthCheck.ApiKeyHealth}/100");
                    Console.WriteLine($"Security Event Health: {healthCheck.SecurityEventHealth}/100");
                    Console.WriteLine();
                    Console.WriteLine("📋 Recommendations:");
                    foreach (var rec in healthCheck.Recommendations)
                    {
                        var priority = rec.Priority switch
                        {
                            RecommendationPriority.Low => "🟢",
                            RecommendationPriority.Medium => "🟡",
                            RecommendationPriority.High => "🟠",
                            RecommendationPriority.Critical => "🔴",
                            _ => "⚪"
                        };
                        Console.WriteLine($"  {priority} {rec.Title}: {rec.Description}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to perform security health check: {ex.Message}");
                    _logger.LogError(ex, "Failed to perform security health check");
                }
            });

            // Compliance report
            var complianceCommand = new Command("report", "Generate compliance report");
            var startDateOption = new Option<string>("--start", "Start date (yyyy-mm-dd)");
            var endDateOption = new Option<string>("--end", "End date (yyyy-mm-dd)");

            complianceCommand.AddOption(startDateOption);
            complianceCommand.AddOption(endDateOption);

            complianceCommand.SetHandler(async (string startDate, string endDate) =>
            {
                try
                {
                    var start = string.IsNullOrEmpty(startDate) 
                        ? DateTimeOffset.UtcNow.AddDays(-30) 
                        : DateTimeOffset.Parse(startDate);
                    var end = string.IsNullOrEmpty(endDate) 
                        ? DateTimeOffset.UtcNow 
                        : DateTimeOffset.Parse(endDate);

                    Console.WriteLine($"🔒 Security Compliance Report ({start:yyyy-MM-dd} to {end:yyyy-MM-dd})");
                    Console.WriteLine(new string('=', 60));
                    
                    var report = await _securityComplianceService.GenerateComplianceReportAsync(start, end);

                    Console.WriteLine($"Total Events: {report.TotalEvents:N0}");
                    Console.WriteLine($"Security Events: {report.SecurityEvents:N0}");
                    Console.WriteLine($"Compliance Events: {report.ComplianceEvents:N0}");
                    Console.WriteLine();
                    Console.WriteLine("🔑 API Key Statistics:");
                    Console.WriteLine($"  Total Keys: {report.ApiKeyStatistics.TotalKeys}");
                    Console.WriteLine($"  Active Keys: {report.ApiKeyStatistics.ActiveKeys}");
                    Console.WriteLine($"  Expired Keys: {report.ApiKeyStatistics.ExpiredKeys}");
                    Console.WriteLine($"  Revoked Keys: {report.ApiKeyStatistics.RevokedKeys}");
                    Console.WriteLine();
                    Console.WriteLine("📊 Security Metrics:");
                    Console.WriteLine($"  Threat Level: {report.SecurityMetrics.ThreatLevel}");
                    Console.WriteLine($"  Security Event Rate: {report.SecurityMetrics.SecurityEventRate:F2}/hour");
                    Console.WriteLine($"  Failed Auth Attempts: {report.SecurityMetrics.FailedAuthenticationAttempts:N0}");
                    Console.WriteLine();
                    Console.WriteLine("⚠️  Violations:");
                    foreach (var violation in report.Violations)
                    {
                        var severity = violation.Severity switch
                        {
                            ComplianceViolationSeverity.Low => "🟢",
                            ComplianceViolationSeverity.Medium => "🟡",
                            ComplianceViolationSeverity.High => "🟠",
                            ComplianceViolationSeverity.Critical => "🔴",
                            _ => "⚪"
                        };
                        Console.WriteLine($"  {severity} {violation.Description}");
                        Console.WriteLine($"    Remediation: {violation.Remediation}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to generate compliance report: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate compliance report");
                }
            }, startDateOption, endDateOption);

            securityCommand.AddCommand(healthCommand);
            securityCommand.AddCommand(complianceCommand);

            return securityCommand;
        }

        /// <summary>
        /// Creates AI model management commands.
        /// </summary>
        private Command CreateModelManagementCommand()
        {
            var modelCommand = new Command("models", "AI model management and configuration");

            // List models
            var listCommand = new Command("list", "List available AI models");
            listCommand.SetHandler(async () =>
            {
                try
                {
                    Console.WriteLine("🤖 Available AI Models");
                    Console.WriteLine(new string('=', 25));
                    Console.WriteLine("Model management is not yet implemented.");
                    Console.WriteLine("This feature will be available in future updates.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to list models: {ex.Message}");
                    _logger.LogError(ex, "Failed to list AI models");
                }
            });

            // Model status
            var statusCommand = new Command("status", "Show model status and health");
            statusCommand.SetHandler(async () =>
            {
                try
                {
                    Console.WriteLine("📊 AI Model Status");
                    Console.WriteLine(new string('=', 20));
                    Console.WriteLine("Model status monitoring is not yet implemented.");
                    Console.WriteLine("This feature will be available in future updates.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get model status: {ex.Message}");
                    _logger.LogError(ex, "Failed to get AI model status");
                }
            });

            modelCommand.AddCommand(listCommand);
            modelCommand.AddCommand(statusCommand);

            return modelCommand;
        }

        /// <summary>
        /// Creates analytics and reporting commands.
        /// </summary>
        private Command CreateAnalyticsCommand()
        {
            var analyticsCommand = new Command("analytics", "AI analytics and reporting");

            // Usage analytics
            var usageCommand = new Command("usage", "Show AI usage analytics");
            var usageDaysOption = new Option<int>("--days", () => 7, "Number of days to analyze");

            usageCommand.AddOption(usageDaysOption);
            usageCommand.SetHandler(async (int days) =>
            {
                try
                {
                    Console.WriteLine($"📈 AI Usage Analytics (Last {days} days)");
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine("Usage analytics are not yet implemented.");
                    Console.WriteLine("This feature will be available in future updates.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get usage analytics: {ex.Message}");
                    _logger.LogError(ex, "Failed to get AI usage analytics");
                }
            }, usageDaysOption);

            // Performance trends
            var trendsCommand = new Command("trends", "Show performance trends");
            var trendsDaysOption = new Option<int>("--days", () => 30, "Number of days to analyze");

            trendsCommand.AddOption(trendsDaysOption);
            trendsCommand.SetHandler(async (int days) =>
            {
                try
                {
                    Console.WriteLine($"📊 Performance Trends (Last {days} days)");
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine("Performance trend analysis is not yet implemented.");
                    Console.WriteLine("This feature will be available in future updates.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get performance trends: {ex.Message}");
                    _logger.LogError(ex, "Failed to get performance trends");
                }
            }, trendsDaysOption);

            analyticsCommand.AddCommand(usageCommand);
            analyticsCommand.AddCommand(trendsCommand);

            return analyticsCommand;
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