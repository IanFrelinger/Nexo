using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function app configuration
/// </summary>
public record FunctionAppConfiguration
{
    public Dictionary<string, string> AppSettings { get; init; } = new();
    public Dictionary<string, string> ConnectionStrings { get; init; } = new();
    public Dictionary<string, string> SiteConfig { get; init; } = new();
    public List<string> EnabledFunctions { get; init; } = new();
    public Dictionary<string, object> FunctionConfigurations { get; init; } = new();
} 