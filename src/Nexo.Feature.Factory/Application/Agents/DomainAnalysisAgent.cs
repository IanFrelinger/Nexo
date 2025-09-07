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
using Nexo.Feature.Factory.Domain.ValueObjects;
using System.Text.Json;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// AI agent specialized in analyzing natural language descriptions and extracting domain entities, value objects, and business rules.
    /// </summary>
    public sealed class DomainAnalysisAgent : IAgent
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<DomainAnalysisAgent> _logger;
        private AgentState _status = AgentState.Idle;

        public string AgentId => "domain-analysis-agent";
        public string Name => "Domain Analysis Agent";
        public string Description => "Analyzes natural language descriptions to extract domain entities, value objects, and business rules";
        public AgentState Status => _status;

        public IReadOnlyList<AgentCapability> Capabilities => new List<AgentCapability>
        {
            new AgentCapability("EntityExtraction", "Extract entities from natural language", "string", "EntityDefinition[]"),
            new AgentCapability("ValueObjectExtraction", "Extract value objects from natural language", "string", "ValueObjectDefinition[]"),
            new AgentCapability("BusinessRuleExtraction", "Extract business rules from natural language", "string", "BusinessRule[]"),
            new AgentCapability("ValidationRuleExtraction", "Extract validation rules from natural language", "string", "ValidationRule[]")
        };

        public DomainAnalysisAgent(IModelOrchestrator modelOrchestrator, ILogger<DomainAnalysisAgent> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Idle;
            _logger.LogInformation("Domain Analysis Agent initialized");
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Offline;
            _logger.LogInformation("Domain Analysis Agent shut down");
            await Task.CompletedTask;
        }

        public async Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _status = AgentState.Working;
                _logger.LogInformation("Processing domain analysis request: {RequestId}", request.RequestId);

                object result = request.RequestType switch
                {
                    "AnalyzeFeature" => await AnalyzeFeatureAsync(request.Data, cancellationToken),
                    "ExtractEntities" => await ExtractEntitiesAsync(request.Data, cancellationToken),
                    "ExtractValueObjects" => await ExtractValueObjectsAsync(request.Data, cancellationToken),
                    "ExtractBusinessRules" => await ExtractBusinessRulesAsync(request.Data, cancellationToken),
                    _ => throw new NotSupportedException($"Request type '{request.RequestType}' is not supported")
                };

                _status = AgentState.Idle;
                return new AgentResponse(request.RequestId, result, true);
            }
            catch (Exception ex)
            {
                _status = AgentState.Error;
                _logger.LogError(ex, "Error processing domain analysis request: {RequestId}", request.RequestId);
                return new AgentResponse(request.RequestId, new { Error = ex.Message }, false, ex.Message);
            }
        }

        private async Task<FeatureSpecification> AnalyzeFeatureAsync(object data, CancellationToken cancellationToken)
        {
            if (data is not (string description, TargetPlatform platform))
                throw new ArgumentException("Data must be a tuple of (string description, TargetPlatform platform)");

            var specification = new FeatureSpecification(
                FeatureSpecificationId.New(),
                description,
                platform
            );

            // Extract entities
            var entities = await ExtractEntitiesAsync(description, cancellationToken);
            foreach (var entity in entities)
            {
                specification.AddEntity(entity);
            }

            // Extract value objects
            var valueObjects = await ExtractValueObjectsAsync(description, cancellationToken);
            foreach (var valueObject in valueObjects)
            {
                specification.AddValueObject(valueObject);
            }

            // Extract business rules
            var businessRules = await ExtractBusinessRulesAsync(description, cancellationToken);
            foreach (var rule in businessRules)
            {
                specification.AddBusinessRule(rule);
            }

            // Extract validation rules
            var validationRules = await ExtractValidationRulesAsync(description, cancellationToken);
            foreach (var rule in validationRules)
            {
                specification.AddValidationRule(rule);
            }

            specification.UpdateStatus(FeatureSpecificationStatus.Analyzing);
            return specification;
        }

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

        private async Task<List<BusinessRule>> ExtractBusinessRulesAsync(object data, CancellationToken cancellationToken)
        {
            var description = data as string ?? throw new ArgumentException("Data must be a string description");

            var prompt = $@"
Analyze the following natural language description and extract business rules.
Return a JSON array of business rules with the following structure:
{{
  ""businessRules"": [
    {{
      ""name"": ""RuleName"",
      ""description"": ""Rule description"",
      ""condition"": ""When condition"",
      ""action"": ""What should happen"",
      ""priority"": ""Medium"",
      ""appliesTo"": ""EntityName""
    }}
  ]
}}

Description: {description}

Focus on:
1. Business constraints and rules
2. Validation requirements
3. Business logic that must be enforced
4. Rules that span multiple entities

Return only valid JSON:";

            var response = await CallAIAsync(prompt, cancellationToken);
            return ParseBusinessRules(response);
        }

        private async Task<List<ValidationRule>> ExtractValidationRulesAsync(object data, CancellationToken cancellationToken)
        {
            var description = data as string ?? throw new ArgumentException("Data must be a string description");

            var prompt = $@"
Analyze the following natural language description and extract validation rules.
Return a JSON array of validation rules with the following structure:
{{
  ""validationRules"": [
    {{
      ""name"": ""RuleName"",
      ""description"": ""Rule description"",
      ""type"": ""Required"",
      ""expression"": ""validation expression"",
      ""errorMessage"": ""Error message"",
      ""severity"": ""Error"",
      ""appliesTo"": ""PropertyName""
    }}
  ]
}}

Description: {description}

Focus on:
1. Field validation requirements
2. Format constraints (email, phone, etc.)
3. Range validations
4. Custom validation rules

Return only valid JSON:";

            var response = await CallAIAsync(prompt, cancellationToken);
            return ParseValidationRules(response);
        }

        private async Task<string> CallAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new Nexo.Feature.AI.Models.ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are a domain analysis expert. Analyze the given description and extract structured domain information. Return only valid JSON without any additional text or explanations.",
                MaxTokens = 4000,
                Temperature = 0.3
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Response;
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

        private List<BusinessRule> ParseBusinessRules(string jsonResponse)
        {
            try
            {
                var document = JsonDocument.Parse(jsonResponse);
                var rules = new List<BusinessRule>();

                if (document.RootElement.TryGetProperty("businessRules", out var rulesArray))
                {
                    foreach (var ruleElement in rulesArray.EnumerateArray())
                    {
                        var rule = ParseBusinessRule(ruleElement);
                        rules.Add(rule);
                    }
                }

                return rules;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse business rules from AI response");
                return new List<BusinessRule>();
            }
        }

        private BusinessRule ParseBusinessRule(JsonElement ruleElement)
        {
            var name = ruleElement.GetProperty("name").GetString() ?? "UnknownRule";
            var description = ruleElement.GetProperty("description").GetString() ?? "No description";
            var condition = ruleElement.GetProperty("condition").GetString() ?? "true";
            var action = ruleElement.GetProperty("action").GetString() ?? "No action";
            var priorityText = ruleElement.TryGetProperty("priority", out var priorityElement) ? priorityElement.GetString() : "Medium";
            var appliesTo = ruleElement.TryGetProperty("appliesTo", out var appliesElement) ? appliesElement.GetString() : null;

            var priority = Enum.TryParse<BusinessRulePriority>(priorityText, true, out var parsedPriority) ? parsedPriority : BusinessRulePriority.Medium;

            return new BusinessRule(name, description, condition, action, priority, appliesTo);
        }

        private List<ValidationRule> ParseValidationRules(string jsonResponse)
        {
            try
            {
                var document = JsonDocument.Parse(jsonResponse);
                var rules = new List<ValidationRule>();

                if (document.RootElement.TryGetProperty("validationRules", out var rulesArray))
                {
                    foreach (var ruleElement in rulesArray.EnumerateArray())
                    {
                        var rule = ParseValidationRule(ruleElement);
                        rules.Add(rule);
                    }
                }

                return rules;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse validation rules from AI response");
                return new List<ValidationRule>();
            }
        }

        private ValidationRule ParseValidationRule(JsonElement ruleElement)
        {
            var name = ruleElement.GetProperty("name").GetString() ?? "UnknownValidationRule";
            var description = ruleElement.GetProperty("description").GetString() ?? "No description";
            var typeText = ruleElement.GetProperty("type").GetString() ?? "Custom";
            var expression = ruleElement.GetProperty("expression").GetString() ?? "true";
            var errorMessage = ruleElement.GetProperty("errorMessage").GetString() ?? "Validation failed";
            var severityText = ruleElement.TryGetProperty("severity", out var severityElement) ? severityElement.GetString() : "Error";
            var appliesTo = ruleElement.TryGetProperty("appliesTo", out var appliesElement) ? appliesElement.GetString() : null;

            var type = Enum.TryParse<ValidationType>(typeText, true, out var parsedType) ? parsedType : ValidationType.Custom;
            var severity = Enum.TryParse<Nexo.Feature.Factory.Domain.Models.ValidationSeverity>(severityText, true, out var parsedSeverity) ? parsedSeverity : Nexo.Feature.Factory.Domain.Models.ValidationSeverity.Error;

            return new ValidationRule(name, description, type, expression, errorMessage, severity, appliesTo);
        }
    }
}
