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
    /// Entity extraction and parsing functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
        private async Task<List<EntityDefinition>> ExtractEntitiesAsync(object data, CancellationToken cancellationToken)
        {
            var description = data as string ?? throw new ArgumentException("Data must be a string description");

            var prompt = $@"
Analyze the following natural language description and extract domain entities. 
Return a JSON array of entity definitions with the following structure:
{{
  ""entities"": [
    {{
      ""name"": ""EntityName"",
      ""description"": ""Entity description"",
      ""namespace"": ""Domain.Entities"",
      ""properties"": [
        {{
          ""name"": ""PropertyName"",
          ""type"": ""string"",
          ""description"": ""Property description"",
          ""isRequired"": true,
          ""isUnique"": false
        }}
      ],
      ""includeCrudOperations"": true,
      ""includeValidation"": true
    }}
  ]
}}

Description: {description}

Focus on:
1. Main business entities (Customer, Order, Product, etc.)
2. Properties with their types and constraints
3. Required vs optional properties
4. Unique constraints
5. Business relationships

Return only valid JSON:";

            var response = await CallAIAsync(prompt, cancellationToken);
            return ParseEntityDefinitions(response);
        }

        private List<EntityDefinition> ParseEntityDefinitions(string jsonResponse)
        {
            try
            {
                var document = JsonDocument.Parse(jsonResponse);
                var entities = new List<EntityDefinition>();

                if (document.RootElement.TryGetProperty("entities", out var entitiesArray))
                {
                    foreach (var entityElement in entitiesArray.EnumerateArray())
                    {
                        var entity = ParseEntityDefinition(entityElement);
                        entities.Add(entity);
                    }
                }

                return entities;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse entity definitions from AI response");
                return new List<EntityDefinition>();
            }
        }

        private EntityDefinition ParseEntityDefinition(JsonElement entityElement)
        {
            var name = entityElement.GetProperty("name").GetString() ?? "UnknownEntity";
            var description = entityElement.GetProperty("description").GetString() ?? "No description";
            var @namespace = entityElement.GetProperty("namespace").GetString() ?? "UnknownNamespace";
            var includeCrud = entityElement.TryGetProperty("includeCrudOperations", out var crudElement) ? crudElement.GetBoolean() : true;
            var includeValidation = entityElement.TryGetProperty("includeValidation", out var validationElement) ? validationElement.GetBoolean() : true;

            var entity = new EntityDefinition(name, description, @namespace, includeCrud, includeValidation);

            if (entityElement.TryGetProperty("properties", out var propertiesArray))
            {
                foreach (var propertyElement in propertiesArray.EnumerateArray())
                {
                    var property = ParsePropertyDefinition(propertyElement);
                    entity.AddProperty(property);
                }
            }

            return entity;
        }

        private PropertyDefinition ParsePropertyDefinition(JsonElement propertyElement)
        {
            var name = propertyElement.GetProperty("name").GetString() ?? "UnknownProperty";
            var type = propertyElement.GetProperty("type").GetString() ?? "string";
            var description = propertyElement.GetProperty("description").GetString() ?? "No description";
            var isRequired = propertyElement.TryGetProperty("isRequired", out var requiredElement) ? requiredElement.GetBoolean() : false;
            var isUnique = propertyElement.TryGetProperty("isUnique", out var uniqueElement) ? uniqueElement.GetBoolean() : false;

            return new PropertyDefinition(name, type, description, isRequired, isUnique);
        }
    }
}
