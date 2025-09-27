using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Analytics;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Report export functionality for comprehensive reporting.
    /// </summary>
    public partial class ComprehensiveReportingService
    {
        public async Task<ReportExport> ExportReportAsync(
            ComprehensiveReport report,
            ReportExportFormat format,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting comprehensive report to {Format} format", format);

                string content;

                switch (format)
                {
                    case ReportExportFormat.Html:
                        content = await GenerateHtmlReportAsync(report, cancellationToken);
                        break;
                    case ReportExportFormat.Json:
                        content = await GenerateJsonReportAsync(report, cancellationToken);
                        break;
                    case ReportExportFormat.Pdf:
                        content = await GeneratePdfReportAsync(report, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {format}");
                }

                return new ReportExport
                {
                    Format = format,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    Report = report,
                    Data = content
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting comprehensive report");
                throw;
            }
        }
    }
}
