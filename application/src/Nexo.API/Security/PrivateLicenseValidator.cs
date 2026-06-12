using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>
/// Loads and validates the on-disk Private license file.
/// </summary>
public sealed class PrivateLicenseValidator : IPrivateLicenseValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly NexoPrivateLicenseOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PrivateLicenseValidator> _logger;
    private readonly object _sync = new();
    private PrivateLicenseStatus? _cached;

    public PrivateLicenseValidator(
        IOptions<NexoPrivateLicenseOptions> optionsAccessor,
        IHostEnvironment environment,
        ILogger<PrivateLicenseValidator> logger)
    {
        _options = optionsAccessor.Value;
        _environment = environment;
        _logger = logger;
    }

    public PrivateLicenseStatus GetStatus()
    {
        lock (_sync)
        {
            return _cached ??= Evaluate();
        }
    }

    private PrivateLicenseStatus Evaluate()
    {
        var path = ResolveLicensePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return new PrivateLicenseStatus
            {
                State = PrivateLicenseState.NotConfigured,
                Detail = "No license file configured.",
            };
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("Private license file not found at {Path}", path);
            return new PrivateLicenseStatus
            {
                State = PrivateLicenseState.Invalid,
                Detail = $"License file not found: {path}",
            };
        }

        PrivateLicenseDocument document;
        try
        {
            var json = File.ReadAllText(path);
            document = JsonSerializer.Deserialize<PrivateLicenseDocument>(json, JsonOptions)
                       ?? throw new InvalidOperationException("License JSON deserialized to null.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse private license file at {Path}", path);
            return new PrivateLicenseStatus
            {
                State = PrivateLicenseState.Invalid,
                Detail = "License file could not be parsed.",
            };
        }

        if (!string.IsNullOrWhiteSpace(_options.HmacSecret))
        {
            if (string.IsNullOrWhiteSpace(document.Signature))
            {
                return Invalid("License signature is required but missing.");
            }

            if (!VerifySignature(document, _options.HmacSecret.Trim()))
            {
                return Invalid("License signature verification failed.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (document.ExpiresAt <= now)
        {
            return new PrivateLicenseStatus
            {
                State = PrivateLicenseState.Expired,
                CustomerId = document.CustomerId,
                TenantId = document.TenantId,
                ExpiresAt = document.ExpiresAt,
                Seats = document.Seats,
                Detail = $"License expired at {document.ExpiresAt:O}.",
            };
        }

        return new PrivateLicenseStatus
        {
            State = PrivateLicenseState.Valid,
            CustomerId = document.CustomerId,
            TenantId = document.TenantId,
            ExpiresAt = document.ExpiresAt,
            Seats = document.Seats,
        };
    }

    private string? ResolveLicensePath()
    {
        var env = Environment.GetEnvironmentVariable("NEXO_LICENSE_FILE");
        if (!string.IsNullOrWhiteSpace(env))
            return Path.IsPathRooted(env.Trim())
                ? env.Trim()
                : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, env.Trim()));

        var configured = _options.LicenseFilePath;
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        return Path.IsPathRooted(configured.Trim())
            ? configured.Trim()
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configured.Trim()));
    }

    private static bool VerifySignature(PrivateLicenseDocument document, string secret)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            customerId = document.CustomerId,
            tenantId = document.TenantId,
            expiresAt = document.ExpiresAt,
            seats = document.Seats,
        }, JsonOptions);

        var key = Encoding.UTF8.GetBytes(secret);
        var payload = Encoding.UTF8.GetBytes(canonical);
        var computed = Convert.ToBase64String(HMACSHA256.HashData(key, payload));
        var provided = document.Signature!.Trim();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(provided));
    }

    private static PrivateLicenseStatus Invalid(string detail) =>
        new()
        {
            State = PrivateLicenseState.Invalid,
            Detail = detail,
        };
}
