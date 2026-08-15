namespace Nexo.Certification.Physical;

/// <summary>
/// Optional manufacturing metadata. Load-bearing for <see cref="BindingScope.Instance"/> and
/// <see cref="BindingScope.Batch"/> scopes per validation policy.
/// </summary>
public sealed record ManufactureMeta(
    string? BatchId,
    string? SerialNumber,
    DateTimeOffset? ManufacturedAt);
