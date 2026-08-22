namespace Ashlar.Contracts;

/// <summary>Catalog of ingress entry points exposed by the current host.</summary>
/// <param name="EntryPoints">Registered ingress seams available to operators and integrators.</param>
public sealed record IngressCatalogResponse(IReadOnlyList<IngressCatalogEntry> EntryPoints);
