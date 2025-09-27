using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Domain logic generation functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        private async Task GenerateDomainLogicComponents(ScriptGenerationConfigV2 config)
        {
            Console.WriteLine("🏗️ Generating domain logic components using templates...");
            
            var outputDir = "GeneratedNexoScripts/DomainLogic";
            Directory.CreateDirectory(outputDir);
            
            foreach (var component in config.DomainLogicComponents)
            {
                var scriptContent = GenerateDomainLogicFromTemplate(component, config.Defaults);
                var filePath = Path.Combine(outputDir, $"{component.Name}.cs");
                await File.WriteAllTextAsync(filePath, scriptContent);
                Console.WriteLine($"✅ Generated {component.Name}.cs");
            }
        }
        
        private string GenerateDomainLogicFromTemplate(DomainLogicComponent component, ConfigurationDefaults defaults)
        {
            var crossDomainUsages = component.CrossDomainUsages?.Length > 0 
                ? component.CrossDomainUsages 
                : defaults.CrossDomainUsages;
            
            var dependencies = component.Dependencies.Select(d => $"I{d.Replace("Provider", "")}Provider").ToArray();
            var constructorParams = component.Dependencies.Select(d => $"I{d.Replace("Provider", "")}Provider {d.ToLower()}").ToArray();
            var constructorAssignments = component.Dependencies.Select(d => $"this.{d.ToLower()} = {d.ToLower()};").ToArray();
                
            return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{{
    /// <summary>
    /// Abstract domain logic for {component.Name}
    /// Domain: {component.Domain}
    /// Cross-domain usage: {string.Join(", ", crossDomainUsages)}
    /// </summary>
    public abstract class {component.Name} : BaseDomainLogic
    {{
        {string.Join("\n        ", dependencies.Select(d => $"protected readonly {d};"))}
        
        protected {component.Name}({string.Join(", ", constructorParams)})
        {{
            {string.Join("\n            ", constructorAssignments)}
        }}
        
        /// <summary>
        /// {component.Description}
        /// </summary>
        public override abstract Task<bool> ValidateAsync();
        
        /// <summary>
        /// Execute the core domain logic
        /// </summary>
        public override abstract Task<object> ExecuteAsync(object input);
        
        /// <summary>
        /// Get the current state of the component
        /// </summary>
        public override abstract Task<Dictionary<string, object>> GetStateAsync();
    }}
    
    /// <summary>
    /// Interface for {component.Name} providers
    /// </summary>
    public abstract Task<bool> ValidateAsync();
    public abstract Task<object> ExecuteAsync(object input);
    public abstract Task<Dictionary<string, object>> GetStateAsync();
    public virtual async Task InitializeAsync()
    public virtual async Task CleanupAsync()
    public virtual Dictionary<string, object> GetPlatformInfo()
    public override async Task InitializeAsync()
    public override async Task CleanupAsync()
    public virtual void RegisterComponent<T>(string name, T component) where T : class
    public virtual T GetComponent<T>(string name) where T : class
    public virtual void RegisterPlatformImplementation(string domain, string platform, object implementation)
    public virtual T GetPlatformImplementation<T>(string domain, string platform) where T : class
    // Merged from IBaseDomainLogicProvider
    public abstract Task<bool> ValidateAsync();
    public abstract Task<object> ExecuteAsync(object input);
    public abstract Task<Dictionary<string, object>> GetStateAsync();
    public virtual async Task InitializeAsync()
    public virtual async Task CleanupAsync()
    public virtual Dictionary<string, object> GetPlatformInfo()
    public override async Task InitializeAsync()
    public override async Task CleanupAsync()
    public virtual void RegisterComponent<T>(string name, T component) where T : class
    public virtual T GetComponent<T>(string name) where T : class
    public virtual void RegisterPlatformImplementation(string domain, string platform, object implementation)
    public virtual T GetPlatformImplementation<T>(string domain, string platform) where T : class
    // Merged from IBaseDomainLogicValidator
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from FileSystemSafetyRule
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from NetworkSafetyRule
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from SystemCommandSafetyRule
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from DataDestructionSafetyRule
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from SecurityVulnerabilitySafetyRule
    public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
    // Merged from ResourceExhaustionSafetyRule
    public async Task<FeatureResult> ProcessAsync(FeatureRequest request)
    public async Task<bool> ValidateAsync(FeatureRequest request)
    public async Task<FeatureEntity> GetByIdAsync(Guid id)
    public async Task SaveAsync(FeatureEntity entity)
    public Guid Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
    // Merged from IFeatureService
    public async Task<FeatureResult> ProcessAsync(FeatureRequest request)
    public async Task<bool> ValidateAsync(FeatureRequest request)
    public async Task<FeatureEntity> GetByIdAsync(Guid id)
    public async Task SaveAsync(FeatureEntity entity)
    public Guid Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
    // Merged from FeatureService
    public async Task<FeatureResult> ProcessAsync(FeatureRequest request)
    public async Task<bool> ValidateAsync(FeatureRequest request)
    public async Task<FeatureEntity> GetByIdAsync(Guid id)
    public async Task SaveAsync(FeatureEntity entity)
    public Guid Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
    // Merged from FeatureRepository
    public AIDocumentationStep(IAIRuntimeSelector runtimeSelector, ILogger<AIDocumentationStep> logger)
    public async Task<DocumentationRequest> ExecuteAsync(DocumentationRequest input, PipelineContext context)
    public async Task<bool> CanExecuteAsync(DocumentationRequest input, PipelineContext context)
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string GeneratedDocumentation { get; set; } = string.Empty;
    public DocumentationType DocumentationType { get; set; }
    public int QualityScore { get; set; }
    public int Coverage { get; set; }
    public DateTime GenerationTime { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public AIEngineType EngineType { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    // Merged from DocumentationResult
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from OperationSnapshotRequest
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from OperationSnapshot
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from DependencyInfo
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from Checkpoint
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from RollbackRequest
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from RollbackSession
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from RollbackStep
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from RollbackValidationResult
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from ValidationIssue
    public AIOperationRollback(ILogger<AIOperationRollback> logger)
    public async Task<string> CreateSnapshotAsync(OperationSnapshotRequest request)
    public Task<RollbackSession> StartRollbackAsync(RollbackRequest request)
    public Task<RollbackSession?> GetRollbackStatusAsync(string sessionId)
    public Task<bool> CancelRollbackAsync(string sessionId)
    public Task<List<OperationSnapshot>> GetSnapshotsAsync(string operationId)
    public Task<List<RollbackSession>> GetRollbackHistoryAsync(string? operationId = null)
    public Task<RollbackValidationResult> ValidateRollbackAsync(string snapshotId)
    public Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<RollbackStep> Steps { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SnapshotId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<Checkpoint> Checkpoints { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string CheckpointId { get; set; } = string.Empty;
    public string DependencyId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string SnapshotId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RollbackStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RollbackStep> Steps { get; set; } = new();
    public string StepId { get; set; } = string.Empty;
    public string CheckpointId { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationIssueType Type { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int CleanedSnapshots { get; set; }
    public int CleanedSessions { get; set; }
    public DateTime CleanupTime { get; set; }
    // Merged from CleanupResult
    public IterationOptimizationAgent(
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
    public bool RequiresIteration { get; init; }
    public int EstimatedDataSize { get; init; }
    public PlatformTarget TargetPlatform { get; init; }
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    public bool IsCpuBound { get; init; }
    public bool IsIoBound { get; init; }
    public bool RequiresAsync { get; init; }
    public string CollectionVariableName { get; init; } = string.Empty;
    public string ItemVariableName { get; init; } = string.Empty;
    public string ActionCode { get; init; } = string.Empty;
    public bool RequiresComplexLogic { get; init; }
    public bool IncludeNullChecks { get; init; }
    public bool IncludeBoundsChecking { get; init; }
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
    public IterationContext? IterationContext { get; init; }
    // Merged from IterationAnalysis
    public interface I{component.Name}Provider : IBaseDomainLogicProvider
    {{
        new Task<{component.Name}> CreateAsync();
    }}
    
    /// <summary>
    /// Interface for {component.Name} validation
    /// </summary>
    public interface I{component.Name}Validator : IBaseDomainLogicValidator
    {{
        Task<bool> ValidateAsync({component.Name} component);
    }}
}}";
        }
    }
}
