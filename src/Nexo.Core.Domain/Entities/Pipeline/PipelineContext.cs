using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.Pipeline
{
    /// <summary>
    /// Pipeline execution context
    /// </summary>
    public class PipelineContext
    {
        /// <summary>
        /// Pipeline ID
        /// </summary>
        public string PipelineId { get; set; } = string.Empty;
        
        /// <summary>
        /// Pipeline name
        /// </summary>
        public string PipelineName { get; set; } = string.Empty;
        
        /// <summary>
        /// Current step ID
        /// </summary>
        public string CurrentStepId { get; set; } = string.Empty;
        
        /// <summary>
        /// Current step name
        /// </summary>
        public string CurrentStepName { get; set; } = string.Empty;
        
        /// <summary>
        /// Pipeline status
        /// </summary>
        public PipelineStatus Status { get; set; } = PipelineStatus.NotStarted;
        
        /// <summary>
        /// Pipeline start time
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Pipeline end time
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Pipeline duration
        /// </summary>
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        
        /// <summary>
        /// Environment profile
        /// </summary>
        public EnvironmentProfile EnvironmentProfile { get; set; } = new();
        
        /// <summary>
        /// Pipeline data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Pipeline variables
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();
        
        /// <summary>
        /// Pipeline parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// Pipeline errors
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// Pipeline warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Pipeline steps
        /// </summary>
        public List<PipelineStep> Steps { get; set; } = new();
        
        /// <summary>
        /// Adds data
        /// </summary>
        public void AddData(string key, object value)
        {
            Data[key] = value;
        }
        
        /// <summary>
        /// Adds variable
        /// </summary>
        public void AddVariable(string key, object value)
        {
            Variables[key] = value;
        }
        
        /// <summary>
        /// Adds parameter
        /// </summary>
        public void AddParameter(string key, object value)
        {
            Parameters[key] = value;
        }
        
        /// <summary>
        /// Adds error
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Adds step
        /// </summary>
        public void AddStep(PipelineStep step)
        {
            Steps.Add(step);
        }
    }
    
    /// <summary>
    /// Pipeline step
    /// </summary>
    public class PipelineStep
    {
        /// <summary>
        /// Step ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Step name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Step status
        /// </summary>
        public PipelineStepStatus Status { get; set; } = PipelineStepStatus.Pending;
        
        /// <summary>
        /// Step start time
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// Step end time
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Step duration
        /// </summary>
        public TimeSpan? Duration => EndTime?.Subtract(StartTime ?? DateTime.UtcNow);
        
        /// <summary>
        /// Step data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Step errors
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// Step warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
    
    /// <summary>
    /// Pipeline status
    /// </summary>
    public enum PipelineStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }
    
    /// <summary>
    /// Pipeline step status
    /// </summary>
    public enum PipelineStepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }
}
