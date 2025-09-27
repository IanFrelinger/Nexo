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
    /// Validation rule extraction and parsing functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
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
