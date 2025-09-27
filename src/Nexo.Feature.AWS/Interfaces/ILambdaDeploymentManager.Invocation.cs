using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Lambda function invocation functionality.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
        /// <summary>
        /// Invokes a Lambda function
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="payload">Invocation payload</param>
        /// <param name="invocationType">Invocation type (RequestResponse, Event, DryRun)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Invocation result</returns>
        Task<LambdaInvocationResult> InvokeFunctionAsync(
            string functionName,
            string payload,
            string invocationType = "RequestResponse",
            CancellationToken cancellationToken = default);
    }
}
