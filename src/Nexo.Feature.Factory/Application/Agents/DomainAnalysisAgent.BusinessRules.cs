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
    /// Business rule extraction and parsing functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
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
    }
}
