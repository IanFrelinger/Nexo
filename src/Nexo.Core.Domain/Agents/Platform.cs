namespace Nexo.Core.Domain.Agents;

/// <summary>
/// Platforms supported by agents.
/// </summary>
public enum Platform
{
    /// <summary>Unity game engine integration.</summary>
    Unity,

    /// <summary>Browser or web application host.</summary>
    Web,

    /// <summary>Nexo CLI host.</summary>
    Cli,

    /// <summary>HTTP API host.</summary>
    Api,

    /// <summary>Visual Studio Code extension host.</summary>
    VsCode,

    /// <summary>Slack bot or app integration.</summary>
    Slack,

    /// <summary>Microsoft Teams bot or app integration.</summary>
    Teams,

    /// <summary>Discord bot integration.</summary>
    Discord
}
