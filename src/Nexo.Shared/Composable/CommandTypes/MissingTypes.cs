using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Missing types that are referenced in the codebase but not yet defined
    /// These are stub implementations to avoid compilation errors
    /// </summary>
    
    public class TaskId
    {
        public string Value { get; }
        
        public TaskId(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public override string ToString() => Value;
        
        public static implicit operator string(TaskId taskId) => taskId.Value;
        public static implicit operator TaskId(string value) => new TaskId(value);
    }
    
    public class TaskName
    {
        public string Value { get; }
        
        public TaskName(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public override string ToString() => Value;
        
        public static implicit operator string(TaskName taskName) => taskName.Value;
        public static implicit operator TaskName(string value) => new TaskName(value);
    }
    
    public class TaskDescription
    {
        public string Value { get; }
        
        public TaskDescription(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public override string ToString() => Value;
        
        public static implicit operator string(TaskDescription taskDescription) => taskDescription.Value;
        public static implicit operator TaskDescription(string value) => new TaskDescription(value);
    }
    
    public enum TaskType
    {
        Analysis,
        Generation,
        Processing,
        Validation,
        General,
        CodeGeneration,
        SecurityAnalysis,
        AssemblyAnalysis,
        CodeAnalysis
    }
    
    public enum TaskPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
    
    public class WorkflowStep : IWorkflowStep
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Type { get; }
        public string Status { get; }
        public int Order { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public ICommandIdentifier CommandId { get; }
        public IReadOnlyList<ICommandIdentifier> Dependencies { get; }
        public bool IsParallel { get; }
        public bool IsOptional { get; }
        
        public WorkflowStep(string id, string name, string description, string type = "General", string status = "Pending", int order = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Order = order;
            Parameters = new Dictionary<string, object>();
            CommandId = new CommandIdentifier(id, "Nexo.Shared.Composable", "1.0.0");
            Dependencies = new List<ICommandIdentifier>();
            IsParallel = false;
            IsOptional = false;
        }
    }
    
    public class WorkflowDefinition : IWorkflowDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<IWorkflowStep> Steps { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        
        public WorkflowDefinition(string id, string name, string description, IReadOnlyList<IWorkflowStep> steps)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            Parameters = new Dictionary<string, object>();
        }
    }
    
    
    public class AgentMessage
    {
        public string Id { get; }
        public string Content { get; }
        public string SenderId { get; }
        public string ReceiverId { get; }
        public DateTime Timestamp { get; }
        public MessagePriority Priority { get; }
        public string Type { get; }
        
        public AgentMessage(string id, string content, string senderId, string receiverId, MessagePriority priority = MessagePriority.Normal, string type = "General")
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            SenderId = senderId ?? throw new ArgumentNullException(nameof(senderId));
            ReceiverId = receiverId ?? throw new ArgumentNullException(nameof(receiverId));
            Timestamp = DateTime.UtcNow;
            Priority = priority;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
        
        public static AgentMessage CreateCapabilityRequest(string senderId, string receiverId, string capability, string messageType = "CapabilityRequest", MessagePriority priority = MessagePriority.High)
        {
            return new AgentMessage(
                Guid.NewGuid().ToString(),
                $"Requesting capability: {capability}",
                senderId,
                receiverId,
                priority,
                messageType
            );
        }
    }
    
    public enum MessagePriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }
    
    public class AgentName
    {
        public string Value { get; }
        
        public AgentName(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public override string ToString() => Value;
        
        public static implicit operator string(AgentName agentName) => agentName.Value;
        public static implicit operator AgentName(string value) => new AgentName(value);
    }
    
    public enum PlatformType
    {
        Windows,
        Linux,
        macOS,
        Docker,
        Cloud,
        CrossPlatform
    }
    
    public enum AgentStatus
    {
        Inactive,
        Active,
        Busy,
        Error,
        Shutdown
    }
}
