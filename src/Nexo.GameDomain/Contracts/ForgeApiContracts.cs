using Nexo.GameDomain.Scoping;

namespace Nexo.GameDomain.Contracts;

/// <summary>
/// Request to generate a new game descriptor via the Forge AI pipeline.
/// </summary>
/// <param name="Prompt">Natural-language description of the desired game element.</param>
/// <param name="Category">Optional category hint (e.g. <c>"weapon"</c>, <c>"ability"</c>, <c>"map_element"</c>).</param>
public sealed record ForgeGenerateRequest(string Prompt, string? Category);

/// <summary>
/// Response from the Forge AI generation pipeline.
/// </summary>
/// <param name="Category">Resolved category of the generated descriptor.</param>
/// <param name="DescriptorJson">JSON-serialised descriptor payload.</param>
/// <param name="Error">Error message if generation failed; <c>null</c> on success.</param>
public sealed record ForgeGenerateResponse(string Category, string DescriptorJson, string? Error);

/// <summary>
/// Request to apply or update a scoped setting.
/// </summary>
/// <param name="SettingId">Identifier of the setting to modify.</param>
/// <param name="Value">New value for the setting.</param>
/// <param name="Scope">Scope at which the setting should be applied.</param>
public sealed record ForgeSettingRequest(string SettingId, object Value, SettingScope Scope);

/// <summary>
/// Request to resolve all effective settings for a given context.
/// </summary>
/// <param name="PlayerId">Optional player identifier.</param>
/// <param name="TeamId">Optional team identifier.</param>
/// <param name="ZoneId">Optional zone identifier.</param>
/// <param name="ObjectId">Optional object identifier.</param>
/// <param name="ActiveMoment">Optional active moment identifier.</param>
public sealed record ForgeSettingResolveRequest(
    string? PlayerId,
    string? TeamId,
    string? ZoneId,
    string? ObjectId,
    string? ActiveMoment);

/// <summary>
/// Request to create a new Forge session.
/// </summary>
/// <param name="InitialGameMode">Optional initial game rule set.</param>
public sealed record ForgeSessionCreateRequest(Descriptors.GameRuleDescriptor? InitialGameMode);

/// <summary>
/// Request to execute a registered macro.
/// </summary>
/// <param name="MacroId">Identifier of the macro to run.</param>
/// <param name="ParameterOverrides">Optional parameter overrides for the macro invocation.</param>
public sealed record ForgeMacroRunRequest(
    string MacroId,
    IReadOnlyDictionary<string, object>? ParameterOverrides);

/// <summary>
/// Request to apply an aesthetic pack at a given scope.
/// </summary>
/// <param name="AestheticId">Identifier of the aesthetic pack to apply.</param>
/// <param name="Scope">Scope at which the aesthetic should take effect.</param>
public sealed record ForgeAestheticApplyRequest(string AestheticId, SettingScope Scope);
