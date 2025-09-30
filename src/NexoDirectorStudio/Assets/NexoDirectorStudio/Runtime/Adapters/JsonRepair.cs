using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Utility for repairing malformed JSON responses from AI adapters.
    /// Attempts to fix common JSON issues and provide detailed error information.
    /// </summary>
    public static class JsonRepair
    {
        /// <summary>
        /// Attempts to repair malformed JSON.
        /// </summary>
        /// <param name="json">The malformed JSON string</param>
        /// <returns>Repair result with fixed JSON or error details</returns>
        public static JsonRepairResult Repair(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JsonRepairResult
                {
                    IsSuccessful = false,
                    OriginalJson = json,
                    RepairedJson = json,
                    ErrorMessage = "JSON string is null or empty",
                    RepairAttempts = 0
                };
            }
            
            var attempts = new List<string>();
            var errors = new List<string>();
            
            // Attempt 1: Try parsing as-is
            attempts.Add(json);
            if (TryParseJson(json, out var error1))
            {
                return new JsonRepairResult
                {
                    IsSuccessful = true,
                    OriginalJson = json,
                    RepairedJson = json,
                    ErrorMessage = "",
                    RepairAttempts = 1
                };
            }
            errors.Add(error1);
            
            // Attempt 2: Fix common issues
            var repaired1 = FixCommonIssues(json);
            attempts.Add(repaired1);
            if (TryParseJson(repaired1, out var error2))
            {
                return new JsonRepairResult
                {
                    IsSuccessful = true,
                    OriginalJson = json,
                    RepairedJson = repaired1,
                    ErrorMessage = "",
                    RepairAttempts = 2
                };
            }
            errors.Add(error2);
            
            // Attempt 3: Extract JSON from text
            var repaired2 = ExtractJsonFromText(json);
            attempts.Add(repaired2);
            if (TryParseJson(repaired2, out var error3))
            {
                return new JsonRepairResult
                {
                    IsSuccessful = true,
                    OriginalJson = json,
                    RepairedJson = repaired2,
                    ErrorMessage = "",
                    RepairAttempts = 3
                };
            }
            errors.Add(error3);
            
            // Attempt 4: Create minimal valid JSON
            var repaired3 = CreateMinimalJson(json);
            attempts.Add(repaired3);
            if (TryParseJson(repaired3, out var error4))
            {
                return new JsonRepairResult
                {
                    IsSuccessful = true,
                    OriginalJson = json,
                    RepairedJson = repaired3,
                    ErrorMessage = "",
                    RepairAttempts = 4
                };
            }
            errors.Add(error4);
            
            return new JsonRepairResult
            {
                IsSuccessful = false,
                OriginalJson = json,
                RepairedJson = repaired3,
                ErrorMessage = $"All repair attempts failed. Errors: {string.Join("; ", errors)}",
                RepairAttempts = 4
            };
        }
        
        private static bool TryParseJson(string json, out string error)
        {
            try
            {
                JsonDocument.Parse(json);
                error = "";
                return true;
            }
            catch (JsonException ex)
            {
                error = ex.Message;
                return false;
            }
        }
        
        private static string FixCommonIssues(string json)
        {
            var repaired = json;
            
            // Fix trailing commas
            repaired = Regex.Replace(repaired, @",(\s*[}\]])", "$1");
            
            // Fix missing quotes around keys
            repaired = Regex.Replace(repaired, @"(\w+):", @"""$1"":");
            
            // Fix single quotes to double quotes
            repaired = repaired.Replace("'", "\"");
            
            // Fix unescaped quotes in strings
            repaired = Regex.Replace(repaired, @"""([^""\\]*(\\.[^""\\]*)*)""", match =>
            {
                var content = match.Groups[1].Value;
                var escaped = content.Replace("\"", "\\\"");
                return $"\"{escaped}\"";
            });
            
            // Fix missing commas between objects
            repaired = Regex.Replace(repaired, @"}\s*{", "},{");
            
            // Fix missing commas between arrays
            repaired = Regex.Replace(repaired, @"]\s*\[", "],[");
            
            return repaired;
        }
        
        private static string ExtractJsonFromText(string text)
        {
            // Look for JSON-like content between curly braces
            var match = Regex.Match(text, @"\{.*\}", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Value;
            }
            
            // Look for JSON-like content between square brackets
            match = Regex.Match(text, @"\[.*\]", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Value;
            }
            
            return text;
        }
        
        private static string CreateMinimalJson(string json)
        {
            // Try to extract key-value pairs and create a minimal valid JSON
            var pairs = new Dictionary<string, object>();
            
            // Extract simple key-value pairs
            var keyValueMatches = Regex.Matches(json, @"""([^""]+)""\s*:\s*""([^""]*)""");
            foreach (Match match in keyValueMatches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                pairs[key] = value;
            }
            
            // Extract numeric values
            var numericMatches = Regex.Matches(json, @"""([^""]+)""\s*:\s*(\d+)");
            foreach (Match match in numericMatches)
            {
                var key = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);
                pairs[key] = value;
            }
            
            // Extract boolean values
            var booleanMatches = Regex.Matches(json, @"""([^""]+)""\s*:\s*(true|false)");
            foreach (Match match in booleanMatches)
            {
                var key = match.Groups[1].Value;
                var value = bool.Parse(match.Groups[2].Value);
                pairs[key] = value;
            }
            
            // Create minimal JSON
            if (pairs.Count > 0)
            {
                return JsonSerializer.Serialize(pairs, new JsonSerializerOptions { WriteIndented = true });
            }
            
            // If no pairs found, create a basic structure
            return JsonSerializer.Serialize(new { error = "Unable to parse JSON", original = json }, new JsonSerializerOptions { WriteIndented = true });
        }
        
        /// <summary>
        /// Validates that a JSON string is well-formed.
        /// </summary>
        /// <param name="json">The JSON string to validate</param>
        /// <returns>Validation result</returns>
        public static JsonValidationResult Validate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JsonValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "JSON string is null or empty",
                    ErrorPosition = 0
                };
            }
            
            try
            {
                using var document = JsonDocument.Parse(json);
                return new JsonValidationResult
                {
                    IsValid = true,
                    ErrorMessage = "",
                    ErrorPosition = 0
                };
            }
            catch (JsonException ex)
            {
                return new JsonValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                    ErrorPosition = ex.BytePositionInLine
                };
            }
        }
        
        /// <summary>
        /// Attempts to repair JSON with a specific schema in mind.
        /// </summary>
        /// <param name="json">The malformed JSON string</param>
        /// <param name="expectedSchema">The expected JSON schema</param>
        /// <returns>Repair result with schema-aware fixes</returns>
        public static JsonRepairResult RepairWithSchema(string json, JsonSchema expectedSchema)
        {
            var basicRepair = Repair(json);
            
            if (basicRepair.IsSuccessful)
            {
                // Try to validate against schema
                var schemaValidation = ValidateAgainstSchema(basicRepair.RepairedJson, expectedSchema);
                
                if (schemaValidation.IsValid)
                {
                    return basicRepair;
                }
                
                // Try to fix schema issues
                var schemaRepaired = FixSchemaIssues(basicRepair.RepairedJson, expectedSchema);
                return new JsonRepairResult
                {
                    IsSuccessful = true,
                    OriginalJson = json,
                    RepairedJson = schemaRepaired,
                    ErrorMessage = "",
                    RepairAttempts = basicRepair.RepairAttempts + 1
                };
            }
            
            return basicRepair;
        }
        
        private static JsonValidationResult ValidateAgainstSchema(string json, JsonSchema schema)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                
                // Basic schema validation
                foreach (var requiredField in schema.RequiredFields)
                {
                    if (!root.TryGetProperty(requiredField, out _))
                    {
                        return new JsonValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Missing required field: {requiredField}",
                            ErrorPosition = 0
                        };
                    }
                }
                
                return new JsonValidationResult
                {
                    IsValid = true,
                    ErrorMessage = "",
                    ErrorPosition = 0
                };
            }
            catch (JsonException ex)
            {
                return new JsonValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                    ErrorPosition = ex.BytePositionInLine
                };
            }
        }
        
        private static string FixSchemaIssues(string json, JsonSchema schema)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var dictionary = new Dictionary<string, object>();
                
                // Copy existing fields
                foreach (var property in root.EnumerateObject())
                {
                    dictionary[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetInt32(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => property.Value.ToString()
                    };
                }
                
                // Add missing required fields with default values
                foreach (var requiredField in schema.RequiredFields)
                {
                    if (!dictionary.ContainsKey(requiredField))
                    {
                        dictionary[requiredField] = schema.GetDefaultValue(requiredField);
                    }
                }
                
                return JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json;
            }
        }
    }
    
    /// <summary>
    /// Result of JSON repair operation.
    /// </summary>
    public sealed record JsonRepairResult
    {
        /// <summary>
        /// Whether the repair was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }
        
        /// <summary>
        /// The original JSON string.
        /// </summary>
        public string OriginalJson { get; init; } = "";
        
        /// <summary>
        /// The repaired JSON string.
        /// </summary>
        public string RepairedJson { get; init; } = "";
        
        /// <summary>
        /// Error message if repair failed.
        /// </summary>
        public string ErrorMessage { get; init; } = "";
        
        /// <summary>
        /// Number of repair attempts made.
        /// </summary>
        public int RepairAttempts { get; init; }
    }
    
    /// <summary>
    /// Result of JSON validation.
    /// </summary>
    public sealed record JsonValidationResult
    {
        /// <summary>
        /// Whether the JSON is valid.
        /// </summary>
        public bool IsValid { get; init; }
        
        /// <summary>
        /// Error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; init; } = "";
        
        /// <summary>
        /// Position of the error in the JSON string.
        /// </summary>
        public long ErrorPosition { get; init; }
    }
    
    /// <summary>
    /// JSON schema definition for validation.
    /// </summary>
    public sealed record JsonSchema
    {
        /// <summary>
        /// Required fields in the JSON.
        /// </summary>
        public IReadOnlyList<string> RequiredFields { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Optional fields in the JSON.
        /// </summary>
        public IReadOnlyList<string> OptionalFields { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Gets the default value for a required field.
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <returns>Default value for the field</returns>
        public object GetDefaultValue(string fieldName)
        {
            return fieldName switch
            {
                "id" => Guid.NewGuid().ToString(),
                "name" => "Unknown",
                "description" => "",
                "score" => 0,
                "isValid" => false,
                "status" => "unknown",
                _ => ""
            };
        }
    }
}
