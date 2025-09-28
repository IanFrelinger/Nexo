using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Default implementation of loop operations
    /// </summary>
    public class LoopOperations : ILoopOperations
    {
        public async Task ForAsync(int start, int end, Func<int, Task> action)
        {
            for (int i = start; i < end; i++)
            {
                await action(i);
            }
        }
        
        public async Task ForAsync(int start, int end, int step, Func<int, Task> action)
        {
            for (int i = start; i < end; i += step)
            {
                await action(i);
            }
        }
        
        public async Task WhileAsync(Func<Task<bool>> condition, Func<Task> action)
        {
            while (await condition())
            {
                await action();
            }
        }
        
        public async Task DoWhileAsync(Func<Task> action, Func<Task<bool>> condition)
        {
            do
            {
                await action();
            } while (await condition());
        }
        
        public async Task ForEachAsync<T>(IReadOnlyList<T> items, Func<T, int, Task> action)
        {
            for (int i = 0; i < items.Count; i++)
            {
                await action(items[i], i);
            }
        }
        
        public async Task ForEachAsync<T>(IEnumerable<T> items, Func<T, int, Task> action)
        {
            int index = 0;
            foreach (var item in items)
            {
                await action(item, index);
                index++;
            }
        }
        
        public async Task LoopAsync<T>(IReadOnlyList<T> items, Func<T, int, ILoopControl, Task> action)
        {
            var control = new LoopControl();
            for (int i = 0; i < items.Count; i++)
            {
                if (control.ShouldBreak) break;
                if (control.ShouldContinue) continue;
                
                await action(items[i], i, control);
            }
        }
    }
    
    /// <summary>
    /// Default implementation of loop control
    /// </summary>
    public class LoopControl : ILoopControl
    {
        public bool ShouldBreak { get; private set; }
        public bool ShouldContinue { get; private set; }
        
        public void Break()
        {
            ShouldBreak = true;
        }
        
        public void Continue()
        {
            ShouldContinue = true;
        }
    }
}
