using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Scoping;

namespace Nexo.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to create a new Forge session.
/// </summary>
/// <param name="InitialGameMode">Optional initial game rule set.</param>
public sealed record ForgeSessionCreateRequest(Descriptors.GameRuleDescriptor? InitialGameMode);
