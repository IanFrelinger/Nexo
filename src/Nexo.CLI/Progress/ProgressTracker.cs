using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Progress
{
    /// <summary>
    /// Advanced progress tracking with nested operations and real-time updates
    /// </summary>
    public class ProgressTracker : IProgressTracker
    {
        private readonly Stack<ProgressOperation> _operationStack = new();
        private readonly Timer _updateTimer;
        private readonly object _lock = new();
        private readonly ILogger<ProgressTracker> _logger;
        private int _currentLine;
        private bool _disposed = false;
        
        public ProgressTracker(ILogger<ProgressTracker> logger)
        {
            _logger = logger;
            _updateTimer = new Timer(UpdateDisplay, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            Console.CursorVisible = false;
        }
        
        public IProgressOperation StartOperation(string description, int totalSteps = -1)
        {
            lock (_lock)
            {
                var operation = new ProgressOperation(description, totalSteps, _operationStack.Count, this);
                _operationStack.Push(operation);
                return operation;
            }
        }
        
        public void UpdateDisplay()
        {
            lock (_lock)
            {
                if (!_operationStack.Any()) return;
                
                // Save cursor position
                var originalTop = Console.CursorTop;
                var originalLeft = Console.CursorLeft;
                
                // Update each operation display
                var operations = _operationStack.Reverse().ToArray();
                
                for (int i = 0; i < operations.Length; i++)
                {
                    Console.SetCursorPosition(0, _currentLine + i);
                    RenderOperationProgress(operations[i], i);
                }
                
                // Restore cursor position
                Console.SetCursorPosition(originalLeft, originalTop);
            }
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                _operationStack.Clear();
                Console.Clear();
            }
        }
        
        internal void RemoveOperation(ProgressOperation operation)
        {
            lock (_lock)
            {
                var tempStack = new Stack<ProgressOperation>();
                
                // Remove the operation from the stack
                while (_operationStack.Count > 0)
                {
                    var op = _operationStack.Pop();
                    if (op != operation)
                    {
                        tempStack.Push(op);
                    }
                }
                
                // Restore the stack
                while (tempStack.Count > 0)
                {
                    _operationStack.Push(tempStack.Pop());
                }
            }
        }
        
        private void RenderOperationProgress(ProgressOperation operation, int depth)
        {
            var indent = new string(' ', depth * 2);
            var progressBar = RenderProgressBar(operation);
            var timeInfo = RenderTimeInfo(operation);
            
            var line = $"{indent}{operation.Description} {progressBar} {timeInfo}";
            
            // Clear the line and write new content
            Console.Write(line.PadRight(Console.WindowWidth - 1));
            Console.WriteLine();
        }
        
        private string RenderProgressBar(ProgressOperation operation)
        {
            if (operation.TotalSteps <= 0)
            {
                // Indeterminate progress
                var spinnerChars = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
                var spinnerIndex = (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / 100) % spinnerChars.Length;
                return $"{spinnerChars[spinnerIndex]} Working...";
            }
            
            var progressPercent = (double)operation.CurrentStep / operation.TotalSteps;
            var barWidth = 20;
            var filledWidth = (int)(progressPercent * barWidth);
            
            var bar = "[" + 
                      new string('█', filledWidth) + 
                      new string('░', barWidth - filledWidth) + 
                      $"] {progressPercent:P0}";
            
            return bar;
        }
        
        private string RenderTimeInfo(ProgressOperation operation)
        {
            var elapsed = DateTime.UtcNow - operation.StartTime;
            
            if (operation.TotalSteps > 0 && operation.CurrentStep > 0)
            {
                var estimatedTotal = TimeSpan.FromTicks(elapsed.Ticks * operation.TotalSteps / operation.CurrentStep);
                var remaining = estimatedTotal - elapsed;
                
                return $"({elapsed:mm\\:ss}/{estimatedTotal:mm\\:ss})";
            }
            
            return $"({elapsed:mm\\:ss})";
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _updateTimer?.Dispose();
                Console.CursorVisible = true;
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Represents an individual progress operation
    /// </summary>
    public class ProgressOperation : IProgressOperation
    {
        private readonly ProgressTracker _tracker;
        private readonly Stack<ProgressOperation> _nestedOperations = new();
        private bool _disposed = false;
        
        public string Description { get; }
        public int CurrentStep { get; private set; }
        public int TotalSteps { get; }
        public DateTime StartTime { get; }
        public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;
        public bool IsCompleted { get; private set; }
        
        internal ProgressOperation(string description, int totalSteps, int depth, ProgressTracker tracker)
        {
            Description = description;
            TotalSteps = totalSteps;
            StartTime = DateTime.UtcNow;
            _tracker = tracker;
        }
        
        public void Advance()
        {
            Advance(1);
        }
        
        public void Advance(int steps)
        {
            if (IsCompleted) return;
            
            CurrentStep = Math.Min(CurrentStep + steps, TotalSteps);
            
            if (TotalSteps > 0 && CurrentStep >= TotalSteps)
            {
                Complete();
            }
        }
        
        public void SetStep(int step)
        {
            if (IsCompleted) return;
            
            CurrentStep = Math.Max(0, Math.Min(step, TotalSteps));
            
            if (TotalSteps > 0 && CurrentStep >= TotalSteps)
            {
                Complete();
            }
        }
        
        public void Complete()
        {
            if (IsCompleted) return;
            
            IsCompleted = true;
            CurrentStep = TotalSteps > 0 ? TotalSteps : CurrentStep;
        }
        
        public IProgressOperation StartNestedOperation(string description, int totalSteps = -1)
        {
            var nestedOperation = new ProgressOperation(description, totalSteps, _nestedOperations.Count, _tracker);
            _nestedOperations.Push(nestedOperation);
            return nestedOperation;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose nested operations
                while (_nestedOperations.Count > 0)
                {
                    _nestedOperations.Pop().Dispose();
                }
                
                _tracker.RemoveOperation(this);
                _disposed = true;
            }
        }
    }
}
