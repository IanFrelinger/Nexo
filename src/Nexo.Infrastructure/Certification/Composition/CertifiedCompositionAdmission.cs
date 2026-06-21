using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

public sealed class CertifiedCompositionAdmission : ICertifiedCompositionAdmission
{
    private readonly ICompositionCertificationGate _gate;
    private readonly CertifiedCompositionRegistry _registry;

    public CertifiedCompositionAdmission(
        ICompositionCertificationGate gate,
        CertifiedCompositionRegistry registry)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<CompositionCertificationDecision> CertifyAndAdmitAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var decision = await _gate.CertifyAsync(request, cancellationToken).ConfigureAwait(false);
        if (!decision.Admitted)
            return decision;

        if (!_registry.TryAdmit(request.Spec, decision.Record))
        {
            return decision with
            {
                Admitted = false,
                FailureCheck = "admission",
                Record = decision.Record with
                {
                    Admitted = false,
                    Signed = false,
                    Reason = "Registry rejected composition admission (signature or record invalid)"
                }
            };
        }

        return decision;
    }

    public bool IsAdmitted(string compositionId) => _registry.GetComposition(compositionId) is not null;
}
