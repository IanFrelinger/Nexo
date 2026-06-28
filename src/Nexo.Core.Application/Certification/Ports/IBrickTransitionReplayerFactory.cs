using Nexo.Certification.State;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Creates schema-bound <see cref="ITransitionReplayer"/> instances for Tier-2 verification.
/// </summary>
public interface IBrickTransitionReplayerFactory
{
    ITransitionReplayer Create(StateSchema schema);
}
