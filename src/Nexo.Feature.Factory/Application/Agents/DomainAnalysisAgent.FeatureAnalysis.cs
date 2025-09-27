using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Feature specification analysis and orchestration functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
        private async Task<FeatureSpecification> AnalyzeFeatureAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not (string description, TargetPlatform platform))
                throw new ArgumentException("Data must be a tuple of (string description, TargetPlatform platform)");

            var specification = new FeatureSpecification(
                FeatureSpecificationId.New(),
                description,
                platform
            );

            // Extract entities
            var entities = await ExtractEntitiesAsync(description, cancellationToken);
            foreach (var entity in entities)
            {
                specification.AddEntity(entity);
            }

            // Extract value objects
            var valueObjects = await ExtractValueObjectsAsync(description, cancellationToken);
            foreach (var valueObject in valueObjects)
            {
                specification.AddValueObject(valueObject);
            }

            // Extract business rules
            var businessRules = await ExtractBusinessRulesAsync(description, cancellationToken);
            foreach (var rule in businessRules)
            {
                specification.AddBusinessRule(rule);
            }

            // Extract validation rules
            var validationRules = await ExtractValidationRulesAsync(description, cancellationToken);
            foreach (var rule in validationRules)
            {
                specification.AddValidationRule(rule);
            }

            specification.UpdateStatus(FeatureSpecificationStatus.Analyzing);
            return specification;
        }
    }
}
