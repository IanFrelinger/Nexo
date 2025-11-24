using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Common.Ports;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.Infrastructure.Validation.Adapters;

/// <summary>
/// Decorator for IValidationService that adds caching (Decorator pattern - OCP).
/// </summary>
public class CachedValidationServiceAdapter : IValidationService
{
    private readonly IValidationService _inner;
    private readonly ICacheStrategy _cache;
    private readonly ILogger<CachedValidationServiceAdapter> _logger;

    public CachedValidationServiceAdapter(
        IValidationService inner,
        ICacheStrategy cache,
        ILogger<CachedValidationServiceAdapter> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResult> ValidateAsync(
        string? filter,
        CancellationToken cancellationToken = default)
    {
        // Generate cache key from filter + project hash
        var cacheKey = await GenerateCacheKeyAsync(filter, cancellationToken);

        // Try to get from cache
        var cachedResult = await _cache.GetAsync<ValidationResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogInformation("Returning cached validation result for filter: {Filter}", filter ?? "none");
            return cachedResult;
        }

        // Cache miss, call inner service
        _logger.LogInformation("Cache miss, running validation with filter: {Filter}", filter ?? "none");
        var result = await _inner.ValidateAsync(filter, cancellationToken);

        // Store in cache (15 minute expiration for test results)
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);

        return result;
    }

    private async Task<string> GenerateCacheKeyAsync(string? filter, CancellationToken cancellationToken)
    {
        // Create a hash from filter + test project modification times
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var testProjects = currentDir.GetFiles("*.csproj", SearchOption.AllDirectories)
            .Where(f => f.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
                       f.DirectoryName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(f => f.FullName)
            .Take(50) // Limit to avoid performance issues
            .Select(f => $"{f.FullName}:{f.LastWriteTimeUtc.Ticks}");

        var content = $"{filter ?? "none"}|{string.Join("|", testProjects)}";
        var hash = await ComputeHashAsync(content, cancellationToken);
        return $"validation:{hash}";
    }

    private static Task<string> ComputeHashAsync(string content, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }, cancellationToken);
    }
}

