using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Validation.Models;
using Ashlar.Core.Application.Validation.Ports;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Infrastructure.Caching;

namespace Ashlar.Infrastructure.Validation.Adapters;

/// <summary>
/// Decorator for IValidationService that adds caching (Decorator pattern - OCP).
/// 
/// Provides caching layer for validation results:
/// - Generates cache keys from filter + test project modification times
/// - Caches results for 15 minutes
/// - Returns cached results when available
/// - Falls through to inner service on cache miss
/// 
/// Implements the Decorator pattern to add caching without modifying the base service.
/// </summary>
public class CachedValidationServiceAdapter : IValidationService
{
    private readonly IValidationService _inner;
    private readonly ICacheStrategy _cache;
    private readonly ILogger<CachedValidationServiceAdapter> _logger;

    /// <summary>Initializes a new cached validation service adapter.</summary>
    public CachedValidationServiceAdapter(
        IValidationService inner,
        ICacheStrategy cache,
        ILogger<CachedValidationServiceAdapter> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Validate asynchronously.</summary>
    public async Task<ValidationResult> ValidateAsync(
        string? filter,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = await GenerateCacheKeyAsync(filter, cancellationToken);

        var cachedResult = await _cache.GetAsync<ValidationResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogInformation("Returning cached validation result for filter: {Filter}", filter ?? "none");
            progress?.Report(new ProgressReport
            {
                Percentage = 100,
                Message = "Returning cached validation result",
                Metadata = new Dictionary<string, object> { ["Cached"] = true }
            });
            return cachedResult;
        }

        _logger.LogInformation("Cache miss, running validation with filter: {Filter}", filter ?? "none");
        var result = await _inner.ValidateAsync(filter, progress, cancellationToken);
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
        var hash = await CacheKeyGenerator.ComputeHashAsync(content, cancellationToken);
        return $"validation:{hash}";
    }
}

