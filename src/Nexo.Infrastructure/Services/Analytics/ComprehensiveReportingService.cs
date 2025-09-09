using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Analytics;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Caching.Advanced;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Comprehensive reporting service that integrates all analytics and reporting capabilities.
    /// Part of Phase 3.3 analytics and reporting features.
    /// </summary>
    public class ComprehensiveReportingService : IComprehensiveReportingService
    {
        private readonly ILogger<ComprehensiveReportingService> _logger;
        private readonly IAIAnalyticsService _aiAnalyticsService;
        private readonly ISecurityComplianceService _securityComplianceService;
        private readonly AdvancedCacheConfigurationService _cacheConfigurationService;

        public ComprehensiveReportingService(
            ILogger<ComprehensiveReportingService> logger,
            IAIAnalyticsService aiAnalyticsService,
            ISecurityComplianceService securityComplianceService,
            AdvancedCacheConfigurationService cacheConfigurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiAnalyticsService = aiAnalyticsService ?? throw new ArgumentNullException(nameof(aiAnalyticsService));
            _securityComplianceService = securityComplianceService ?? throw new ArgumentNullException(nameof(securityComplianceService));
            _cacheConfigurationService = cacheConfigurationService ?? throw new ArgumentNullException(nameof(cacheConfigurationService));
        }

        public async Task<ComprehensiveReport> GenerateComprehensiveReportAsync(
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            ReportConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating comprehensive report for period {StartDate} to {EndDate}",
                    startDate, endDate);

                var report = new ComprehensiveReport
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    StartDate = startDate,
                    EndDate = endDate,
                    Configuration = configuration
                };

                // Generate AI analytics report
                if (configuration.IncludeAIAnalytics)
                {
                    report.AIAnalytics = await _aiAnalyticsService.GenerateReportAsync(startDate, endDate, cancellationToken);
                }

                // Generate security compliance report
                if (configuration.IncludeSecurityCompliance)
                {
                    report.SecurityCompliance = await _securityComplianceService.GenerateComplianceReportAsync(startDate, endDate, cancellationToken);
                }

                // Generate cache performance report
                if (configuration.IncludeCachePerformance)
                {
                    report.CachePerformance = await _cacheConfigurationService.GetPerformanceReportAsync(cancellationToken);
                }

                // Generate system performance insights
                if (configuration.IncludeSystemPerformance)
                {
                    report.SystemPerformance = await GenerateSystemPerformanceReportAsync(cancellationToken);
                }

                // Generate executive summary
                report.ExecutiveSummary = await GenerateExecutiveSummaryAsync(report, cancellationToken);

                // Generate recommendations
                report.Recommendations = await GenerateComprehensiveRecommendationsAsync(report, cancellationToken);

                _logger.LogInformation("Comprehensive report generated successfully");

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comprehensive report");
                throw;
            }
        }

        public async Task<string> ExportReportAsync(
            ComprehensiveReport report,
            ReportFormat format,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting comprehensive report to {Format} format at {OutputPath}",
                    format, outputPath);

                string content;
                string extension;

                switch (format)
                {
                    case ReportFormat.Markdown:
                        content = await GenerateMarkdownReportAsync(report, cancellationToken);
                        extension = ".md";
                        break;
                    case ReportFormat.Html:
                        content = await GenerateHtmlReportAsync(report, cancellationToken);
                        extension = ".html";
                        break;
                    case ReportFormat.Json:
                        content = await GenerateJsonReportAsync(report, cancellationToken);
                        extension = ".json";
                        break;
                    case ReportFormat.Pdf:
                        content = await GeneratePdfReportAsync(report, cancellationToken);
                        extension = ".pdf";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported report format: {format}");
                }

                // Ensure output path has correct extension
                if (!outputPath.EndsWith(extension))
                {
                    outputPath += extension;
                }

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the report
                await File.WriteAllTextAsync(outputPath, content, cancellationToken);

                _logger.LogInformation("Report exported successfully to {OutputPath}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to {Format} format", format);
                throw;
            }
        }

        public async Task<IEnumerable<ReportTemplate>> GetAvailableTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var templates = new List<ReportTemplate>
                {
                    new ReportTemplate
                    {
                        Id = "executive-summary",
                        Name = "Executive Summary",
                        Description = "High-level overview for executives and stakeholders",
                        Sections = new[] { "Executive Summary", "Key Metrics", "Top Recommendations" }
                    },
                    new ReportTemplate
                    {
                        Id = "technical-detailed",
                        Name = "Technical Detailed Report",
                        Description = "Comprehensive technical report with all sections",
                        Sections = new[] { "AI Analytics", "Security Compliance", "Cache Performance", "System Performance", "Detailed Recommendations" }
                    },
                    new ReportTemplate
                    {
                        Id = "security-focused",
                        Name = "Security Focused Report",
                        Description = "Report focused on security and compliance aspects",
                        Sections = new[] { "Security Compliance", "API Key Management", "Audit Logs", "Security Recommendations" }
                    },
                    new ReportTemplate
                    {
                        Id = "performance-focused",
                        Name = "Performance Focused Report",
                        Description = "Report focused on performance and optimization",
                        Sections = new[] { "AI Analytics", "Cache Performance", "System Performance", "Performance Recommendations" }
                    },
                    new ReportTemplate
                    {
                        Id = "custom",
                        Name = "Custom Report",
                        Description = "Customizable report with selected sections",
                        Sections = new[] { "Custom" }
                    }
                };

                return await Task.FromResult(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available report templates");
                return Enumerable.Empty<ReportTemplate>();
            }
        }

        public async Task<ComprehensiveReport> GenerateReportFromTemplateAsync(
            string templateId,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var templates = await GetAvailableTemplatesAsync(cancellationToken);
                var template = templates.FirstOrDefault(t => t.Id == templateId);

                if (template == null)
                {
                    throw new ArgumentException($"Template not found: {templateId}");
                }

                var configuration = new ReportConfiguration
                {
                    IncludeAIAnalytics = template.Sections.Contains("AI Analytics"),
                    IncludeSecurityCompliance = template.Sections.Contains("Security Compliance"),
                    IncludeCachePerformance = template.Sections.Contains("Cache Performance"),
                    IncludeSystemPerformance = template.Sections.Contains("System Performance"),
                    IncludeExecutiveSummary = template.Sections.Contains("Executive Summary"),
                    IncludeRecommendations = template.Sections.Contains("Top Recommendations") || template.Sections.Contains("Detailed Recommendations")
                };

                return await GenerateComprehensiveReportAsync(startDate, endDate, configuration, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report from template {TemplateId}", templateId);
                throw;
            }
        }

        private async Task<SystemPerformanceReport> GenerateSystemPerformanceReportAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
                var cpuTime = process.TotalProcessorTime.TotalMilliseconds;

                return new SystemPerformanceReport
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    MemoryUsageMB = memoryUsage,
                    CpuTimeMs = cpuTime,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    StartTime = process.StartTime,
                    Uptime = DateTime.Now - process.StartTime,
                    // Additional system metrics would be collected here
                    DiskUsage = await GetDiskUsageAsync(),
                    NetworkStats = await GetNetworkStatsAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating system performance report");
                return new SystemPerformanceReport { GeneratedAt = DateTimeOffset.UtcNow };
            }
        }

        private async Task<ExecutiveSummary> GenerateExecutiveSummaryAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            try
            {
                var summary = new ExecutiveSummary
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    Period = $"{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}"
                };

                // Key metrics
                if (report.AIAnalytics != null)
                {
                    summary.TotalAIOperations = report.AIAnalytics.TotalOperations;
                    summary.AISuccessRate = report.AIAnalytics.SuccessRate;
                    summary.TotalAICost = report.AIAnalytics.TotalCost;
                    summary.AverageResponseTime = report.AIAnalytics.AverageResponseTime;
                }

                if (report.SecurityCompliance != null)
                {
                    summary.ComplianceScore = report.SecurityCompliance.ComplianceScore;
                    summary.ActiveApiKeys = report.SecurityCompliance.ApiKeyStatistics.ActiveKeys;
                    summary.SecurityEvents = report.SecurityCompliance.AuditStatistics.TotalEvents;
                }

                if (report.CachePerformance != null)
                {
                    summary.CacheHitRate = report.CachePerformance.HitRate;
                    summary.CacheOperations = report.CachePerformance.TotalOperations;
                }

                // Overall health score
                summary.OverallHealthScore = CalculateOverallHealthScore(report);

                // Key insights
                summary.KeyInsights = GenerateKeyInsights(report);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating executive summary");
                return new ExecutiveSummary { GeneratedAt = DateTimeOffset.UtcNow };
            }
        }

        private async Task<List<ComprehensiveRecommendation>> GenerateComprehensiveRecommendationsAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            try
            {
                var recommendations = new List<ComprehensiveRecommendation>();

                // AI Analytics recommendations
                if (report.AIAnalytics?.Recommendations != null)
                {
                    foreach (var rec in report.AIAnalytics.Recommendations)
                    {
                        recommendations.Add(new ComprehensiveRecommendation
                        {
                            Category = "AI Analytics",
                            Type = rec.Type.ToString(),
                            Priority = rec.Priority.ToString(),
                            Title = rec.Title,
                            Description = rec.Description,
                            Impact = rec.Impact,
                            Effort = rec.Effort.ToString()
                        });
                    }
                }

                // Security recommendations
                if (report.SecurityCompliance?.Recommendations != null)
                {
                    foreach (var rec in report.SecurityCompliance.Recommendations)
                    {
                        recommendations.Add(new ComprehensiveRecommendation
                        {
                            Category = "Security",
                            Type = rec.Type.ToString(),
                            Priority = rec.Priority.ToString(),
                            Title = rec.Title,
                            Description = rec.Description,
                            Impact = rec.Impact,
                            Effort = rec.Effort.ToString()
                        });
                    }
                }

                // Cache performance recommendations
                if (report.CachePerformance?.Recommendations != null)
                {
                    foreach (var rec in report.CachePerformance.Recommendations)
                    {
                        recommendations.Add(new ComprehensiveRecommendation
                        {
                            Category = "Cache Performance",
                            Type = rec.Type.ToString(),
                            Priority = rec.Priority.ToString(),
                            Title = rec.Title,
                            Description = rec.Description,
                            Impact = rec.Impact,
                            Effort = rec.Effort.ToString()
                        });
                    }
                }

                // Sort by priority
                return recommendations.OrderByDescending(r => r.Priority).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comprehensive recommendations");
                return new List<ComprehensiveRecommendation>();
            }
        }

        private async Task<string> GenerateMarkdownReportAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("# Comprehensive System Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
            sb.AppendLine($"**Period:** {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine();

            // Executive Summary
            if (report.ExecutiveSummary != null)
            {
                sb.AppendLine("## Executive Summary");
                sb.AppendLine();
                sb.AppendLine($"**Overall Health Score:** {report.ExecutiveSummary.OverallHealthScore:P2}");
                sb.AppendLine($"**Total AI Operations:** {report.ExecutiveSummary.TotalAIOperations:N0}");
                sb.AppendLine($"**AI Success Rate:** {report.ExecutiveSummary.AISuccessRate:P2}");
                sb.AppendLine($"**Compliance Score:** {report.ExecutiveSummary.ComplianceScore:P2}");
                sb.AppendLine();

                if (report.ExecutiveSummary.KeyInsights.Any())
                {
                    sb.AppendLine("### Key Insights");
                    sb.AppendLine();
                    foreach (var insight in report.ExecutiveSummary.KeyInsights)
                    {
                        sb.AppendLine($"- {insight}");
                    }
                    sb.AppendLine();
                }
            }

            // AI Analytics
            if (report.AIAnalytics != null)
            {
                sb.AppendLine("## AI Analytics");
                sb.AppendLine();
                sb.AppendLine($"- **Total Operations:** {report.AIAnalytics.TotalOperations:N0}");
                sb.AppendLine($"- **Success Rate:** {report.AIAnalytics.SuccessRate:P2}");
                sb.AppendLine($"- **Average Response Time:** {report.AIAnalytics.AverageResponseTime:F0}ms");
                sb.AppendLine($"- **Total Cost:** ${report.AIAnalytics.TotalCost:F2}");
                sb.AppendLine();
            }

            // Security Compliance
            if (report.SecurityCompliance != null)
            {
                sb.AppendLine("## Security Compliance");
                sb.AppendLine();
                sb.AppendLine($"- **Compliance Score:** {report.SecurityCompliance.ComplianceScore:P2}");
                sb.AppendLine($"- **Active API Keys:** {report.SecurityCompliance.ApiKeyStatistics.ActiveKeys}");
                sb.AppendLine($"- **Total Audit Events:** {report.SecurityCompliance.AuditStatistics.TotalEvents:N0}");
                sb.AppendLine($"- **Success Rate:** {report.SecurityCompliance.AuditStatistics.SuccessRate:P2}");
                sb.AppendLine();
            }

            // Cache Performance
            if (report.CachePerformance != null)
            {
                sb.AppendLine("## Cache Performance");
                sb.AppendLine();
                sb.AppendLine($"- **Hit Rate:** {report.CachePerformance.HitRate:P2}");
                sb.AppendLine($"- **Total Operations:** {report.CachePerformance.TotalOperations:N0}");
                sb.AppendLine($"- **Error Rate:** {report.CachePerformance.ErrorRate:P2}");
                sb.AppendLine();
            }

            // Recommendations
            if (report.Recommendations.Any())
            {
                sb.AppendLine("## Recommendations");
                sb.AppendLine();
                foreach (var rec in report.Recommendations)
                {
                    sb.AppendLine($"### {rec.Title}");
                    sb.AppendLine();
                    sb.AppendLine($"**Category:** {rec.Category}");
                    sb.AppendLine($"**Priority:** {rec.Priority}");
                    sb.AppendLine($"**Effort:** {rec.Effort}");
                    sb.AppendLine();
                    sb.AppendLine(rec.Description);
                    sb.AppendLine();
                    sb.AppendLine($"**Impact:** {rec.Impact}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private async Task<string> GenerateHtmlReportAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <title>Comprehensive System Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 40px; }");
            sb.AppendLine("        h1 { color: #333; }");
            sb.AppendLine("        h2 { color: #666; border-bottom: 2px solid #eee; }");
            sb.AppendLine("        .metric { background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }");
            sb.AppendLine("        .recommendation { background: #fff3cd; padding: 15px; margin: 10px 0; border-left: 4px solid #ffc107; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("    <h1>Comprehensive System Report</h1>");
            sb.AppendLine($"    <p><strong>Generated:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}</p>");
            sb.AppendLine($"    <p><strong>Period:</strong> {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}</p>");

            // Executive Summary
            if (report.ExecutiveSummary != null)
            {
                sb.AppendLine("    <h2>Executive Summary</h2>");
                sb.AppendLine($"    <div class=\"metric\">Overall Health Score: {report.ExecutiveSummary.OverallHealthScore:P2}</div>");
                sb.AppendLine($"    <div class=\"metric\">Total AI Operations: {report.ExecutiveSummary.TotalAIOperations:N0}</div>");
                sb.AppendLine($"    <div class=\"metric\">AI Success Rate: {report.ExecutiveSummary.AISuccessRate:P2}</div>");
            }

            // Recommendations
            if (report.Recommendations.Any())
            {
                sb.AppendLine("    <h2>Recommendations</h2>");
                foreach (var rec in report.Recommendations)
                {
                    sb.AppendLine("    <div class=\"recommendation\">");
                    sb.AppendLine($"        <h3>{rec.Title}</h3>");
                    sb.AppendLine($"        <p><strong>Category:</strong> {rec.Category} | <strong>Priority:</strong> {rec.Priority}</p>");
                    sb.AppendLine($"        <p>{rec.Description}</p>");
                    sb.AppendLine($"        <p><strong>Impact:</strong> {rec.Impact}</p>");
                    sb.AppendLine("    </div>");
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private async Task<string> GenerateJsonReportAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        private async Task<string> GeneratePdfReportAsync(
            ComprehensiveReport report,
            CancellationToken cancellationToken)
        {
            // For now, return HTML content that can be converted to PDF
            // In a real implementation, you would use a PDF generation library
            return await GenerateHtmlReportAsync(report, cancellationToken);
        }

        private double CalculateOverallHealthScore(ComprehensiveReport report)
        {
            var scores = new List<double>();

            if (report.AIAnalytics != null)
            {
                scores.Add(report.AIAnalytics.SuccessRate);
            }

            if (report.SecurityCompliance != null)
            {
                scores.Add(report.SecurityCompliance.ComplianceScore);
            }

            if (report.CachePerformance != null)
            {
                scores.Add(report.CachePerformance.HitRate);
            }

            return scores.Any() ? scores.Average() : 0.0;
        }

        private List<string> GenerateKeyInsights(ComprehensiveReport report)
        {
            var insights = new List<string>();

            if (report.AIAnalytics != null)
            {
                if (report.AIAnalytics.SuccessRate > 0.95)
                {
                    insights.Add("AI operations show excellent reliability with high success rates.");
                }
                else if (report.AIAnalytics.SuccessRate < 0.90)
                {
                    insights.Add("AI operations show concerning reliability issues that need attention.");
                }

                if (report.AIAnalytics.TotalCost > 100)
                {
                    insights.Add("AI costs are significant and should be monitored for optimization opportunities.");
                }
            }

            if (report.SecurityCompliance != null)
            {
                if (report.SecurityCompliance.ComplianceScore > 0.9)
                {
                    insights.Add("Security compliance is excellent with high scores across all areas.");
                }
                else if (report.SecurityCompliance.ComplianceScore < 0.7)
                {
                    insights.Add("Security compliance needs immediate attention with several areas requiring improvement.");
                }
            }

            if (report.CachePerformance != null)
            {
                if (report.CachePerformance.HitRate > 0.8)
                {
                    insights.Add("Cache performance is good with high hit rates.");
                }
                else if (report.CachePerformance.HitRate < 0.6)
                {
                    insights.Add("Cache performance could be improved with optimization strategies.");
                }
            }

            return insights;
        }

        private async Task<long> GetDiskUsageAsync()
        {
            try
            {
                var drive = new DriveInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                return drive.TotalSize - drive.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<NetworkStats> GetNetworkStatsAsync()
        {
            // Placeholder for network statistics
            return new NetworkStats
            {
                BytesReceived = 0,
                BytesSent = 0,
                PacketsReceived = 0,
                PacketsSent = 0
            };
        }
    }

    /// <summary>
    /// Comprehensive report model.
    /// </summary>
    public class ComprehensiveReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public ReportConfiguration Configuration { get; set; } = new();
        public AIAnalyticsReport? AIAnalytics { get; set; }
        public SecurityComplianceReport? SecurityCompliance { get; set; }
        public CachePerformanceReport? CachePerformance { get; set; }
        public SystemPerformanceReport? SystemPerformance { get; set; }
        public ExecutiveSummary? ExecutiveSummary { get; set; }
        public List<ComprehensiveRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Report configuration.
    /// </summary>
    public class ReportConfiguration
    {
        public bool IncludeAIAnalytics { get; set; } = true;
        public bool IncludeSecurityCompliance { get; set; } = true;
        public bool IncludeCachePerformance { get; set; } = true;
        public bool IncludeSystemPerformance { get; set; } = true;
        public bool IncludeExecutiveSummary { get; set; } = true;
        public bool IncludeRecommendations { get; set; } = true;
    }

    /// <summary>
    /// Report format options.
    /// </summary>
    public enum ReportFormat
    {
        Markdown,
        Html,
        Json,
        Pdf
    }

    /// <summary>
    /// Report template.
    /// </summary>
    public class ReportTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Sections { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Executive summary.
    /// </summary>
    public class ExecutiveSummary
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public string Period { get; set; } = string.Empty;
        public double OverallHealthScore { get; set; }
        public long TotalAIOperations { get; set; }
        public double AISuccessRate { get; set; }
        public decimal TotalAICost { get; set; }
        public double AverageResponseTime { get; set; }
        public double ComplianceScore { get; set; }
        public int ActiveApiKeys { get; set; }
        public long SecurityEvents { get; set; }
        public double CacheHitRate { get; set; }
        public long CacheOperations { get; set; }
        public List<string> KeyInsights { get; set; } = new();
    }

    /// <summary>
    /// System performance report.
    /// </summary>
    public class SystemPerformanceReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public long MemoryUsageMB { get; set; }
        public double CpuTimeMs { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public long DiskUsage { get; set; }
        public NetworkStats NetworkStats { get; set; } = new();
    }

    /// <summary>
    /// Network statistics.
    /// </summary>
    public class NetworkStats
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public long PacketsReceived { get; set; }
        public long PacketsSent { get; set; }
    }

    /// <summary>
    /// Comprehensive recommendation.
    /// </summary>
    public class ComprehensiveRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Effort { get; set; } = string.Empty;
    }
}
