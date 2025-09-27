using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Lambda function logging functionality.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
        /// <summary>
        /// Gets function logs from CloudWatch
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="startTime">Start time for logs</param>
        /// <param name="endTime">End time for logs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Function logs</returns>
        Task<LambdaLogsResult> GetFunctionLogsAsync(
            string functionName,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);
    }
}
