using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Entity and value object generation functionality
    /// </summary>
    public sealed partial class CodeGenerationAgent
    {
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
    }
}
