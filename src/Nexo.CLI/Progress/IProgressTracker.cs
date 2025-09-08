using System;
using System.Threading.Tasks;

namespace Nexo.CLI.Progress
{
    /// <summary>
    /// Interface for advanced progress tracking with nested operations and real-time updates
    /// </summary>
    public interface IProgressTracker : IDisposable
    {
        /// <summary>
        /// Starts a new progress operation
        /// </summary>
        IProgressOperation StartOperation(string description, int totalSteps = -1);
        
        /// <summary>
        /// Updates the display
        /// </summary>
        void UpdateDisplay();
        
        /// <summary>
        /// Clears all progress operations
        /// </summary>
        void Clear();
    }
    
    /// <summary>
    /// Interface for individual progress operations
    /// </summary>
    public interface IProgressOperation : IDisposable
    {
        string Description { get; }
        int CurrentStep { get; }
        int TotalSteps { get; }
        DateTime StartTime { get; }
        TimeSpan ElapsedTime { get; }
        bool IsCompleted { get; }
        
        /// <summary>
        /// Advances the progress by one step
        /// </summary>
        void Advance();
        
        /// <summary>
        /// Advances the progress by the specified number of steps
        /// </summary>
        void Advance(int steps);
        
        /// <summary>
        /// Sets the current step
        /// </summary>
        void SetStep(int step);
        
        /// <summary>
        /// Completes the operation
        /// </summary>
        void Complete();
        
        /// <summary>
        /// Starts a nested operation
        /// </summary>
        IProgressOperation StartNestedOperation(string description, int totalSteps = -1);
    }
    
    /// <summary>
    /// Interface for multi-step progress display
    /// </summary>
    public interface IMultiStepProgressDisplay
    {
        /// <summary>
        /// Defines the steps for a multi-step operation
        /// </summary>
        void DefineSteps(params string[] stepDescriptions);
        
        /// <summary>
        /// Executes the steps with progress tracking
        /// </summary>
        Task ExecuteStepsAsync(params Func<IProgressReporter, Task>[] stepExecutors);
        
        /// <summary>
        /// Shows the current progress overview
        /// </summary>
        void ShowProgressOverview();
    }
    
    /// <summary>
    /// Interface for progress reporting
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports progress with a message
        /// </summary>
        void ReportProgress(string message);
        
        /// <summary>
        /// Reports progress with a percentage
        /// </summary>
        void ReportProgress(int percentage, string message);
        
        /// <summary>
        /// Reports an error
        /// </summary>
        void ReportError(string error);
        
        /// <summary>
        /// Reports completion
        /// </summary>
        void ReportCompletion(string message);
    }
    
    /// <summary>
    /// Represents the status of a step
    /// </summary>
    public enum StepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    
    /// <summary>
    /// Represents step progress information
    /// </summary>
    public class StepProgress
    {
        public string Description { get; set; } = string.Empty;
        public StepStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Error { get; set; }
        public string CurrentMessage { get; set; } = string.Empty;
    }
}
