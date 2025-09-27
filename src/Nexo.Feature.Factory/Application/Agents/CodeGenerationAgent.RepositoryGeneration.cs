using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Repository interface and implementation generation functionality
    /// </summary>
    public sealed partial class CodeGenerationAgent
    {
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
    }
}
