using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Registry of compositions admitted only through the composition certification gate.
/// </summary>
public sealed class CertifiedCompositionRegistry
{
    private readonly Dictionary<string, CompositionSpec> _compositions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICompositionCertificationRecordStore _store;
    private readonly CompositionCertificationRecordSigner _signer;
    private readonly ILogger<CertifiedCompositionRegistry>? _logger;

    /// <summary>Initializes a new certified composition registry.</summary>
    public CertifiedCompositionRegistry(
        ICompositionCertificationRecordStore store,
        CompositionCertificationRecordSigner signer,
        ILogger<CertifiedCompositionRegistry>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
    }

    /// <summary>Gets composition.</summary>
    public CompositionSpec? GetComposition(string compositionId)
    {
        if (!_store.IsAdmitted(compositionId))
            return null;
        return _compositions.TryGetValue(compositionId, out var spec) ? spec : null;
    }

    internal bool TryAdmit(CompositionSpec spec, CompositionCertificationRecord record)
    {
        if (!record.Admitted || !record.Signed || !_signer.Verify(record))
        {
            _logger?.LogWarning(
                "Rejected ungated composition admission attempt for {CompositionId}",
                spec.CompositionId);
            return false;
        }

        _store.Save(record);
        _compositions[spec.CompositionId] = spec;
        _logger?.LogInformation("Admitted certified composition {CompositionId}", spec.CompositionId);
        return true;
    }
}
