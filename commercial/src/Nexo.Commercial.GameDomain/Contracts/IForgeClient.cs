using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Macros;
using Nexo.Commercial.GameDomain.Session;

namespace Nexo.Commercial.GameDomain.Contracts;
/// <summary>
/// Contract for a game-engine client (Unity, Unreal, Godot, or custom) communicating with the Nexo Forge backend.
/// Implementations handle transport (HTTP, gRPC, WebSocket) without exposing
/// framework-specific dependencies.
/// </summary>
public interface IForgeClient
{
    /// <summary>Retrieves the current session state.</summary>
    Task<SessionState> GetSessionAsync(CancellationToken ct);

    /// <summary>Generates a new game descriptor from a natural-language prompt.</summary>
    Task<ForgeGenerateResponse> GenerateAsync(ForgeGenerateRequest request, CancellationToken ct);

    /// <summary>Applies or updates a scoped setting.</summary>
    Task ApplySettingAsync(ForgeSettingRequest request, CancellationToken ct);

    /// <summary>Resolves all effective settings for the specified context.</summary>
    Task<Dictionary<string, object>> ResolveSettingsAsync(ForgeSettingResolveRequest request, CancellationToken ct);

    /// <summary>Lists all macros registered in the current session.</summary>
    Task<IReadOnlyList<MacroDefinition>> ListMacrosAsync(CancellationToken ct);

    /// <summary>Registers a new macro definition in the current session.</summary>
    Task RegisterMacroAsync(MacroDefinition macro, CancellationToken ct);

    /// <summary>Lists all aesthetic packs available in the current session.</summary>
    Task<IReadOnlyList<AestheticPack>> ListAestheticsAsync(CancellationToken ct);

    /// <summary>Applies an aesthetic pack at the specified scope.</summary>
    Task ApplyAestheticAsync(ForgeAestheticApplyRequest request, CancellationToken ct);
}
