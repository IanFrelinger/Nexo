using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to create a new Forge session.
/// </summary>
/// <param name="InitialGameMode">Optional initial game rule set.</param>
public sealed record ForgeSessionCreateRequest(Descriptors.GameRuleDescriptor? InitialGameMode);
