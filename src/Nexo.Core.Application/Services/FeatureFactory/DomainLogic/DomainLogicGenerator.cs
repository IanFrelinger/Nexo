using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.DomainLogic
{
    public class DomainLogicGenerator : IDomainLogicGenerator
    {
        private readonly ILogger<DomainLogicGenerator> _logger;
        private readonly IAIRuntimeSelector _runtimeSelector;

        public DomainLogicGenerator(ILogger<DomainLogicGenerator> logger, IAIRuntimeSelector runtimeSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
        }

        public Task<DomainLogicResult> GenerateDomainLogicAsync(ValidatedRequirements requirements, CancellationToken cancellationToken) => Task.FromResult(new DomainLogicResult());
        public Task<BusinessEntityResult> GenerateBusinessEntitiesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new BusinessEntityResult());
        public Task<ValueObjectResult> GenerateValueObjectsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new ValueObjectResult());
        public Task<BusinessRuleResult> GenerateBusinessRulesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new BusinessRuleResult());
        public Task<DomainServiceResult> GenerateDomainServicesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new DomainServiceResult());
        public Task<AggregateRootResult> GenerateAggregateRootsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new AggregateRootResult());
        public Task<DomainEventResult> GenerateDomainEventsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken) => Task.FromResult(new DomainEventResult());
        public Task<RepositoryResult> GenerateRepositoriesAsync(List<DomainEntity> entities, CancellationToken cancellationToken) => Task.FromResult(new RepositoryResult());
        public Task<FactoryResult> GenerateFactoriesAsync(List<DomainEntity> entities, CancellationToken cancellationToken) => Task.FromResult(new FactoryResult());
        public Task<SpecificationResult> GenerateSpecificationsAsync(List<DomainEntity> entities, CancellationToken cancellationToken) => Task.FromResult(new SpecificationResult());
    }
}
