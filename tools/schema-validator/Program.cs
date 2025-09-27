using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace SchemaValidator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    async options => await ValidateSchema(options),
                    errors => Task.FromResult(1)
                );
        }

        static async Task<int> ValidateSchema(Options options)
        {
            try
            {
                Console.WriteLine($"Validating {options.InputFile} against schema {options.SchemaFile}");

                // Load the JSON schema
                var schemaJson = await File.ReadAllTextAsync(options.SchemaFile);
                var schema = JObject.Parse(schemaJson);

                // Load and parse the YAML file
                var yamlContent = await File.ReadAllTextAsync(options.InputFile);
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(yamlContent);

                // Convert YAML to JSON for validation
                var jsonSerializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();
                var jsonContent = jsonSerializer.Serialize(yamlObject);
                var jsonObject = JObject.Parse(jsonContent);

                // Basic validation - check required fields
                var validationErrors = new List<string>();
                ValidateRequiredFields(schema, jsonObject, "", validationErrors);

                if (validationErrors.Count == 0)
                {
                    Console.WriteLine("✅ Validation successful");
                    return 0;
                }
                else
                {
                    Console.WriteLine("❌ Validation failed:");
                    foreach (var error in validationErrors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during validation: {ex.Message}");
                if (options.Verbose)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return 1;
            }
        }

        static void ValidateRequiredFields(JObject schema, JObject data, string path, List<string> errors)
        {
            var requiredFields = schema["required"] as JArray;
            if (requiredFields == null) return;

            foreach (var requiredField in requiredFields)
            {
                var fieldName = requiredField.ToString();
                var fieldPath = string.IsNullOrEmpty(path) ? fieldName : $"{path}.{fieldName}";
                
                if (!data.ContainsKey(fieldName))
                {
                    errors.Add($"Missing required field: {fieldPath}");
                }
                else
                {
                    var fieldValue = data[fieldName];
                    var fieldSchema = schema["properties"]?[fieldName] as JObject;
                    
                    if (fieldSchema != null && fieldValue != null)
                    {
                        ValidateFieldType(fieldSchema, fieldValue, fieldPath, errors);
                    }
                }
            }
        }

        static void ValidateFieldType(JObject fieldSchema, JToken value, string path, List<string> errors)
        {
            var expectedType = fieldSchema["type"]?.ToString();
            if (string.IsNullOrEmpty(expectedType)) return;

            switch (expectedType)
            {
                case "string":
                    if (value.Type != JTokenType.String)
                    {
                        errors.Add($"Field {path} must be a string");
                    }
                    else
                    {
                        var strValue = value.ToString();
                        var minLength = fieldSchema["minLength"]?.Value<int>();
                        var maxLength = fieldSchema["maxLength"]?.Value<int>();
                        
                        if (minLength.HasValue && strValue.Length < minLength.Value)
                        {
                            errors.Add($"Field {path} must be at least {minLength.Value} characters long");
                        }
                        
                        if (maxLength.HasValue && strValue.Length > maxLength.Value)
                        {
                            errors.Add($"Field {path} must be no more than {maxLength.Value} characters long");
                        }

                        var pattern = fieldSchema["pattern"]?.ToString();
                        if (!string.IsNullOrEmpty(pattern))
                        {
                            var regex = new System.Text.RegularExpressions.Regex(pattern);
                            if (!regex.IsMatch(strValue))
                            {
                                errors.Add($"Field {path} does not match required pattern: {pattern}");
                            }
                        }
                    }
                    break;
                    
                case "array":
                    if (value.Type != JTokenType.Array)
                    {
                        errors.Add($"Field {path} must be an array");
                    }
                    break;
                    
                case "object":
                    if (value.Type != JTokenType.Object)
                    {
                        errors.Add($"Field {path} must be an object");
                    }
                    break;
                    
                case "number":
                    if (value.Type != JTokenType.Float && value.Type != JTokenType.Integer)
                    {
                        errors.Add($"Field {path} must be a number");
                    }
                    break;
                    
                case "boolean":
                    if (value.Type != JTokenType.Boolean)
                    {
                        errors.Add($"Field {path} must be a boolean");
                    }
                    break;
            }
        }
    }

    public partial class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input YAML file to validate")]
        public string InputFile { get; set; } = string.Empty;

        [Option('s', "schema", Required = true, HelpText = "JSON schema file to validate against")]
        public string SchemaFile { get; set; } = string.Empty;

        [Option('o', "output", HelpText = "Output file for validation results (optional)")]
        public string? OutputFile { get; set; }

        [Option('v', "verbose", HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }
}
