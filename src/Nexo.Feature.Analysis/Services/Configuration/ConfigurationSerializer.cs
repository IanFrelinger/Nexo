using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.Configuration;

/// <summary>
/// Handles serialization and deserialization of coding standard configurations.
/// </summary>
public class ConfigurationSerializer
{
    private readonly ILogger<ConfigurationSerializer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationSerializer(ILogger<ConfigurationSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<CodingStandardConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading coding standards configuration from file {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await LoadFromJsonAsync(jsonContent, cancellationToken);
    }

    public async Task SaveToFileAsync(CodingStandardConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving coding standards configuration to file {FilePath}", filePath);

        var jsonContent = await ToJsonAsync(configuration, cancellationToken);
        await File.WriteAllTextAsync(filePath, jsonContent, cancellationToken);
    }

    public async Task<CodingStandardConfiguration> LoadFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading coding standards configuration from JSON");

        try
        {
            var configuration = JsonSerializer.Deserialize<CodingStandardConfiguration>(jsonContent, _jsonOptions);
            if (configuration == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration from JSON");
            }

            await Task.CompletedTask;
            return configuration;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing configuration JSON");
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
        }
    }

    public async Task<string> ToJsonAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting coding standards configuration to JSON");

        try
        {
            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            await Task.CompletedTask;
            return json;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error serializing configuration to JSON");
            throw new InvalidOperationException($"Failed to serialize configuration: {ex.Message}", ex);
        }
    }
}
