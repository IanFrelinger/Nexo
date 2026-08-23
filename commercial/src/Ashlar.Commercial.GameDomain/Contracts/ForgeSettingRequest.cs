using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to apply or update a scoped setting.
/// </summary>
/// <param name="SettingId">Identifier of the setting to modify.</param>
/// <param name="Value">New value for the setting.</param>
/// <param name="Scope">Scope at which the setting should be applied.</param>
public sealed record ForgeSettingRequest(string SettingId, object Value, SettingScope Scope);
