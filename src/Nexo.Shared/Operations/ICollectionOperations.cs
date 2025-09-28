using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Interface for collection operations (replaces LINQ)
    /// </summary>
    public interface ICollectionOperations
    {
        /// <summary>
        /// Filters a collection based on a predicate
        /// </summary>
        Task<IReadOnlyList<T>> WhereAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
        
        /// <summary>
        /// Projects each element of a collection to a new form
        /// </summary>
        Task<IReadOnlyList<TResult>> SelectAsync<T, TResult>(IReadOnlyList<T> source, Func<T, Task<TResult>> selector);
        
        /// <summary>
        /// Executes an action for each element in the collection
        /// </summary>
        Task ForEachAsync<T>(IReadOnlyList<T> source, Func<T, Task> action);
        
        /// <summary>
        /// Determines whether any element satisfies a condition
        /// </summary>
        Task<bool> AnyAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
        
        /// <summary>
        /// Determines whether all elements satisfy a condition
        /// </summary>
        Task<bool> AllAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
        
        /// <summary>
        /// Returns the first element that satisfies a condition
        /// </summary>
        Task<T> FirstOrDefaultAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
        
        /// <summary>
        /// Returns the number of elements that satisfy a condition
        /// </summary>
        Task<int> CountAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
        
        /// <summary>
        /// Groups elements by a key selector
        /// </summary>
        Task<IReadOnlyDictionary<TKey, IReadOnlyList<T>>> GroupByAsync<T, TKey>(
            IReadOnlyList<T> source, 
            Func<T, Task<TKey>> keySelector)
            where TKey : notnull;
        
        /// <summary>
        /// Orders elements by a key selector
        /// </summary>
        Task<IReadOnlyList<T>> OrderByAsync<T, TKey>(
            IReadOnlyList<T> source, 
            Func<T, Task<TKey>> keySelector)
            where TKey : notnull;
        
        /// <summary>
        /// Takes the first N elements
        /// </summary>
        Task<IReadOnlyList<T>> TakeAsync<T>(IReadOnlyList<T> source, int count);
        
        /// <summary>
        /// Skips the first N elements
        /// </summary>
        Task<IReadOnlyList<T>> SkipAsync<T>(IReadOnlyList<T> source, int count);
    }
}
