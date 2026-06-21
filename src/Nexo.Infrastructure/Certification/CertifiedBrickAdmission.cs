using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

public sealed class CertifiedBrickAdmission : ICertifiedBrickAdmission
{
    private readonly ICertificationGate _gate;
    private readonly CertifiedBrickRegistry _registry;

    public CertifiedBrickAdmission(ICertificationGate gate, CertifiedBrickRegistry registry)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<CertificationDecision> CertifyAndAdmitAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var decision = await _gate.CertifyAsync(request, cancellationToken).ConfigureAwait(false);
        if (decision.Admitted)
        {
            if (!_registry.TryAdmit(request.Brick, decision.Record))
            {
                return decision with
                {
                    Admitted = false,
                    FailureCheck = "admission",
                    Record = decision.Record with
                    {
                        Admitted = false,
                        Signed = false,
                        Status = "FAIL",
                        Reason = "Registry rejected admission (signature or record invalid)"
                    }
                };
            }
        }

        return decision;
    }

    public bool IsAdmitted(string brickId) => _registry.GetBrick(brickId) is not null;
}
