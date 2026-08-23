namespace Ashlar.Core.Application.Environments;

/// <summary>
/// Connection description for a map data backend. <see cref="Kind"/> selects which provider
/// implementation the host registers (e.g. <see cref="MapDataSourceKinds.Http"/> for REST,
/// self-hosted mirrors; <see cref="MapDataSourceKinds.File"/> for local cache).
/// </summary>
/// <param name="Kind">Provider discriminator.</param>
/// <param name="BaseUrl">Optional base URL for HTTP-style providers.</param>
/// <param name="Headers">Optional static headers (API keys for private endpoints).</param>
/// <param name="Options">Provider-specific options (timeouts, path prefixes, bucket names).</param>
public sealed record MapDataSourceBinding(
    string Kind = MapDataSourceKinds.Http,
    string? BaseUrl = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    IReadOnlyDictionary<string, string>? Options = null);
