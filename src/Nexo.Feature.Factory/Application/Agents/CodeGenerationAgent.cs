using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// AI agent specialized in generating Clean Architecture code from domain specifications.
    /// </summary>
    public sealed class CodeGenerationAgent : IAgent
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<CodeGenerationAgent> _logger;
        private AgentState _status = AgentState.Idle;

        public string AgentId => "code-generation-agent";
        public string Name => "Code Generation Agent";
        public string Description => "Generates Clean Architecture code from domain specifications";
        public AgentState Status => _status;

        public IReadOnlyList<AgentCapability> Capabilities => new List<AgentCapability>
        {
            new AgentCapability("EntityGeneration", "Generate entity classes", "EntityDefinition", "string"),
            new AgentCapability("ValueObjectGeneration", "Generate value object classes", "ValueObjectDefinition", "string"),
            new AgentCapability("RepositoryGeneration", "Generate repository interfaces and implementations", "EntityDefinition", "string[]"),
            new AgentCapability("UseCaseGeneration", "Generate use case classes", "EntityDefinition", "string[]"),
            new AgentCapability("TestGeneration", "Generate unit tests", "EntityDefinition", "string[]")
        };

        public CodeGenerationAgent(IModelOrchestrator modelOrchestrator, ILogger<CodeGenerationAgent> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Idle;
            _logger.LogInformation("Code Generation Agent initialized");
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Offline;
            _logger.LogInformation("Code Generation Agent shut down");
            await Task.CompletedTask;
        }

        public async Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _status = AgentState.Working;
                _logger.LogInformation("Processing code generation request: {RequestId}", request.RequestId);

                object result = request.RequestType switch
                {
                    "GenerateEntity" => await GenerateEntityAsync(request.Data, cancellationToken),
                    "GenerateValueObject" => await GenerateValueObjectAsync(request.Data, cancellationToken),
                    "GenerateRepository" => await GenerateRepositoryAsync(request.Data, cancellationToken),
                    "GenerateUseCase" => await GenerateUseCaseAsync(request.Data, cancellationToken),
                    "GenerateTests" => await GenerateTestsAsync(request.Data, cancellationToken),
                    "GenerateAll" => await GenerateAllAsync(request.Data, cancellationToken),
                    _ => throw new NotSupportedException($"Request type '{request.RequestType}' is not supported")
                };

                _status = AgentState.Idle;
                return new AgentResponse(request.RequestId, result, true);
            }
            catch (Exception ex)
            {
                _status = AgentState.Error;
                _logger.LogError(ex, "Error processing code generation request: {RequestId}", request.RequestId);
                return new AgentResponse(request.RequestId, new { Error = ex.Message }, false, ex.Message);
            }
        }

        private async Task<string> GenerateEntityAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not EntityDefinition entityDefinition)
                throw new ArgumentException("Data must be an EntityDefinition");

            var prompt = $@"
Generate a Clean Architecture entity class in C# for the following specification:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}
Include CRUD Operations: {entityDefinition.IncludeCrudOperations}
Include Validation: {entityDefinition.IncludeValidation}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description} - Required: {p.IsRequired}, Unique: {p.IsUnique}"))}

Business Rules:
{string.Join("\n", entityDefinition.Methods.Select(m => $"- {m.Name}: {m.Description}"))}

Requirements:
1. Follow Clean Architecture principles
2. Use proper encapsulation with private setters
3. Include validation in the constructor and property setters
4. Implement proper equality comparison
5. Include domain events if applicable
6. Use value objects for complex types
7. Follow C# naming conventions
8. Include XML documentation
9. Make the class sealed
10. Include a factory method for creation

Generate only the entity class code without any additional text or explanations:";

            return await CallAIAsync(prompt, cancellationToken);
        }

        private async Task<string> GenerateValueObjectAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not ValueObjectDefinition valueObjectDefinition)
                throw new ArgumentException("Data must be a ValueObjectDefinition");

            var prompt = $@"
Generate a Clean Architecture value object class in C# for the following specification:

Value Object Name: {valueObjectDefinition.Name}
Description: {valueObjectDefinition.Description}
Namespace: {valueObjectDefinition.Namespace}
Include Validation: {valueObjectDefinition.IncludeValidation}

Properties:
{string.Join("\n", valueObjectDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description} - Required: {p.IsRequired}"))}

Methods:
{string.Join("\n", valueObjectDefinition.Methods.Select(m => $"- {m.Name}: {m.Description}"))}

Requirements:
1. Follow Clean Architecture principles
2. Make the class immutable
3. Implement proper equality comparison (Equals, GetHashCode, ==, !=)
4. Include validation in the constructor
5. Use proper encapsulation
6. Follow C# naming conventions
7. Include XML documentation
8. Make the class sealed
9. Include factory methods for creation
10. Handle null values appropriately

Generate only the value object class code without any additional text or explanations:";

            return await CallAIAsync(prompt, cancellationToken);
        }

        private async Task<List<string>> GenerateRepositoryAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not EntityDefinition entityDefinition)
                throw new ArgumentException("Data must be an EntityDefinition");

            var interfacePrompt = $@"
Generate a repository interface in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description}"))}

Requirements:
1. Follow Clean Architecture principles
2. Place in Application layer (Interfaces)
3. Include standard CRUD operations
4. Include query methods for unique properties
5. Use async/await pattern
6. Include proper cancellation token support
7. Follow C# naming conventions
8. Include XML documentation
9. Use generic repository pattern if applicable

Generate only the repository interface code without any additional text or explanations:";

            var implementationPrompt = $@"
Generate a repository implementation in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description}"))}

Requirements:
1. Follow Clean Architecture principles
2. Place in Infrastructure layer
3. Implement the repository interface
4. Use Entity Framework Core or similar ORM
5. Include proper error handling
6. Use async/await pattern
7. Include proper cancellation token support
8. Follow C# naming conventions
9. Include XML documentation
10. Include unit of work pattern

Generate only the repository implementation code without any additional text or explanations:";

            var interfaceCode = await CallAIAsync(interfacePrompt, cancellationToken);
            var implementationCode = await CallAIAsync(implementationPrompt, cancellationToken);

            return new List<string> { interfaceCode, implementationCode };
        }

        private async Task<List<string>> GenerateUseCaseAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not EntityDefinition entityDefinition)
                throw new ArgumentException("Data must be an EntityDefinition");

            var createPrompt = $@"
Generate a Create use case in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description} - Required: {p.IsRequired}"))}

Requirements:
1. Follow Clean Architecture principles
2. Place in Application layer (UseCases)
3. Include input/output models
4. Include validation
5. Use repository pattern
6. Include proper error handling
7. Use async/await pattern
8. Follow C# naming conventions
9. Include XML documentation
10. Include domain events if applicable

Generate only the Create use case code without any additional text or explanations:";

            var updatePrompt = $@"
Generate an Update use case in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description} - Required: {p.IsRequired}"))}

Requirements:
1. Follow Clean Architecture principles
2. Place in Application layer (UseCases)
3. Include input/output models
4. Include validation
5. Use repository pattern
6. Include proper error handling
7. Use async/await pattern
8. Follow C# naming conventions
9. Include XML documentation
10. Include domain events if applicable

Generate only the Update use case code without any additional text or explanations:";

            var deletePrompt = $@"
Generate a Delete use case in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Requirements:
1. Follow Clean Architecture principles
2. Place in Application layer (UseCases)
3. Include input/output models
4. Include validation
5. Use repository pattern
6. Include proper error handling
7. Use async/await pattern
8. Follow C# naming conventions
9. Include XML documentation
10. Include domain events if applicable

Generate only the Delete use case code without any additional text or explanations:";

            var getByIdPrompt = $@"
Generate a GetById use case in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Requirements:
1. Follow Clean Architecture principles
2. Place in Application layer (UseCases)
3. Include input/output models
4. Include validation
5. Use repository pattern
6. Include proper error handling
7. Use async/await pattern
8. Follow C# naming conventions
9. Include XML documentation

Generate only the GetById use case code without any additional text or explanations:";

            var createCode = await CallAIAsync(createPrompt, cancellationToken);
            var updateCode = await CallAIAsync(updatePrompt, cancellationToken);
            var deleteCode = await CallAIAsync(deletePrompt, cancellationToken);
            var getByIdCode = await CallAIAsync(getByIdPrompt, cancellationToken);

            return new List<string> { createCode, updateCode, deleteCode, getByIdCode };
        }

        private async Task<List<string>> GenerateTestsAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not EntityDefinition entityDefinition)
                throw new ArgumentException("Data must be an EntityDefinition");

            var entityTestPrompt = $@"
Generate unit tests in C# for the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Properties:
{string.Join("\n", entityDefinition.Properties.Select(p => $"- {p.Name} ({p.Type}): {p.Description} - Required: {p.IsRequired}"))}

Requirements:
1. Use xUnit testing framework
2. Use FluentAssertions for assertions
3. Use Moq for mocking
4. Test all public methods and properties
5. Test validation rules
6. Test edge cases and error conditions
7. Include proper test naming conventions
8. Include XML documentation
9. Test constructor validation
10. Test equality comparison

Generate only the entity unit test code without any additional text or explanations:";

            var repositoryTestPrompt = $@"
Generate unit tests in C# for the repository of the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Requirements:
1. Use xUnit testing framework
2. Use FluentAssertions for assertions
3. Use Moq for mocking
4. Test all repository methods
5. Test error handling
6. Test async operations
7. Include proper test naming conventions
8. Include XML documentation
9. Test CRUD operations
10. Test query methods

Generate only the repository unit test code without any additional text or explanations:";

            var useCaseTestPrompt = $@"
Generate unit tests in C# for the use cases of the following entity:

Entity Name: {entityDefinition.Name}
Description: {entityDefinition.Description}
Namespace: {entityDefinition.Namespace}

Requirements:
1. Use xUnit testing framework
2. Use FluentAssertions for assertions
3. Use Moq for mocking
4. Test all use case methods
5. Test validation
6. Test error handling
7. Test async operations
8. Include proper test naming conventions
9. Include XML documentation
10. Test success and failure scenarios

Generate only the use case unit test code without any additional text or explanations:";

            var entityTestCode = await CallAIAsync(entityTestPrompt, cancellationToken);
            var repositoryTestCode = await CallAIAsync(repositoryTestPrompt, cancellationToken);
            var useCaseTestCode = await CallAIAsync(useCaseTestPrompt, cancellationToken);

            return new List<string> { entityTestCode, repositoryTestCode, useCaseTestCode };
        }

        private async Task<List<CodeArtifact>> GenerateAllAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not FeatureSpecification specification)
                throw new ArgumentException("Data must be a FeatureSpecification");

            var artifacts = new List<CodeArtifact>();

            // Generate entities
            foreach (var entity in specification.Entities)
            {
                var entityCode = await GenerateEntityAsync(entity, cancellationToken);
                artifacts.Add(new CodeArtifact(
                    $"{entity.Name}.cs",
                    ArtifactType.Entity,
                    entityCode,
                    $"src/Domain/Entities/{entity.Name}.cs",
                    entity.Namespace
                ));
            }

            // Generate value objects
            foreach (var valueObject in specification.ValueObjects)
            {
                var valueObjectCode = await GenerateValueObjectAsync(valueObject, cancellationToken);
                artifacts.Add(new CodeArtifact(
                    $"{valueObject.Name}.cs",
                    ArtifactType.ValueObject,
                    valueObjectCode,
                    $"src/Domain/ValueObjects/{valueObject.Name}.cs",
                    valueObject.Namespace
                ));
            }

            // Generate repositories and use cases for entities
            foreach (var entity in specification.Entities)
            {
                var repositoryCodes = await GenerateRepositoryAsync(entity, cancellationToken);
                artifacts.Add(new CodeArtifact(
                    $"I{entity.Name}Repository.cs",
                    ArtifactType.Repository,
                    repositoryCodes[0],
                    $"src/Application/Interfaces/I{entity.Name}Repository.cs",
                    "Application.Interfaces"
                ));
                artifacts.Add(new CodeArtifact(
                    $"{entity.Name}Repository.cs",
                    ArtifactType.Repository,
                    repositoryCodes[1],
                    $"src/Infrastructure/Repositories/{entity.Name}Repository.cs",
                    "Infrastructure.Repositories"
                ));

                var useCaseCodes = await GenerateUseCaseAsync(entity, cancellationToken);
                var useCaseNames = new[] { "Create", "Update", "Delete", "GetById" };
                for (int i = 0; i < useCaseCodes.Count; i++)
                {
                    artifacts.Add(new CodeArtifact(
                        $"{useCaseNames[i]}{entity.Name}UseCase.cs",
                        ArtifactType.UseCase,
                        useCaseCodes[i],
                        $"src/Application/UseCases/{useCaseNames[i]}{entity.Name}UseCase.cs",
                        "Application.UseCases"
                    ));
                }

                var testCodes = await GenerateTestsAsync(entity, cancellationToken);
                var testNames = new[] { "Entity", "Repository", "UseCase" };
                for (int i = 0; i < testCodes.Count; i++)
                {
                    artifacts.Add(new CodeArtifact(
                        $"{entity.Name}{testNames[i]}Tests.cs",
                        ArtifactType.Test,
                        testCodes[i],
                        $"tests/{entity.Name}{testNames[i]}Tests.cs",
                        "Tests"
                    ));
                }
            }

            return artifacts;
        }

        private async Task<string> CallAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new Nexo.Feature.AI.Models.ModelRequest(0.7, 0.0, 0.0, false)
            {
                Input = prompt,
                SystemPrompt = "You are a Clean Architecture expert. Generate high-quality, production-ready C# code that follows Clean Architecture principles, SOLID principles, and C# best practices. Return only the code without any additional text, explanations, or markdown formatting.",
                MaxTokens = 4000,
                Temperature = 0.2
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Content;
        }
    }
}
