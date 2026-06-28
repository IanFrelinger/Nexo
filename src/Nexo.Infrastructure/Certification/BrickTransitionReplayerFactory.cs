using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

public sealed class BrickTransitionReplayerFactory : IBrickTransitionReplayerFactory
{
    private readonly CertifiedBrickInstantiator _instantiator;

    public BrickTransitionReplayerFactory(CertifiedBrickInstantiator instantiator)
    {
        _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
    }

    public ITransitionReplayer Create(StateSchema schema) =>
        new BrickTransitionReplayer(schema, _instantiator);
}
