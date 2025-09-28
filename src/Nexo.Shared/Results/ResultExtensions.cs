using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Shared.Results
{
    /// <summary>
    /// Extension methods for working with results
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Safely executes an action with error handling
        /// </summary>
        public static OperationResult<T> SafeExecute<T>(this Func<T> action, string? context = null)
        {
            try
            {
                var result = action();
                return ResultFactory.Success(result);
            }
            catch (Exception ex)
            {
                return ResultFactory.Failure<T>(ex.Message, ex);
            }
        }

        /// <summary>
        /// Safely executes an async action with error handling
        /// </summary>
        public static async Task<OperationResult<T>> SafeExecuteAsync<T>(this Func<Task<T>> action, string? context = null)
        {
            try
            {
                var result = await action();
                return ResultFactory.Success(result);
            }
            catch (Exception ex)
            {
                return ResultFactory.Failure<T>(ex.Message, ex);
            }
        }

        /// <summary>
        /// Chains results together
        /// </summary>
        public static OperationResult<TOutput> Chain<TInput, TOutput>(
            this OperationResult<TInput> input,
            Func<TInput, TOutput> transform)
        {
            if (!input.IsSuccess)
            {
                return ResultFactory.Failure<TOutput>(input.ErrorMessage ?? "Previous operation failed");
            }

            try
            {
                var result = transform(input.Data!);
                return ResultFactory.Success(result);
            }
            catch (Exception ex)
            {
                return ResultFactory.Failure<TOutput>(ex.Message, ex);
            }
        }

        /// <summary>
        /// Chains async results together
        /// </summary>
        public static async Task<OperationResult<TOutput>> ChainAsync<TInput, TOutput>(
            this OperationResult<TInput> input,
            Func<TInput, Task<TOutput>> transform)
        {
            if (!input.IsSuccess)
            {
                return ResultFactory.Failure<TOutput>(input.ErrorMessage ?? "Previous operation failed");
            }

            try
            {
                var result = await transform(input.Data!);
                return ResultFactory.Success(result);
            }
            catch (Exception ex)
            {
                return ResultFactory.Failure<TOutput>(ex.Message, ex);
            }
        }

        /// <summary>
        /// Adds metadata to a result
        /// </summary>
        public static OperationResult<T> WithMetadata<T>(this OperationResult<T> result, string key, object value)
        {
            result.Metadata[key] = value;
            return result;
        }

        /// <summary>
        /// Adds multiple metadata items to a result
        /// </summary>
        public static OperationResult<T> WithMetadata<T>(this OperationResult<T> result, Dictionary<string, object> metadata)
        {
            foreach (var (key, value) in metadata)
            {
                result.Metadata[key] = value;
            }
            return result;
        }

        /// <summary>
        /// Adds a warning to a result
        /// </summary>
        public static OperationResult<T> WithWarning<T>(this OperationResult<T> result, string warning)
        {
            result.Warnings.Add(warning);
            return result;
        }

        /// <summary>
        /// Adds multiple warnings to a result
        /// </summary>
        public static OperationResult<T> WithWarnings<T>(this OperationResult<T> result, IEnumerable<string> warnings)
        {
            result.Warnings.AddRange(warnings);
            return result;
        }
    }
}
