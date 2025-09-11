using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Enums.FeatureFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Deployment
{
    public class DeploymentManager : IDeploymentManager
    {
        private readonly ILogger<DeploymentManager> _logger;
        private readonly IAIRuntimeSelector _runtimeSelector;

        public DeploymentManager(ILogger<DeploymentManager> logger, IAIRuntimeSelector runtimeSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
        }

        public Task<DeploymentResult> DeployApplicationAsync(ApplicationLogicResult application, DeploymentTarget target, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentResult> DeployToCloudAsync(ApplicationLogicResult application, CloudProvider provider, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentResult> DeployToContainerAsync(ApplicationLogicResult application, ContainerPlatform platform, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentResult> DeployToDesktopAsync(ApplicationLogicResult application, DesktopPlatform platform, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentResult> DeployToMobileAsync(ApplicationLogicResult application, MobilePlatform platform, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentResult> DeployToWebAsync(ApplicationLogicResult application, WebPlatform platform, CancellationToken cancellationToken) => Task.FromResult(new DeploymentResult { Success = true });
        public Task<DeploymentPackage> CreateDeploymentPackageAsync(ApplicationLogicResult application, Nexo.Core.Domain.Enums.FeatureFactory.PackageType packageType, CancellationToken cancellationToken) => Task.FromResult(new DeploymentPackage { PackageType = packageType });
        public Task<ValidationResult> ValidateDeploymentTargetAsync(DeploymentTarget target, CancellationToken cancellationToken) => Task.FromResult(new ValidationResult { IsValid = true });
        public Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken) => Task.FromResult(new DeploymentStatus { State = DeploymentState.Completed });
        public Task<RollbackResult> RollbackDeploymentAsync(string deploymentId, CancellationToken cancellationToken) => Task.FromResult(new RollbackResult { Success = true });
    }
}
