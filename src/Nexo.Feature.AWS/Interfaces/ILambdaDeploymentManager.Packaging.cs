using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Lambda function packaging functionality.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
        /// <summary>
        /// Creates a deployment package from source code
        /// </summary>
        /// <param name="sourcePath">Source code path</param>
        /// <param name="outputPath">Output ZIP file path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Package creation result</returns>
        Task<LambdaPackageResult> CreateDeploymentPackageAsync(
            string sourcePath,
            string outputPath,
            CancellationToken cancellationToken = default);
    }
}
