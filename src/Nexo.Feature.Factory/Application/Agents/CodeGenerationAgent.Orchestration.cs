using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Code generation orchestration and AI integration functionality
    /// </summary>
    public sealed partial class CodeGenerationAgent
    {
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
            var request = new Nexo.Feature.AI.Models.ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are a Clean Architecture expert. Generate high-quality, production-ready C# code that follows Clean Architecture principles, SOLID principles, and C# best practices. Return only the code without any additional text, explanations, or markdown formatting.",
                MaxTokens = 4000,
                Temperature = 0.2
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Response;
        }
    }
}
