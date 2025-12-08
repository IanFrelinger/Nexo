using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Adapts and merges JSON schemas to resolve conflicts.
/// </summary>
public sealed class SchemaAdapter
{
    private readonly ILogger<SchemaAdapter> _logger;

    public SchemaAdapter(ILogger<SchemaAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Merges two JSON schemas into a canonical form.
    /// </summary>
    public JsonElement? MergeSchemas(JsonElement schema1, JsonElement schema2)
    {
        try
        {
            // Parse both schemas
            var doc1 = JsonDocument.Parse(schema1.GetRawText());
            var doc2 = JsonDocument.Parse(schema2.GetRawText());

            var root1 = doc1.RootElement;
            var root2 = doc2.RootElement;

            // Check if both have "type" property
            if (root1.TryGetProperty("type", out var type1) &&
                root2.TryGetProperty("type", out var type2))
            {
                var type1Str = type1.GetString();
                var type2Str = type2.GetString();

                // If types are the same, merge properties
                if (type1Str == type2Str)
                {
                    return MergeSameTypeSchemas(root1, root2, type1Str!);
                }

                // If types are compatible (e.g., number and integer), use the more general one
                if (AreCompatibleTypes(type1Str!, type2Str!))
                {
                    return UseMoreGeneralType(root1, root2, type1Str!, type2Str!);
                }

                // If types are incompatible, try to create a union (oneOf)
                return CreateUnionSchema(root1, root2);
            }

            // If no type specified, try to merge as objects
            return MergeObjectSchemas(root1, root2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to merge schemas");
            return null;
        }
    }

    private JsonElement MergeSameTypeSchemas(JsonElement schema1, JsonElement schema2, string type)
    {
        var merged = new Dictionary<string, object>
        {
            ["type"] = type
        };

        // Merge properties if both are objects
        if (type == "object")
        {
            var props1 = schema1.TryGetProperty("properties", out var p1) ? p1 : default;
            var props2 = schema2.TryGetProperty("properties", out var p2) ? p2 : default;

            if (props1.ValueKind != JsonValueKind.Null && props2.ValueKind != JsonValueKind.Null)
            {
                var mergedProps = new Dictionary<string, object>();
                foreach (var prop in props1.EnumerateObject())
                {
                    mergedProps[prop.Name] = prop.Value;
                }
                foreach (var prop in props2.EnumerateObject())
                {
                    // If property exists in both, merge their schemas
                    if (mergedProps.ContainsKey(prop.Name))
                    {
                        var existing = JsonDocument.Parse(mergedProps[prop.Name].ToString()!).RootElement;
                        var mergedProp = MergeSchemas(existing, prop.Value);
                        if (mergedProp.HasValue)
                        {
                            mergedProps[prop.Name] = JsonDocument.Parse(mergedProp.Value.GetRawText()).RootElement;
                        }
                    }
                    else
                    {
                        mergedProps[prop.Name] = prop.Value;
                    }
                }
                merged["properties"] = mergedProps;
            }
        }

        var json = JsonSerializer.Serialize(merged);
        return JsonDocument.Parse(json).RootElement;
    }

    private bool AreCompatibleTypes(string type1, string type2)
    {
        var compatiblePairs = new[]
        {
            ("integer", "number"),
            ("number", "integer")
        };

        return compatiblePairs.Contains((type1, type2)) || compatiblePairs.Contains((type2, type1));
    }

    private JsonElement UseMoreGeneralType(JsonElement schema1, JsonElement schema2, string type1, string type2)
    {
        // Use "number" over "integer", "object" over others
        var generalType = type1 == "number" || type1 == "object" ? type1 : type2;
        var generalSchema = generalType == type1 ? schema1 : schema2;

        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["type"] = generalType
        });
        return JsonDocument.Parse(json).RootElement;
    }

    private JsonElement CreateUnionSchema(JsonElement schema1, JsonElement schema2)
    {
        // Create a oneOf schema
        var union = new Dictionary<string, object>
        {
            ["oneOf"] = new[] { schema1, schema2 }
        };

        var json = JsonSerializer.Serialize(union);
        return JsonDocument.Parse(json).RootElement;
    }

    private JsonElement MergeObjectSchemas(JsonElement schema1, JsonElement schema2)
    {
        // Default to object type with merged properties
        var merged = new Dictionary<string, object>
        {
            ["type"] = "object"
        };

        var json = JsonSerializer.Serialize(merged);
        return JsonDocument.Parse(json).RootElement;
    }
}

