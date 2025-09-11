using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Interface for backup services
    /// </summary>
    public interface IBackupService
    {
        Task<string> CreateBackupAsync(string userId, BackupType type);
        Task<bool> RestoreBackupAsync(string userId, string backupId);
        Task<List<BackupInfo>> GetBackupsAsync(string userId);
        Task<bool> DeleteBackupAsync(string userId, string backupId);
        Task<bool> ScheduleBackupAsync(string userId, BackupSchedule schedule);
        Task<BackupInfo> GetBackupByOperationIdAsync(string operationId);
        Task<bool> RestoreFromBackupAsync(string operationId, string backupId);
    }

    public enum BackupType
    {
        Full,
        Incremental,
        Differential
    }

    public class BackupInfo
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public class BackupSchedule
    {
        public string Id { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime NextRun { get; set; }
        public bool IsEnabled { get; set; }
    }
}
