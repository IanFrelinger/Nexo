namespace Ashlar.Commercial.GameDomain.Discord;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>Discord embed structure for rich messages.</summary>
public sealed record DiscordEmbed
{
    /// <summary>Title value.</summary>
    public string? Title { get; init; }
    /// <summary>Description value.</summary>
    public string? Description { get; init; }
    /// <summary>Color value.</summary>
    public int? Color { get; init; }
    /// <summary>Fields value.</summary>
    public IReadOnlyList<DiscordEmbedField>? Fields { get; init; }
    /// <summary>Footer value.</summary>
    public DiscordEmbedFooter? Footer { get; init; }
    /// <summary>Timestamp value.</summary>
    public string? Timestamp { get; init; }
}
