using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Shared.Extensions
{
    /// <summary>
    /// Extension methods for collection operations
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Converts a collection to a comma-separated string
        /// </summary>
        public static string ToCommaSeparatedString<T>(this IEnumerable<T> collection, string separator = ", ")
        {
            return string.Join(separator, collection);
        }

        /// <summary>
        /// Executes an action for each item in a collection
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes an async action for each item in a collection
        /// </summary>
        public static async Task ForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> action)
        {
            foreach (var item in collection)
            {
                await action(item);
            }
        }

        /// <summary>
        /// Chunks a collection into smaller collections
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> collection, int chunkSize)
        {
            var list = collection.ToList();
            for (int i = 0; i < list.Count; i += chunkSize)
            {
                yield return list.Skip(i).Take(chunkSize);
            }
        }

        /// <summary>
        /// Checks if a collection is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// Gets the first item or a default value
        /// </summary>
        public static T? FirstOrDefault<T>(this IEnumerable<T> collection, T? defaultValue = default)
        {
            var enumerator = collection.GetEnumerator();
            if (enumerator.MoveNext())
                return enumerator.Current;
            return defaultValue;
        }

        /// <summary>
        /// Converts a collection to a dictionary
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(
            this IEnumerable<T> collection, 
            Func<T, TKey> keySelector, 
            Func<T, TValue> valueSelector)
            where TKey : notnull
        {
            return collection.ToDictionary(keySelector, valueSelector);
        }

        /// <summary>
        /// Converts a collection to a lookup
        /// </summary>
        public static ILookup<TKey, TValue> ToLookup<T, TKey, TValue>(
            this IEnumerable<T> collection,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector)
        {
            return collection.ToLookup(keySelector, valueSelector);
        }
    }
}
