using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Core Lambda deployment functionality.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
        /// <summary>
        /// Deploys a new Lambda function
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="runtime">Runtime (e.g., dotnet8, nodejs18.x)</param>
        /// <param name="handler">Handler (e.g., MyFunction::MyFunction.Function::FunctionHandler)</param>
        /// <param name="zipFilePath">Path to deployment package</param>
        /// <param name="roleArn">IAM role ARN</param>
        /// <param name="description">Function description</param>
        /// <param name="timeout">Function timeout in seconds</param>
        /// <param name="memorySize">Memory size in MB</param>
        /// <param name="environmentVariables">Environment variables</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deployment result</returns>
        Task<LambdaDeploymentResult> DeployFunctionAsync(
            string functionName,
            string runtime,
            string handler,
            string zipFilePath,
            string roleArn,
            string? description = null,
            int timeout = 30,
            int memorySize = 128,
            Dictionary<string, string>? environmentVariables = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing Lambda function
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="zipFilePath">Path to new deployment package</param>
        /// <param name="description">Updated description</param>
        /// <param name="timeout">Updated timeout</param>
        /// <param name="memorySize">Updated memory size</param>
        /// <param name="environmentVariables">Updated environment variables</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<LambdaDeploymentResult> UpdateFunctionAsync(
            string functionName,
            string zipFilePath,
            string? description = null,
            int? timeout = null,
            int? memorySize = null,
            Dictionary<string, string>? environmentVariables = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a Lambda function
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deletion result</returns>
        Task<LambdaDeploymentResult> DeleteFunctionAsync(string functionName, CancellationToken cancellationToken = default);
    }
}
