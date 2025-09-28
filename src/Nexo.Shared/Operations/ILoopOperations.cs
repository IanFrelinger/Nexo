using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Interface for loop operations (replaces for/while loops)
    /// </summary>
    public interface ILoopOperations
    {
        /// <summary>
        /// Executes a for loop with async support
        /// </summary>
        Task ForAsync(int start, int end, Func<int, Task> action);
        
        /// <summary>
        /// Executes a for loop with async support and step
        /// </summary>
        Task ForAsync(int start, int end, int step, Func<int, Task> action);
        
        /// <summary>
        /// Executes a while loop with async support
        /// </summary>
        Task WhileAsync(Func<Task<bool>> condition, Func<Task> action);
        
        /// <summary>
        /// Executes a do-while loop with async support
        /// </summary>
        Task DoWhileAsync(Func<Task> action, Func<Task<bool>> condition);
        
        /// <summary>
        /// Executes a foreach loop with async support
        /// </summary>
        Task ForEachAsync<T>(IReadOnlyList<T> items, Func<T, int, Task> action);
        
        /// <summary>
        /// Executes a foreach loop with async support and index
        /// </summary>
        Task ForEachAsync<T>(IEnumerable<T> items, Func<T, int, Task> action);
        
        /// <summary>
        /// Executes a loop with break/continue support
        /// </summary>
        Task LoopAsync<T>(IReadOnlyList<T> items, Func<T, int, ILoopControl, Task> action);
    }
    
    /// <summary>
    /// Interface for loop control (break/continue)
    /// </summary>
    public interface ILoopControl
    {
        void Break();
        void Continue();
        bool ShouldBreak { get; }
        bool ShouldContinue { get; }
    }
}
