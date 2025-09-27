using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Use case generation functionality for CRUD operations
    /// </summary>
    public sealed partial class CodeGenerationAgent
    {
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
    }
}
