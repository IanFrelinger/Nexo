using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// Value object extraction and parsing functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
        private async Task<List<ValueObjectDefinition>> ExtractValueObjectsAsync(object data, CancellationToken cancellationToken)
        {
            var description = data as string ?? throw new ArgumentException("Data must be a string description");

            var prompt = $@"
Analyze the following natural language description and extract value objects.
Return a JSON array of value object definitions with the following structure:
{{
  ""valueObjects"": [
    {{
      ""name"": ""ValueObjectName"",
      ""description"": ""Value object description"",
      ""namespace"": ""Domain.ValueObjects"",
      ""properties"": [
        {{
          ""name"": ""PropertyName"",
          ""type"": ""string"",
          ""description"": ""Property description"",
          ""isRequired"": true
        }}
      ],
      ""includeValidation"": true
    }}
  ]
}}

Description: {description}

Focus on:
1. Immutable value objects (Email, Address, Money, etc.)
2. Complex types that represent concepts
3. Validation rules for value objects
4. Properties that should be grouped together

Return only valid JSON:";

            var response = await CallAIAsync(prompt, cancellationToken);
            return ParseValueObjectDefinitions(response);
        }

        private List<ValueObjectDefinition> ParseValueObjectDefinitions(string jsonResponse)
        {
            try
            {
                var document = JsonDocument.Parse(jsonResponse);
                var valueObjects = new List<ValueObjectDefinition>();

                if (document.RootElement.TryGetProperty("valueObjects", out var valueObjectsArray))
                {
                    foreach (var valueObjectElement in valueObjectsArray.EnumerateArray())
                    {
                        var valueObject = ParseValueObjectDefinition(valueObjectElement);
                        valueObjects.Add(valueObject);
                    }
                }

                return valueObjects;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse value object definitions from AI response");
                return new List<ValueObjectDefinition>();
            }
        }

        private ValueObjectDefinition ParseValueObjectDefinition(JsonElement valueObjectElement)
        {
            var name = valueObjectElement.GetProperty("name").GetString() ?? "UnknownValueObject";
            var description = valueObjectElement.GetProperty("description").GetString() ?? "No description";
            var @namespace = valueObjectElement.GetProperty("namespace").GetString() ?? "UnknownNamespace";
            var includeValidation = valueObjectElement.TryGetProperty("includeValidation", out var validationElement) ? validationElement.GetBoolean() : true;

            var valueObject = new ValueObjectDefinition(name, description, @namespace, includeValidation);

            if (valueObjectElement.TryGetProperty("properties", out var propertiesArray))
            {
                foreach (var propertyElement in propertiesArray.EnumerateArray())
                {
                    var property = ParsePropertyDefinition(propertyElement);
                    valueObject.AddProperty(property);
                }
            }

            return valueObject;
        }
    }
}
