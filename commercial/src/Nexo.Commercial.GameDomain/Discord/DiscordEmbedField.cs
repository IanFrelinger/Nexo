namespace Nexo.Commercial.GameDomain.Discord;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>Discord embed field record.</summary>
public sealed record DiscordEmbedField
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Value value.</summary>
    public string Value { get; init; } = string.Empty;
    /// <summary>Inline value.</summary>
    public bool Inline { get; init; }
}
