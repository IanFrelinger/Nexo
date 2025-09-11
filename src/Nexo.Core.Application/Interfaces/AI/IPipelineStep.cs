using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Pipeline;

namespace Nexo.Core.Application.Interfaces.AI
{
    /// <summary>
    /// Interface for pipeline steps that process data
    /// </summary>
    /// <typeparam name="T">The type of data to process</typeparam>
    public interface IPipelineStep<T>
    {
        /// <summary>
        /// Executes the pipeline step
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="context">The pipeline context</param>
        /// <returns>The processed data</returns>
        Task<T> ExecuteAsync(T input, PipelineContext context);
    }
}
