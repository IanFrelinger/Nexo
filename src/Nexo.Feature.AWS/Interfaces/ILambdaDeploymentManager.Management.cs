using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Lambda function management functionality.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
        /// <summary>
        /// Gets Lambda function information
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Function information</returns>
        Task<LambdaFunctionInfo> GetFunctionInfoAsync(string functionName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all Lambda functions
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of functions</returns>
        Task<LambdaListResult> ListFunctionsAsync(CancellationToken cancellationToken = default);
    }
}
