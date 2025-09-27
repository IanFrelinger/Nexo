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
    /// Reporting functionality for EnterpriseSecurityService.
    /// Handles report generation, delivery, and management.
    /// </summary>
    public partial class EnterpriseSecurityService
    {
        /// <summary>
        /// Generates security reports based on AI analysis.
        /// </summary>
        public async Task<SecurityReportingResult> GenerateSecurityReportsAsync(
            SecurityReportingRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating security reports for {ProjectId}", request.ProjectId);

                var aiRequest = new SecurityReportingAIRequest
                {
                    ProjectId = request.ProjectId,
                    ReportTypes = request.ReportTypes,
                    ReportFormat = request.ReportFormat,
                    DeliveryPreferences = request.DeliveryPreferences
                };

                var aiResponse = await _aiService.GenerateSecurityReportsAsync(aiRequest, cancellationToken);

                var createdReports = ParseCreatedReports(aiResponse.Content);
                var reportingMetrics = ParseReportingMetrics(aiResponse.Content);

                return new SecurityReportingResult
                {
                    Success = true,
                    Message = "Security reports generated successfully",
                    CreatedReports = createdReports,
                    ReportingMetrics = reportingMetrics,
                    GeneratedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate security reports for {ProjectId}", request.ProjectId);
                return new SecurityReportingResult
                {
                    Success = false,
                    Message = ex.Message,
                    GeneratedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }

        /// <summary>
        /// Delivers security reports to stakeholders.
        /// </summary>
        public async Task<ReportDeliveryResult> DeliverSecurityReportsAsync(
            ReportDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Delivering security reports for {ProjectId}", request.ProjectId);

                var aiRequest = new ReportDeliveryAIRequest
                {
                    ProjectId = request.ProjectId,
                    ReportIds = request.ReportIds,
                    DeliveryChannels = request.DeliveryChannels,
                    DeliverySchedule = request.DeliverySchedule
                };

                var aiResponse = await _aiService.DeliverSecurityReportsAsync(aiRequest, cancellationToken);

                var deliveryStatus = ParseDeliveryStatus(aiResponse.Content);
                var deliveryMetrics = ParseDeliveryMetrics(aiResponse.Content);

                return new ReportDeliveryResult
                {
                    Success = true,
                    Message = "Security reports delivered successfully",
                    DeliveryStatus = deliveryStatus,
                    DeliveryMetrics = deliveryMetrics,
                    DeliveredAt = DateTimeOffset.UtcNow.DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver security reports for {ProjectId}", request.ProjectId);
                return new ReportDeliveryResult
                {
                    Success = false,
                    Message = ex.Message,
                    DeliveredAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }
    }
}
