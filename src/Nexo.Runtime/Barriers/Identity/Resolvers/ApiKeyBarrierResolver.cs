using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;

namespace Nexo.Runtime.Barriers.Identity.Resolvers;

public sealed class ApiKeyBarrierResolver : IBarrierIdentityResolver
{
    private readonly ApiKeyResolverOptions _options;
    private readonly BarrierHierarchy _hierarchy;
    private readonly ILogger<ApiKeyBarrierResolver> _logger;

    public ApiKeyBarrierResolver(
        ApiKeyResolverOptions options,
        BarrierHierarchy hierarchy,
        ILogger<ApiKeyBarrierResolver> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ResolverName => "ApiKey";

    public ValueTask<BarrierResolutionResult?> TryResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (context is null)
            {
                return default;
            }

            var apiKey = context.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var headerName = _options.HeaderName ?? "x-nexo-api-key";
                if (context.Headers.TryGetValue(headerName, out var headerApiKey))
                {
                    apiKey = headerApiKey;
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
                return default;

            var resolvedKey = apiKey!;
            var computedHashHex = ComputeSha256Hex(resolvedKey);
            var computedBytes = Encoding.UTF8.GetBytes(computedHashHex);

            foreach (var mapping in _options.KeyMapping)
            {
                var configuredBytes = Encoding.UTF8.GetBytes(mapping.Key ?? string.Empty);
                if (!FixedTimeEquals(configuredBytes, computedBytes))
                    continue;

                var level = mapping.Value ?? string.Empty;
                if (!_hierarchy.IsKnown(level))
                {
                    _logger.LogWarning(
                        "API key mapping references unknown barrier level. BarrierLevel={BarrierLevel}",
                        level);
                    return default;
                }

                var prefix = resolvedKey.Length >= 4 ? resolvedKey.Substring(0, 4) : resolvedKey;
                var detail = $"Key prefix '{prefix}****' mapped to '{level}'.";
                var result = new BarrierResolutionResult(
                    ResolvedLevel: level,
                    ResolverName: ResolverName,
                    AuthoritySource: BarrierAuthoritySource.ApiKey,
                    Detail: detail);
                return new ValueTask<BarrierResolutionResult?>(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API key barrier resolution failed.");
        }

        return default;
    }

    private static string ComputeSha256Hex(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    private static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
#if NET8_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(left, right);
#else
        if (left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
#endif
    }
}
