namespace Ashlar.Commercial.GameDomain.Discord;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>Discord embed footer record.</summary>
public sealed record DiscordEmbedFooter
{
    /// <summary>Text value.</summary>
    public string Text { get; init; } = string.Empty;
}
