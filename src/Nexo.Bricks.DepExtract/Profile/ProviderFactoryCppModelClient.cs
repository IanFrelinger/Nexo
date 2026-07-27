using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Execution;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// <see cref="ICppModelClient"/> over <see cref="IProviderFactory"/> with
/// transient retry (parity with the retired CppParserAdapterBrick).
/// </summary>
public sealed class ProviderFactoryCppModelClient : ICppModelClient
{
    private readonly IProviderFactory _providers;
    private readonly int _maxRetries;
    private readonly string _providerKey;
    private readonly ILogger<ProviderFactoryCppModelClient>? _logger;

    /// <summary>Creates a model client with retry bounds.</summary>
    public ProviderFactoryCppModelClient(
        IProviderFactory providers,
        int maxRetries = 3,
        string providerKey = "ollama",
        ILogger<ProviderFactoryCppModelClient>? logger = null)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _maxRetries = Math.Max(1, maxRetries);
        _providerKey = string.IsNullOrWhiteSpace(providerKey) ? "ollama" : providerKey;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        LLMConfig config,
        CancellationToken cancellationToken = default) =>
        ExecuteWithRetryAsync(
            () => _providers.ExecuteLLMAsync(_providerKey, systemPrompt, userPrompt, config, cancellationToken),
            cancellationToken);

    private async Task<string> ExecuteWithRetryAsync(
        Func<Task<string>> call,
        CancellationToken cancellationToken)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await call().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _maxRetries)
            {
                last = ex;
                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                _logger?.LogWarning(ex,
                    "Transient model failure ({Attempt}/{Max}); retrying in {DelayMs}ms",
                    attempt, _maxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                throw new ModelUnavailableException(
                    $"Local model unavailable after {_maxRetries} attempt(s): {ex.Message}", ex);
            }
        }

        throw last is null
            ? new ModelUnavailableException(
                $"Local model unavailable after {_maxRetries} attempt(s).")
            : new ModelUnavailableException(
                $"Local model unavailable after {_maxRetries} attempt(s): {last.Message}", last);
    }

    private static bool IsTransient(Exception ex) =>
        ex is ModelUnavailableException
            or HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or IOException
        || (ex.InnerException is not null && IsTransient(ex.InnerException))
        || ex.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase);
}
