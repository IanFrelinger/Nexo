namespace Nexo.Core.Domain.Enums.Safety
{
    /// <summary>
    /// Risk levels for safety validation
    /// </summary>
    public enum RiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Categories of safety risks
    /// </summary>
    public enum RiskCategory
    {
        DataLoss,
        SystemIntegrity,
        Scale,
        Concurrency,
        Permissions,
        Performance,
        Security,
        Compliance
    }

    /// <summary>
    /// Types of safety safeguards
    /// </summary>
    public enum SafeguardType
    {
        AutomaticBackup,
        DryRunMode,
        ConfirmationPrompt,
        PermissionCheck,
        ResourceValidation,
        DependencyCheck,
        RollbackCapability,
        AuditLogging
    }

    /// <summary>
    /// Types of user operations
    /// </summary>
    public enum OperationType
    {
        CreateFile,
        ModifyFile,
        DeleteFile,
        MoveFile,
        CopyFile,
        BulkOperation,
        SystemOperation,
        ConfigurationChange,
        DatabaseOperation,
        NetworkOperation
    }

    /// <summary>
    /// Status of user operations
    /// </summary>
    public enum OperationStatus
    {
        Pending,
        Validating,
        SafeguardsExecuting,
        Executing,
        Completed,
        Failed,
        RolledBack,
        Cancelled
    }

    /// <summary>
    /// Types of file changes
    /// </summary>
    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Moved,
        Copied,
        Renamed,
        PermissionsChanged,
        AttributesChanged
    }
}
