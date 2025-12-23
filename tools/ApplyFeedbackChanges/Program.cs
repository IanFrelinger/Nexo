using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApplyFeedbackChanges;

/// <summary>
/// Tool to apply DesignChangeRequest objects to a game specification JSON file.
/// This enables automatic iteration based on playtest feedback.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ApplyFeedbackChanges <spec-file> <feedback-file> [output-file]");
            Console.Error.WriteLine("  spec-file: Path to game-specification.json");
            Console.Error.WriteLine("  feedback-file: Path to feedback-synthesis.json (contains DesignChangeRequest array)");
            Console.Error.WriteLine("  output-file: Optional output path (defaults to overwriting spec-file)");
            return 1;
        }

        var specPath = args[0];
        var feedbackPath = args[1];
        var outputPath = args.Length > 2 ? args[2] : specPath;

        try
        {
            // Read game specification
            if (!File.Exists(specPath))
            {
                Console.Error.WriteLine($"Error: Specification file not found: {specPath}");
                return 1;
            }

            var specJson = File.ReadAllText(specPath);
            var spec = JsonNode.Parse(specJson);
            if (spec == null)
            {
                Console.Error.WriteLine($"Error: Invalid JSON in specification file: {specPath}");
                return 1;
            }

            // Read feedback synthesis
            if (!File.Exists(feedbackPath))
            {
                Console.Error.WriteLine($"Error: Feedback file not found: {feedbackPath}");
                return 1;
            }

            var feedbackJson = File.ReadAllText(feedbackPath);
            var feedback = JsonNode.Parse(feedbackJson);
            if (feedback == null)
            {
                Console.Error.WriteLine($"Error: Invalid JSON in feedback file: {feedbackPath}");
                return 1;
            }

            // Extract change requests
            JsonArray? changeRequests = null;
            if (feedback["changeRequests"] is JsonArray arr)
            {
                changeRequests = arr;
            }
            else if (feedback is JsonArray array)
            {
                changeRequests = array;
            }
            else
            {
                Console.Error.WriteLine("Error: Feedback file must contain 'changeRequests' array or be an array of change requests");
                return 1;
            }

            if (changeRequests == null || changeRequests.Count == 0)
            {
                Console.WriteLine("No change requests found in feedback file. Specification unchanged.");
                return 0;
            }

            // Apply each change request
            int applied = 0;
            int skipped = 0;
            foreach (var changeRequestNode in changeRequests)
            {
                if (changeRequestNode == null) continue;

                var changeRequest = changeRequestNode.AsObject();
                var domain = changeRequest["domain"]?.GetValue<string>() ?? "";
                var changeType = changeRequest["changeType"]?.GetValue<string>() ?? "Modify";
                var targetElement = changeRequest["targetElement"]?.GetValue<string>() ?? "";
                var proposedValue = changeRequest["proposedValue"]?.GetValue<string>();

                if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(targetElement) || string.IsNullOrEmpty(proposedValue))
                {
                    Console.WriteLine($"⚠️  Skipping incomplete change request: {targetElement}");
                    skipped++;
                    continue;
                }

                // Apply change based on domain and target element
                if (ApplyChange(spec, domain, changeType, targetElement, proposedValue))
                {
                    applied++;
                    Console.WriteLine($"✅ Applied: {changeType} {domain}.{targetElement} = {proposedValue}");
                }
                else
                {
                    skipped++;
                    Console.WriteLine($"⚠️  Could not apply: {domain}.{targetElement} (path not found)");
                }
            }

            // Write updated specification
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = spec.ToJsonString(options);
            File.WriteAllText(outputPath, updatedJson);

            Console.WriteLine();
            Console.WriteLine($"📊 Summary: {applied} changes applied, {skipped} skipped");
            Console.WriteLine($"💾 Updated specification saved to: {outputPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (ex.StackTrace != null)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    private static bool ApplyChange(JsonNode spec, string domain, string changeType, string targetElement, string proposedValue)
    {
        // Try to find the domain section in the spec
        // Common domains: Combat, Economy, AI, Infrastructure, Security, Gameplay
        var domainLower = domain.ToLowerInvariant();
        var targetLower = targetElement.ToLowerInvariant();

        // Navigate to domain section (could be nested)
        JsonNode? domainNode = null;
        if (spec[domain] != null)
        {
            domainNode = spec[domain];
        }
        else
        {
            // Try case-insensitive search
            foreach (var prop in spec.AsObject())
            {
                if (prop.Key.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    domainNode = prop.Value;
                    break;
                }
            }
        }

        // If domain not found, try to find it in nested structures
        if (domainNode == null)
        {
            domainNode = FindNestedNode(spec, domain);
        }

        // If still not found, create it
        if (domainNode == null)
        {
            if (spec is JsonObject specObj)
            {
                domainNode = new JsonObject();
                specObj[domain] = domainNode;
            }
            else
            {
                return false;
            }
        }

        // Apply the change
        switch (changeType.ToLowerInvariant())
        {
            case "modify":
            case "add":
                if (domainNode is JsonObject domainObj)
                {
                    // Try to parse proposedValue as JSON if it looks like JSON
                    JsonNode? valueNode = null;
                    if (proposedValue.TrimStart().StartsWith("{") || proposedValue.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            valueNode = JsonNode.Parse(proposedValue);
                        }
                        catch
                        {
                            // Fall through to string value
                        }
                    }

                    domainObj[targetElement] = valueNode ?? JsonValue.Create(proposedValue);
                    return true;
                }
                break;

            case "remove":
                if (domainNode is JsonObject domainObj2)
                {
                    domainObj2.Remove(targetElement);
                    return true;
                }
                break;
        }

        return false;
    }

    private static JsonNode? FindNestedNode(JsonNode node, string searchKey)
    {
        if (node is JsonObject obj)
        {
            // Check direct properties
            foreach (var prop in obj)
            {
                if (prop.Key.Equals(searchKey, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value;
                }

                // Recursively search nested objects
                if (prop.Value is JsonObject || prop.Value is JsonArray)
                {
                    var found = FindNestedNode(prop.Value, searchKey);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    var found = FindNestedNode(item, searchKey);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
        }

        return null;
    }
}

