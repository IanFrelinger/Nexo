using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Default implementation of collection operations
    /// </summary>
    public class CollectionOperations : ICollectionOperations
    {
        public async Task<IReadOnlyList<T>> WhereAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            var results = new List<T>();
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    results.Add(item);
                }
            }
            return results;
        }
        
        public async Task<IReadOnlyList<TResult>> SelectAsync<T, TResult>(IReadOnlyList<T> source, Func<T, Task<TResult>> selector)
        {
            var results = new List<TResult>();
            foreach (var item in source)
            {
                var result = await selector(item);
                results.Add(result);
            }
            return results;
        }
        
        public async Task ForEachAsync<T>(IReadOnlyList<T> source, Func<T, Task> action)
        {
            foreach (var item in source)
            {
                await action(item);
            }
        }
        
        public async Task<bool> AnyAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    return true;
                }
            }
            return false;
        }
        
        public async Task<bool> AllAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            foreach (var item in source)
            {
                if (!await predicate(item))
                {
                    return false;
                }
            }
            return true;
        }
        
        public async Task<T> FirstOrDefaultAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    return item;
                }
            }
            return default(T);
        }
        
        public async Task<int> CountAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            int count = 0;
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    count++;
                }
            }
            return count;
        }
        
        public async Task<IReadOnlyDictionary<TKey, IReadOnlyList<T>>> GroupByAsync<T, TKey>(
            IReadOnlyList<T> source, 
            Func<T, Task<TKey>> keySelector)
            where TKey : notnull
        {
            var groups = new Dictionary<TKey, List<T>>();
            foreach (var item in source)
            {
                var key = await keySelector(item);
                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<T>();
                }
                groups[key].Add(item);
            }
            return groups.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<T>)kvp.Value);
        }
        
        public async Task<IReadOnlyList<T>> OrderByAsync<T, TKey>(
            IReadOnlyList<T> source, 
            Func<T, Task<TKey>> keySelector)
            where TKey : notnull
        {
            var items = new List<(T item, TKey key)>();
            foreach (var item in source)
            {
                var key = await keySelector(item);
                items.Add((item, key));
            }
            return items.OrderBy(x => x.key).Select(x => x.item).ToList();
        }
        
        public async Task<IReadOnlyList<T>> TakeAsync<T>(IReadOnlyList<T> source, int count)
        {
            var results = new List<T>();
            for (int i = 0; i < Math.Min(count, source.Count); i++)
            {
                results.Add(source[i]);
            }
            return results;
        }
        
        public async Task<IReadOnlyList<T>> SkipAsync<T>(IReadOnlyList<T> source, int count)
        {
            var results = new List<T>();
            for (int i = count; i < source.Count; i++)
            {
                results.Add(source[i]);
            }
            return results;
        }
    }
}
