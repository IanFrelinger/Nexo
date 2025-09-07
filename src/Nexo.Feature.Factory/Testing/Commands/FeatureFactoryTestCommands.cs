using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Commands
{
    /// <summary>
    /// Test command for validating AI model provider connectivity.
    /// </summary>
    public sealed class ValidateAiConnectivityTestCommand : TestCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidateAiConnectivityTestCommand(IServiceProvider serviceProvider, ILogger<ValidateAiConnectivityTestCommand> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override string CommandId => "validate-ai-connectivity";
        public override string Name => "Validate AI Connectivity";
        public override string Description => "Validates that AI model providers are accessible and responding";
        public override TestCategory Category => TestCategory.Integration;
        public override TestPriority Priority => TestPriority.Critical;
        public override TimeSpan EstimatedDuration => TimeSpan.FromSeconds(30);
        public override bool CanExecuteInParallel => false;
        public override string[] Dependencies => Array.Empty<string>();

        protected override async Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IModelOrchestrator>();
                
                // Test basic connectivity
                var testRequest = new Nexo.Feature.AI.Models.ModelRequest
                {
                    Input = "Test connectivity",
                    SystemPrompt = "Respond with 'OK' if you can process this request",
                    MaxTokens = 10,
                    Temperature = 0.1
                };

                var response = await orchestrator.ExecuteAsync(testRequest, cancellationToken);
                
                outputData["AiResponse"] = response.Response;
                outputData["AiApiCalls"] = 1;
                outputData["AiProcessingTime"] = TimeSpan.FromMilliseconds(100); // Estimated

                var isSuccess = !string.IsNullOrWhiteSpace(response.Response) && response.Response.Contains("OK");
                
                if (isSuccess)
                {
                    _logger.LogInformation("AI connectivity test passed");
                }
                else
                {
                    _logger.LogWarning("AI connectivity test failed: {Response}", response.Response);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI connectivity test failed with exception");
                outputData["Error"] = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Test command for validating domain analysis agent functionality.
    /// </summary>
    public sealed class ValidateDomainAnalysisTestCommand : TestCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidateDomainAnalysisTestCommand(IServiceProvider serviceProvider, ILogger<ValidateDomainAnalysisTestCommand> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override string CommandId => "validate-domain-analysis";
        public override string Name => "Validate Domain Analysis";
        public override string Description => "Validates that the domain analysis agent can extract entities and business rules";
        public override TestCategory Category => TestCategory.Integration;
        public override TestPriority Priority => TestPriority.High;
        public override TimeSpan EstimatedDuration => TimeSpan.FromMinutes(2);
        public override bool CanExecuteInParallel => false;
        public override string[] Dependencies => new[] { "validate-ai-connectivity" };

        protected override async Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<IFeatureOrchestrator>();
                
                var testDescription = "Customer with name, email, phone, billing address. Email must be unique and validated.";
                
                _logger.LogInformation("Testing domain analysis with: {Description}", testDescription);
                
                var specification = await orchestrator.AnalyzeFeatureAsync(testDescription, TargetPlatform.DotNet, cancellationToken);
                
                outputData["SpecificationId"] = specification.Id.Value;
                outputData["EntityCount"] = specification.Entities.Count;
                outputData["ValueObjectCount"] = specification.ValueObjects.Count;
                outputData["BusinessRuleCount"] = specification.BusinessRules.Count;
                outputData["ValidationRuleCount"] = specification.ValidationRules.Count;
                outputData["AiApiCalls"] = 4; // Estimated for domain analysis
                outputData["AiProcessingTime"] = TimeSpan.FromSeconds(30); // Estimated

                // Validate that we got meaningful results
                var hasEntities = specification.Entities.Count > 0;
                var hasCustomerEntity = specification.Entities.Any(e => e.Name.Contains("Customer", StringComparison.OrdinalIgnoreCase));
                var hasEmailProperty = specification.Entities.Any(e => e.Properties.Any(p => p.Name.Contains("Email", StringComparison.OrdinalIgnoreCase)));
                var hasValidationRules = specification.ValidationRules.Count > 0;

                var isSuccess = hasEntities && hasCustomerEntity && hasEmailProperty && hasValidationRules;

                if (isSuccess)
                {
                    _logger.LogInformation("Domain analysis test passed: {EntityCount} entities, {ValueObjectCount} value objects", 
                        specification.Entities.Count, specification.ValueObjects.Count);
                }
                else
                {
                    _logger.LogWarning("Domain analysis test failed: Entities={HasEntities}, Customer={HasCustomer}, Email={HasEmail}, Validation={HasValidation}", 
                        hasEntities, hasCustomerEntity, hasEmailProperty, hasValidationRules);
                }

                // Save specification for inspection
                var specFile = Path.Combine(context.Configuration.OutputDirectory, "test-specification.json");
                var specJson = System.Text.Json.JsonSerializer.Serialize(specification, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(specFile, specJson, cancellationToken);
                artifacts.Add(CreateArtifact("Test Specification", TestArtifactType.ConfigurationFile, specFile));

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain analysis test failed with exception");
                outputData["Error"] = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Test command for validating code generation functionality.
    /// </summary>
    public sealed class ValidateCodeGenerationTestCommand : TestCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidateCodeGenerationTestCommand(IServiceProvider serviceProvider, ILogger<ValidateCodeGenerationTestCommand> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override string CommandId => "validate-code-generation";
        public override string Name => "Validate Code Generation";
        public override string Description => "Validates that the code generation agent can create Clean Architecture code";
        public override TestCategory Category => TestCategory.Integration;
        public override TestPriority Priority => TestPriority.High;
        public override TimeSpan EstimatedDuration => TimeSpan.FromMinutes(3);
        public override bool CanExecuteInParallel => false;
        public override string[] Dependencies => new[] { "validate-domain-analysis" };

        protected override async Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<IFeatureOrchestrator>();
                
                var testDescription = "Customer with name, email, phone, billing address. Email must be unique and validated.";
                
                _logger.LogInformation("Testing code generation with: {Description}", testDescription);
                
                var result = await orchestrator.GenerateFeatureAsync(testDescription, TargetPlatform.DotNet, cancellationToken);
                
                outputData["IsSuccess"] = result.IsSuccess;
                outputData["ArtifactCount"] = result.CodeArtifacts.Count;
                outputData["GenerationDuration"] = result.Metadata.Duration;
                outputData["ExecutionStrategy"] = result.Metadata.ExecutionStrategy.ToString();
                outputData["AiApiCalls"] = 8; // Estimated for code generation
                outputData["AiProcessingTime"] = TimeSpan.FromMinutes(2); // Estimated

                if (!result.IsSuccess)
                {
                    _logger.LogError("Code generation failed: {Errors}", string.Join(", ", result.Errors));
                    outputData["Errors"] = result.Errors;
                    return false;
                }

                // Validate generated artifacts
                var hasEntity = result.CodeArtifacts.Any(a => a.Type == Application.Interfaces.ArtifactType.Entity);
                var hasRepository = result.CodeArtifacts.Any(a => a.Type == Application.Interfaces.ArtifactType.Repository);
                var hasUseCase = result.CodeArtifacts.Any(a => a.Type == Application.Interfaces.ArtifactType.UseCase);
                var hasTests = result.CodeArtifacts.Any(a => a.Type == Application.Interfaces.ArtifactType.Test);
                var hasValidCode = result.CodeArtifacts.All(a => !string.IsNullOrWhiteSpace(a.Content));

                var isSuccess = hasEntity && hasRepository && hasUseCase && hasTests && hasValidCode;

                if (isSuccess)
                {
                    _logger.LogInformation("Code generation test passed: {ArtifactCount} artifacts generated", result.CodeArtifacts.Count);
                    
                    // Save generated code for inspection
                    var outputDir = Path.Combine(context.Configuration.OutputDirectory, "generated-code");
                    Directory.CreateDirectory(outputDir);
                    
                    foreach (var artifact in result.CodeArtifacts)
                    {
                        var filePath = Path.Combine(outputDir, artifact.Name);
                        await File.WriteAllTextAsync(filePath, artifact.Content, cancellationToken);
                        artifacts.Add(CreateArtifact(artifact.Name, TestArtifactType.GeneratedCode, filePath));
                    }
                }
                else
                {
                    _logger.LogWarning("Code generation test failed: Entity={HasEntity}, Repository={HasRepository}, UseCase={HasUseCase}, Tests={HasTests}, ValidCode={HasValidCode}", 
                        hasEntity, hasRepository, hasUseCase, hasTests, hasValidCode);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Code generation test failed with exception");
                outputData["Error"] = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Test command for validating end-to-end feature generation.
    /// </summary>
    public sealed class ValidateEndToEndTestCommand : TestCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidateEndToEndTestCommand(IServiceProvider serviceProvider, ILogger<ValidateEndToEndTestCommand> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override string CommandId => "validate-end-to-end";
        public override string Name => "Validate End-to-End";
        public override string Description => "Validates complete end-to-end feature generation workflow";
        public override TestCategory Category => TestCategory.EndToEnd;
        public override TestPriority Priority => TestPriority.Critical;
        public override TimeSpan EstimatedDuration => TimeSpan.FromMinutes(5);
        public override bool CanExecuteInParallel => false;
        public override string[] Dependencies => new[] { "validate-code-generation" };

        protected override async Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<IFeatureOrchestrator>();
                
                var testDescription = "Order management system with Customer, Order, OrderItem, and Product entities. Orders have status, total amount, and date. OrderItems link products to orders with quantity and price. Products have name, description, and price.";
                
                _logger.LogInformation("Testing end-to-end generation with complex feature: {Description}", testDescription);
                
                var result = await orchestrator.GenerateFeatureAsync(testDescription, TargetPlatform.DotNet, cancellationToken);
                
                outputData["IsSuccess"] = result.IsSuccess;
                outputData["ArtifactCount"] = result.CodeArtifacts.Count;
                outputData["GenerationDuration"] = result.Metadata.Duration;
                outputData["ExecutionStrategy"] = result.Metadata.ExecutionStrategy.ToString();
                outputData["AiApiCalls"] = 15; // Estimated for complex feature
                outputData["AiProcessingTime"] = TimeSpan.FromMinutes(3); // Estimated

                if (!result.IsSuccess)
                {
                    _logger.LogError("End-to-end test failed: {Errors}", string.Join(", ", result.Errors));
                    outputData["Errors"] = result.Errors;
                    return false;
                }

                // Validate complex feature generation
                var entityCount = result.CodeArtifacts.Count(a => a.Type == Application.Interfaces.ArtifactType.Entity);
                var repositoryCount = result.CodeArtifacts.Count(a => a.Type == Application.Interfaces.ArtifactType.Repository);
                var useCaseCount = result.CodeArtifacts.Count(a => a.Type == Application.Interfaces.ArtifactType.UseCase);
                var testCount = result.CodeArtifacts.Count(a => a.Type == Application.Interfaces.ArtifactType.Test);
                
                var hasMultipleEntities = entityCount >= 4; // Customer, Order, OrderItem, Product
                var hasMultipleRepositories = repositoryCount >= 4;
                var hasMultipleUseCases = useCaseCount >= 12; // 3 use cases per entity (Create, Update, Delete)
                var hasMultipleTests = testCount >= 12;
                var hasValidCode = result.CodeArtifacts.All(a => !string.IsNullOrWhiteSpace(a.Content));

                var isSuccess = hasMultipleEntities && hasMultipleRepositories && hasMultipleUseCases && hasMultipleTests && hasValidCode;

                if (isSuccess)
                {
                    _logger.LogInformation("End-to-end test passed: {EntityCount} entities, {RepositoryCount} repositories, {UseCaseCount} use cases, {TestCount} tests", 
                        entityCount, repositoryCount, useCaseCount, testCount);
                    
                    // Save generated code for inspection
                    var outputDir = Path.Combine(context.Configuration.OutputDirectory, "e2e-generated-code");
                    Directory.CreateDirectory(outputDir);
                    
                    foreach (var artifact in result.CodeArtifacts)
                    {
                        var filePath = Path.Combine(outputDir, artifact.Name);
                        await File.WriteAllTextAsync(filePath, artifact.Content, cancellationToken);
                        artifacts.Add(CreateArtifact(artifact.Name, TestArtifactType.GeneratedCode, filePath));
                    }
                }
                else
                {
                    _logger.LogWarning("End-to-end test failed: Entities={EntityCount}, Repositories={RepositoryCount}, UseCases={UseCaseCount}, Tests={TestCount}, ValidCode={HasValidCode}", 
                        entityCount, repositoryCount, useCaseCount, testCount, hasValidCode);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "End-to-end test failed with exception");
                outputData["Error"] = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Test command for validating performance characteristics.
    /// </summary>
    public sealed class ValidatePerformanceTestCommand : TestCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidatePerformanceTestCommand(IServiceProvider serviceProvider, ILogger<ValidatePerformanceTestCommand> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override string CommandId => "validate-performance";
        public override string Name => "Validate Performance";
        public override string Description => "Validates that feature generation meets performance requirements";
        public override TestCategory Category => TestCategory.Performance;
        public override TestPriority Priority => TestPriority.Medium;
        public override TimeSpan EstimatedDuration => TimeSpan.FromMinutes(2);
        public override bool CanExecuteInParallel => false;
        public override string[] Dependencies => new[] { "validate-end-to-end" };

        protected override async Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<IFeatureOrchestrator>();
                
                var testDescription = "Customer with name, email, phone, billing address. Email must be unique and validated.";
                var maxDuration = TimeSpan.FromMinutes(1); // Performance requirement
                
                _logger.LogInformation("Testing performance with max duration: {MaxDuration}", maxDuration);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await orchestrator.GenerateFeatureAsync(testDescription, TargetPlatform.DotNet, cancellationToken);
                stopwatch.Stop();
                
                outputData["ActualDuration"] = stopwatch.Elapsed;
                outputData["MaxDuration"] = maxDuration;
                outputData["IsSuccess"] = result.IsSuccess;
                outputData["ArtifactCount"] = result.CodeArtifacts.Count;
                outputData["AiApiCalls"] = 8; // Estimated
                outputData["AiProcessingTime"] = stopwatch.Elapsed * 0.8; // Estimated 80% AI processing

                var isSuccess = result.IsSuccess && stopwatch.Elapsed <= maxDuration;

                if (isSuccess)
                {
                    _logger.LogInformation("Performance test passed: {Duration} <= {MaxDuration}", stopwatch.Elapsed, maxDuration);
                }
                else
                {
                    _logger.LogWarning("Performance test failed: {Duration} > {MaxDuration}", stopwatch.Elapsed, maxDuration);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Performance test failed with exception");
                outputData["Error"] = ex.Message;
                return false;
            }
        }
    }
}
