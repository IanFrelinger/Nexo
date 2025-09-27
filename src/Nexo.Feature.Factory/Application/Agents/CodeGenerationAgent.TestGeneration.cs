using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Unit test generation functionality
    /// </summary>
    public sealed partial class CodeGenerationAgent
    {
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
    }
}
