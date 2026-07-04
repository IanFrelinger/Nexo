namespace Nexo.Contracts;

/// <summary>Static inventory entry describing an ingress seam exposed by the host.</summary>
/// <param name="Kind">Ingress kind label (e.g. <c>http</c>, <c>sms-simulated</c>).</param>
/// <param name="Route">Route or endpoint path handled by this ingress.</param>
/// <param name="Notes">Operator notes describing behavior, auth, or limitations.</param>
public sealed record IngressCatalogEntry(string Kind, string Route, string Notes);
