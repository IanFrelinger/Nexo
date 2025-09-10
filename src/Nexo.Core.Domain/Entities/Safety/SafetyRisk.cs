using Nexo.Core.Domain.Enums.Safety;

namespace Nexo.Core.Domain.Entities.Safety
{
    /// <summary>
    /// Represents a safety risk identified during operation validation
    /// </summary>
    public class SafetyRisk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RiskLevel Level { get; set; }
        public RiskCategory Category { get; set; }
        public string Message { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public int AffectedFiles { get; set; }
        public string? AdditionalContext { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public bool IsMitigated { get; set; } = false;
        public string? MitigationAction { get; set; }
    }

    /// <summary>
    /// Represents a safety safeguard that can be applied to mitigate risks
    /// </summary>
    public class SafetySafeguard
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SafeguardType Type { get; set; }
        public string Description { get; set; } = "";
        public bool IsRequired { get; set; }
        public int Priority { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of safety validation for a user operation
    /// </summary>
    public class SafetyCheckResult
    {
        public string OperationId { get; set; } = "";
        public List<SafetyRisk> Risks { get; set; } = new();
        public List<SafetySafeguard> Safeguards { get; set; } = new();
        public bool RequiresConfirmation { get; set; }
        public bool RequiresBackup { get; set; }
        public bool IsSafeToProceed { get; set; }
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
        public string? ValidationNotes { get; set; }
    }

    /// <summary>
    /// Represents a user operation that needs safety validation
    /// </summary>
    public class UserOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public OperationType Type { get; set; }
        public string TargetPath { get; set; } = "";
        public int AffectedFiles { get; set; }
        public bool IsDestructive { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string UserId { get; set; } = "";
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public OperationStatus Status { get; set; } = OperationStatus.Pending;
    }

    /// <summary>
    /// Result of executing a safety safeguard
    /// </summary>
    public class SafeguardResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SafeguardType SafeguardType { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of executing all safeguards for an operation
    /// </summary>
    public class SafeguardExecutionResult
    {
        public string OperationId { get; set; } = "";
        public List<SafeguardResult> Results { get; set; } = new();
        public bool AllSafeguardsSuccessful { get; set; }
        public DateTime ExecutionTimestamp { get; set; } = DateTime.UtcNow;
        public string? ExecutionNotes { get; set; }
    }

    /// <summary>
    /// Represents a file change for dry-run preview
    /// </summary>
    public class FileChange
    {
        public string Path { get; set; } = "";
        public FileChangeType ChangeType { get; set; }
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public string? Checksum { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of dry-run execution
    /// </summary>
    public class DryRunResult
    {
        public string OperationId { get; set; } = "";
        public List<FileChange> Changes { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan EstimatedDuration { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of operation rollback
    /// </summary>
    public class RollbackResult
    {
        public string OperationId { get; set; } = "";
        public string? BackupId { get; set; }
        public bool Success { get; set; }
        public int RestoredFiles { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
