using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.FeatureFactory.DomainLogic;
using Nexo.Core.Application.Services.FeatureFactory.TestGeneration;
using Nexo.Core.Application.Services.FeatureFactory.Validation;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration.Components
{
    public partial class DomainLogicWorkflowExecutor
    {
        private readonly ILogger _logger;
        private readonly IDomainLogicGenerator _domainLogicGenerator;
        private readonly ITestSuiteGenerator _testSuiteGenerator;
        private readonly IDomainLogicValidator _domainLogicValidator;

        public DomainLogicWorkflowExecutor(
            ILogger logger,
            IDomainLogicGenerator domainLogicGenerator,
            ITestSuiteGenerator testSuiteGenerator,
            IDomainLogicValidator domainLogicValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _domainLogicGenerator = domainLogicGenerator ?? throw new ArgumentNullException(nameof(domainLogicGenerator));
            _testSuiteGenerator = testSuiteGenerator ?? throw new ArgumentNullException(nameof(testSuiteGenerator));
            _domainLogicValidator = domainLogicValidator ?? throw new ArgumentNullException(nameof(domainLogicValidator));
        }

        public async Task<DomainLogicGenerationResult> ExecuteWorkflowAsync(
            string sessionId,
            ValidatedRequirements requirements,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Executing domain logic workflow for session {SessionId}", sessionId);

                var result = new DomainLogicGenerationResult
                {
                    Success = false,
                    SessionId = sessionId,
                    GeneratedAt = DateTime.UtcNow
                };

                // Step 1: Generate domain logic
                await ExecuteDomainLogicGenerationAsync(sessionId, requirements, result, cancellationToken);
                if (!result.Success) return result;

                // Step 2: Generate test suite
                await ExecuteTestSuiteGenerationAsync(sessionId, result, cancellationToken);
                if (!result.Success) return result;

                // Step 3: Validate generated code
                await ExecuteValidationAsync(sessionId, result, cancellationToken);
                if (!result.Success) return result;

                // Step 4: Optimize generated code
                await ExecuteOptimizationAsync(sessionId, result, cancellationToken);

                result.Success = true;
                result.Message = "Domain logic generation completed successfully";

                _logger.LogInformation("Domain logic workflow completed successfully for session {SessionId}", sessionId);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Domain logic workflow cancelled for session {SessionId}", sessionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain logic workflow failed for session {SessionId}", sessionId);
                throw;
            }
        }

        private async Task ExecuteDomainLogicGenerationAsync(
            string sessionId,
            ValidatedRequirements requirements,
            DomainLogicGenerationResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Executing domain logic generation for session {SessionId}", sessionId);

                var domainLogicResult = await _domainLogicGenerator.GenerateDomainLogicAsync(requirements, cancellationToken);
                
                if (domainLogicResult.Success)
                {
                    result.DomainLogic = domainLogicResult.DomainLogic;
                    result.Entities = domainLogicResult.Entities;
                    result.ValueObjects = domainLogicResult.ValueObjects;
                    result.DomainServices = domainLogicResult.DomainServices;
                    result.BusinessRules = domainLogicResult.BusinessRules;
                    
                    _logger.LogInformation("Domain logic generation completed for session {SessionId}", sessionId);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Domain logic generation failed";
                    result.Errors.AddRange(domainLogicResult.Errors);
                    
                    _logger.LogError("Domain logic generation failed for session {SessionId}: {Errors}", 
                        sessionId, string.Join(", ", domainLogicResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain logic generation error for session {SessionId}", sessionId);
                result.Success = false;
                result.Message = $"Domain logic generation error: {ex.Message}";
                result.Errors.Add(ex.Message);
            }
        }

        private async Task ExecuteTestSuiteGenerationAsync(
            string sessionId,
            DomainLogicGenerationResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Executing test suite generation for session {SessionId}", sessionId);

                var testSuiteResult = await _testSuiteGenerator.GenerateTestSuiteAsync(result.DomainLogic, cancellationToken);
                
                if (testSuiteResult.Success)
                {
                    result.TestSuite = testSuiteResult.TestSuite;
                    result.UnitTests = testSuiteResult.UnitTests;
                    result.IntegrationTests = testSuiteResult.IntegrationTests;
                    
                    _logger.LogInformation("Test suite generation completed for session {SessionId}", sessionId);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Test suite generation failed";
                    result.Errors.AddRange(testSuiteResult.Errors);
                    
                    _logger.LogError("Test suite generation failed for session {SessionId}: {Errors}", 
                        sessionId, string.Join(", ", testSuiteResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test suite generation error for session {SessionId}", sessionId);
                result.Success = false;
                result.Message = $"Test suite generation error: {ex.Message}";
                result.Errors.Add(ex.Message);
            }
        }

        private async Task ExecuteValidationAsync(
            string sessionId,
            DomainLogicGenerationResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Executing validation for session {SessionId}", sessionId);

                var validationResult = await _domainLogicValidator.ValidateDomainLogicAsync(result.DomainLogic, cancellationToken);
                
                if (validationResult.Success)
                {
                    result.ValidationResults = validationResult;
                    
                    _logger.LogInformation("Validation completed for session {SessionId}", sessionId);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Validation failed";
                    result.Errors.AddRange(validationResult.Errors);
                    
                    _logger.LogError("Validation failed for session {SessionId}: {Errors}", 
                        sessionId, string.Join(", ", validationResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error for session {SessionId}", sessionId);
                result.Success = false;
                result.Message = $"Validation error: {ex.Message}";
                result.Errors.Add(ex.Message);
            }
        }

        private async Task ExecuteOptimizationAsync(
            string sessionId,
            DomainLogicGenerationResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Executing optimization for session {SessionId}", sessionId);

                // Perform code optimization
                await OptimizeGeneratedCodeAsync(result);
                
                _logger.LogInformation("Optimization completed for session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Optimization error for session {SessionId}", sessionId);
                // Don't fail the entire workflow for optimization errors
                result.Warnings.Add($"Optimization warning: {ex.Message}");
            }
        }

        private async Task OptimizeGeneratedCodeAsync(DomainLogicGenerationResult result)
        {
            try
            {
                // Optimize entities
                foreach (var entity in result.Entities)
                {
                    await OptimizeEntityAsync(entity);
                }

                // Optimize value objects
                foreach (var valueObject in result.ValueObjects)
                {
                    await OptimizeValueObjectAsync(valueObject);
                }

                // Optimize domain services
                foreach (var service in result.DomainServices)
                {
                    await OptimizeDomainServiceAsync(service);
                }

                // Optimize business rules
                foreach (var rule in result.BusinessRules)
                {
                    await OptimizeBusinessRuleAsync(rule);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during code optimization");
                throw;
            }
        }

        private async Task OptimizeEntityAsync(DomainEntity entity)
        {
            // Implementation would optimize entity code
            await Task.Delay(10);
        }

        private async Task OptimizeValueObjectAsync(ValueObject valueObject)
        {
            // Implementation would optimize value object code
            await Task.Delay(10);
        }

        private async Task OptimizeDomainServiceAsync(DomainService domainService)
        {
            // Implementation would optimize domain service code
            await Task.Delay(10);
        }

        private async Task OptimizeBusinessRuleAsync(BusinessRule businessRule)
        {
            // Implementation would optimize business rule code
            await Task.Delay(10);
        }
    }
}
