using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Enterprise;
using Nexo.Core.Application.Models.Enterprise;
using Nexo.Feature.Enterprise.Interfaces;
using Nexo.Feature.Enterprise.Models;

namespace Nexo.Infrastructure.Services.Enterprise
{
    /// <summary>
    /// Data management functionality for EnterpriseSecurityService.
    /// Handles data export, import, and migration operations.
    /// </summary>
    public partial class EnterpriseSecurityService
    {
        /// <summary>
        /// Exports security data based on AI analysis.
        /// </summary>
        public async Task<SecurityDataExportResult> ExportSecurityDataAsync(
            SecurityDataExportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting security data for {ProjectId}", request.ProjectId);

                var aiRequest = new SecurityDataExportAIRequest
                {
                    ProjectId = request.ProjectId,
                    ExportFormat = request.ExportFormat,
                    DataScope = request.DataScope,
                    ExportOptions = request.ExportOptions
                };

                var aiResponse = await _aiService.ExportSecurityDataAsync(aiRequest, cancellationToken);

                var exportData = ParseExportData(aiResponse.Content);
                var exportSize = ParseExportSize(aiResponse.Content);
                var recordCount = ParseRecordCount(aiResponse.Content);
                var exportMetadata = ParseExportMetadata(aiResponse.Content);

                return new SecurityDataExportResult
                {
                    Success = true,
                    Message = "Security data exported successfully",
                    ExportData = exportData,
                    ExportSize = exportSize,
                    RecordCount = recordCount,
                    ExportMetadata = exportMetadata,
                    ExportedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export security data for {ProjectId}", request.ProjectId);
                return new SecurityDataExportResult
                {
                    Success = false,
                    Message = ex.Message,
                    ExportedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }

        /// <summary>
        /// Imports security data based on AI analysis.
        /// </summary>
        public async Task<SecurityDataImportResult> ImportSecurityDataAsync(
            SecurityDataImportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Importing security data for {ProjectId}", request.ProjectId);

                var aiRequest = new SecurityDataImportAIRequest
                {
                    ProjectId = request.ProjectId,
                    ImportData = request.ImportData,
                    ImportFormat = request.ImportFormat,
                    ImportOptions = request.ImportOptions
                };

                var aiResponse = await _aiService.ImportSecurityDataAsync(aiRequest, cancellationToken);

                var importedCount = ParseImportedCount(aiResponse.Content);
                var skippedCount = ParseSkippedCount(aiResponse.Content);
                var errorCount = ParseErrorCount(aiResponse.Content);
                var importErrors = ParseImportErrors(aiResponse.Content);
                var importMetrics = ParseImportMetrics(aiResponse.Content);

                return new SecurityDataImportResult
                {
                    Success = true,
                    Message = "Security data imported successfully",
                    ImportedCount = importedCount,
                    SkippedCount = skippedCount,
                    ErrorCount = errorCount,
                    ImportErrors = importErrors,
                    ImportMetrics = importMetrics,
                    ImportedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import security data for {ProjectId}", request.ProjectId);
                return new SecurityDataImportResult
                {
                    Success = false,
                    Message = ex.Message,
                    ImportedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }
    }
}
