using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Nexo.Core.Specs
{
    /// <summary>
    /// Represents a feature specification with capabilities, parameters, and policies.
    /// </summary>
    /// <param name="Name">The name of the feature</param>
    /// <param name="Version">The version of the feature</param>
    /// <param name="Capabilities">Array of capabilities this feature provides</param>
    /// <param name="Parameters">Dictionary of configuration parameters</param>
    /// <param name="Policies">Array of policies that apply to this feature</param>
    public record FeatureSpec(
        [property: JsonPropertyName("name"), YamlMember(Alias = "name")] string Name,
        [property: JsonPropertyName("version"), YamlMember(Alias = "version")] string Version,
        [property: JsonPropertyName("capabilities"), YamlMember(Alias = "capabilities")] string[] Capabilities,
        [property: JsonPropertyName("parameters"), YamlMember(Alias = "parameters")] Dictionary<string, string> Parameters,
        [property: JsonPropertyName("policies"), YamlMember(Alias = "policies")] string[] Policies
    );
}
