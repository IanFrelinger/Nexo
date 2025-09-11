using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Interface for audit services
    /// </summary>
    public interface IAuditService
    {
        Task<bool> LogActionAsync(string userId, string action, Dictionary<string, object> metadata);
        Task<List<AuditEntry>> GetAuditLogAsync(string userId, DateTime from, DateTime to);
        Task<bool> ExportAuditLogAsync(string userId, string format);
        Task<bool> ArchiveAuditLogAsync(string userId, DateTime before);
    }

    public class AuditEntry
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }
}
