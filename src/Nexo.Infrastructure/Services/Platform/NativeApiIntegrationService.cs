using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.Platform.Integrators;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Native API integration service for Phase 6 - orchestrates specialized integrators
    /// Provides framework for integrating with native platform APIs and handling permissions.
    /// </summary>
    public partial class NativeApiIntegrationService : INativeApiIntegrationService
    {
        private readonly ILogger<NativeApiIntegrationService> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly NativeApiIntegrator _apiIntegrator;
        private readonly PermissionHandler _permissionHandler;
        private readonly ApiWrapperGenerator _wrapperGenerator;
        private readonly IntegrationValidator _integrationValidator;

        public NativeApiIntegrationService(
            ILogger<NativeApiIntegrationService> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            
            // Initialize specialized integrators
            _apiIntegrator = new NativeApiIntegrator(logger, modelOrchestrator);
            _permissionHandler = new PermissionHandler(logger, modelOrchestrator);
            _wrapperGenerator = new ApiWrapperGenerator(logger, modelOrchestrator);
            _integrationValidator = new IntegrationValidator(logger, modelOrchestrator);
        }

        /// <summary>
        /// Integrates with native platform APIs by delegating to API integrator
        /// </summary>
        public async Task<NativeApiIntegrationResult> IntegrateWithNativeApiAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            return await _apiIntegrator.IntegrateWithNativeApiAsync(platform, apiName, parameters, cancellationToken);
        }

        /// <summary>
        /// Handles permissions by delegating to permission handler
        /// </summary>
        public async Task<PermissionHandlingResult> HandlePermissionsAsync(
            string platform,
            string apiName,
            List<string> requiredPermissions,
            CancellationToken cancellationToken = default)
        {
            return await _permissionHandler.HandlePermissionsAsync(platform, apiName, requiredPermissions, cancellationToken);
        }

        /// <summary>
        /// Generates API wrapper by delegating to wrapper generator
        /// </summary>
        public async Task<NativeApiWrapper> GenerateApiWrapperAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            return await _wrapperGenerator.GenerateApiWrapperAsync(platform, apiName, parameters, cancellationToken);
        }

        /// <summary>
        /// Validates integration by delegating to integration validator
        /// </summary>
        public async Task<NativeApiValidationResult> ValidateIntegrationAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            return await _integrationValidator.ValidateIntegrationAsync(platform, apiName, parameters, cancellationToken);
        }
    }
}
