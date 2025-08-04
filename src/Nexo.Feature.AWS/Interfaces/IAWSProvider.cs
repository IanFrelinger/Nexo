using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Main AWS provider interface for comprehensive AWS services integration
    /// </summary>
    public interface IAWSProvider
    {
        /// <summary>
        /// Gets the AWS region
        /// </summary>
        string Region { get; }

        /// <summary>
        /// Gets the AWS account ID
        /// </summary>
        string AccountId { get; }

        /// <summary>
        /// Tests AWS connectivity and credentials
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connectivity test result</returns>
        Task<AWSConnectivityResult> TestConnectivityAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets AWS account information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AWS account information</returns>
        Task<AWSAccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets AWS service health status
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health status</returns>
        Task<AWSServiceHealthStatus> GetServiceHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets AWS cost and usage information
        /// </summary>
        /// <param name="startDate">Start date for cost analysis</param>
        /// <param name="endDate">End date for cost analysis</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cost and usage information</returns>
        Task<AWSCostInfo> GetCostInfoAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// AWS connectivity test result
    /// </summary>
    public class AWSConnectivityResult
    {
        /// <summary>
        /// Whether the connection was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Connection message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Connection latency in milliseconds
        /// </summary>
        public long LatencyMs { get; set; }

        /// <summary>
        /// Test timestamp
        /// </summary>
        public DateTime TestedAt { get; set; }

        /// <summary>
        /// Error details if connection failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// AWS account information
    /// </summary>
    public class AWSAccountInfo
    {
        /// <summary>
        /// AWS account ID
        /// </summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// AWS account alias
        /// </summary>
        public string? AccountAlias { get; set; }

        /// <summary>
        /// AWS account type
        /// </summary>
        public string AccountType { get; set; } = string.Empty;

        /// <summary>
        /// AWS account status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Account creation date
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Account email address
        /// </summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// AWS service health status
    /// </summary>
    public class AWSServiceHealthStatus
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public string OverallStatus { get; set; } = string.Empty;

        /// <summary>
        /// Service-specific health status
        /// </summary>
        public Dictionary<string, string> ServiceStatus { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Health check duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// AWS cost and usage information
    /// </summary>
    public class AWSCostInfo
    {
        /// <summary>
        /// Total cost for the period
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Cost currency
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Cost breakdown by service
        /// </summary>
        public Dictionary<string, decimal> ServiceCosts { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Cost breakdown by region
        /// </summary>
        public Dictionary<string, decimal> RegionCosts { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Period start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Period end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Cost analysis timestamp
        /// </summary>
        public DateTime AnalyzedAt { get; set; }
    }
} 