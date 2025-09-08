using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Progress
{
    /// <summary>
    /// Multi-step progress display for complex operations
    /// </summary>
    public class MultiStepProgressDisplay : IMultiStepProgressDisplay
    {
        private readonly List<StepProgress> _steps = new();
        private readonly ILogger<MultiStepProgressDisplay> _logger;
        private int _currentStepIndex = 0;
        
        public MultiStepProgressDisplay(ILogger<MultiStepProgressDisplay> logger)
        {
            _logger = logger;
        }
        
        public void DefineSteps(params string[] stepDescriptions)
        {
            _steps.Clear();
            _steps.AddRange(stepDescriptions.Select(desc => new StepProgress
            {
                Description = desc,
                Status = StepStatus.Pending
            }));
        }
        
        public async Task ExecuteStepsAsync(params Func<IProgressReporter, Task>[] stepExecutors)
        {
            if (stepExecutors.Length != _steps.Count)
            {
                throw new ArgumentException("Number of executors must match number of defined steps");
            }
            
            Console.WriteLine("🚀 Starting multi-step operation...\n");
            
            for (int i = 0; i < _steps.Count; i++)
            {
                _currentStepIndex = i;
                _steps[i].Status = StepStatus.InProgress;
                _steps[i].StartTime = DateTime.UtcNow;
                
                RenderStepsOverview();
                
                try
                {
                    var progressReporter = new StepProgressReporter(_steps[i]);
                    await stepExecutors[i](progressReporter);
                    
                    _steps[i].Status = StepStatus.Completed;
                    _steps[i].EndTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _steps[i].Status = StepStatus.Failed;
                    _steps[i].Error = ex.Message;
                    _steps[i].EndTime = DateTime.UtcNow;
                    
                    Console.WriteLine($"\n❌ Step {i + 1} failed: {ex.Message}");
                    _logger.LogError(ex, "Step {StepIndex} failed: {StepDescription}", i + 1, _steps[i].Description);
                    return;
                }
                
                RenderStepsOverview();
            }
            
            Console.WriteLine("\n✅ All steps completed successfully!");
        }
        
        public void ShowProgressOverview()
        {
            RenderStepsOverview();
        }
        
        private void RenderStepsOverview()
        {
            // Clear previous overview
            var linesToClear = _steps.Count + 2;
            for (int i = 0; i < linesToClear; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            
            Console.WriteLine("📋 Progress Overview:");
            
            for (int i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                var statusIcon = step.Status switch
                {
                    StepStatus.Pending => "⏳",
                    StepStatus.InProgress => "🔄",
                    StepStatus.Completed => "✅",
                    StepStatus.Failed => "❌",
                    _ => "❓"
                };
                
                var timing = step.EndTime.HasValue 
                    ? $"({(step.EndTime.Value - step.StartTime).TotalSeconds:F1}s)"
                    : step.StartTime != default 
                        ? $"({(DateTime.UtcNow - step.StartTime).TotalSeconds:F1}s)"
                        : "";
                
                var message = !string.IsNullOrEmpty(step.CurrentMessage) 
                    ? $" - {step.CurrentMessage}"
                    : "";
                
                Console.WriteLine($"  {statusIcon} Step {i + 1}: {step.Description} {timing}{message}");
                
                if (step.Status == StepStatus.Failed && !string.IsNullOrEmpty(step.Error))
                {
                    Console.WriteLine($"     ❌ Error: {step.Error}");
                }
            }
            
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Progress reporter for individual steps
    /// </summary>
    public class StepProgressReporter : IProgressReporter
    {
        private readonly StepProgress _step;
        
        public StepProgressReporter(StepProgress step)
        {
            _step = step;
        }
        
        public void ReportProgress(string message)
        {
            _step.CurrentMessage = message;
        }
        
        public void ReportProgress(int percentage, string message)
        {
            _step.CurrentMessage = $"{percentage}% - {message}";
        }
        
        public void ReportError(string error)
        {
            _step.Error = error;
            _step.Status = StepStatus.Failed;
        }
        
        public void ReportCompletion(string message)
        {
            _step.CurrentMessage = message;
            _step.Status = StepStatus.Completed;
        }
    }
}
