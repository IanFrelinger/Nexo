using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Learning;
using Nexo.Core.Application.Models.Learning;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Learning
{
    /// <summary>
    /// Data management functionality for AILearningSystem.
    /// Handles learning data export, backup, and data management operations.
    /// </summary>
    public partial class AILearningSystem
    {
        /// <summary>
        /// Exports learning data for analysis and backup.
        /// </summary>
        public async Task<LearningDataExport> ExportLearningDataAsync(
            LearningDataExportOptions exportOptions,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Exporting learning data in format: {Format}", exportOptions.Format);

            try
            {
                // Use AI to generate export data
                var prompt = $@"
Export learning data with the following options:
- Format: {exportOptions.Format}
- Start Date: {exportOptions.StartDate}
- End Date: {exportOptions.EndDate}
- Data Types: {string.Join(", ", exportOptions.DataTypes)}
- Include Metadata: {exportOptions.IncludeMetadata}
- Compress: {exportOptions.Compress}

Requirements:
- Generate export data
- Format according to specification
- Include metadata if requested
- Compress if requested
- Provide export summary

Generate comprehensive learning data export.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                var export = new LearningDataExport
                {
                    Id = Guid.NewGuid().ToString(),
                    Format = exportOptions.Format,
                    Data = ParseExportData(response.Response),
                    Size = ParseExportSize(response.Response),
                    ExportedAt = DateTimeOffset.UtcNow.DateTime,
                    Metadata = ParseExportMetadata(response.Response)
                };

                _logger.LogInformation("Successfully exported learning data in format: {Format}", exportOptions.Format);
                return export;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting learning data in format: {Format}", exportOptions.Format);
                return new LearningDataExport
                {
                    Id = Guid.NewGuid().ToString(),
                    Format = exportOptions.Format,
                    ExportedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }
    }
}
