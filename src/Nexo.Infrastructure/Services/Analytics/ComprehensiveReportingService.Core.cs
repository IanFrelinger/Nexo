using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Analytics;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Core.Application.Interfaces.Performance;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Core comprehensive reporting functionality.
    /// </summary>
    public partial class ComprehensiveReportingService
    {
        public async Task<ComprehensiveReport> GenerateComprehensiveReportAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            ReportConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating comprehensive report for period {StartTime} to {EndTime}",
                    startTime, endTime);

                configuration ??= new ReportConfiguration
                {
                    IncludeUsageCharts = true,
                    IncludePerformanceCharts = true,
                    IncludeCostCharts = true,
                    IncludeRawData = false,
                    IncludeMethodology = true,
                    IncludeGlossary = true
                };

                var report = new ComprehensiveReport
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    StartTime = startTime,
                    EndTime = endTime,
                    Configuration = configuration
                };

                // Generate usage report
                report.UsageReport = await GenerateUsageReportAsync(startTime, endTime, cancellationToken);

                // Generate performance report
                report.PerformanceReport = await GeneratePerformanceReportAsync(startTime, endTime, cancellationToken);

                // Generate security report
                report.SecurityReport = await GenerateSecurityReportAsync(startTime, endTime, cancellationToken);

                // Generate cost report
                report.CostReport = await GenerateCostReportAsync(startTime, endTime, cancellationToken);

                // Generate executive summary
                report.ExecutiveSummary = GenerateExecutiveSummaryObject(report);

                // Generate recommendations
                report.Recommendations = GenerateRecommendations(report);

                _logger.LogInformation("Comprehensive report generated successfully with {SectionCount} sections",
                    GetIncludedSectionCount(configuration));

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comprehensive report");
                throw;
            }
        }
    }
}
