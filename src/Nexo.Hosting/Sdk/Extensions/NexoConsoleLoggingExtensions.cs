using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nexo.Hosting.Sdk.Extensions;

/// <summary>
/// Opt-in structured (JSON lines) console logging shared by the shipped hosts (Nexo.API and
/// <c>nexo background-agent daemon</c>). Default output stays the human-readable console formatter;
/// set <c>Nexo:Logging:Json=true</c> (env <c>Nexo__Logging__Json=true</c>) or <c>NEXO_LOG_JSON=1</c>
/// to switch the console provider to <see cref="ConsoleLoggerExtensions.AddJsonConsole(ILoggingBuilder)"/>.
/// </summary>
public static class NexoConsoleLoggingExtensions
{
    /// <summary>Configuration key that turns on JSON console output (<c>true</c>/<c>1</c>).</summary>
    public const string JsonConsoleConfigurationKey = "Nexo:Logging:Json";

    /// <summary>Environment-variable shorthand for <see cref="JsonConsoleConfigurationKey"/> (<c>1</c>/<c>true</c>).</summary>
    public const string JsonConsoleEnvironmentVariable = "NEXO_LOG_JSON";

    /// <summary>
    /// True when <paramref name="configuration"/> requests JSON console logging via
    /// <see cref="JsonConsoleConfigurationKey"/> or <see cref="JsonConsoleEnvironmentVariable"/>
    /// (both are read from the configuration root, so host builders that map environment variables
    /// into configuration honour either spelling; <c>UseSetting</c> works in tests).
    /// </summary>
    /// <param name="configuration">Host configuration root.</param>
    /// <returns>Whether JSON console output was requested.</returns>
    public static bool IsJsonConsoleLoggingRequested(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return IsTruthy(configuration[JsonConsoleConfigurationKey])
               || IsTruthy(configuration[JsonConsoleEnvironmentVariable]);
    }

    /// <summary>
    /// Switches the console logger to the JSON formatter when
    /// <see cref="IsJsonConsoleLoggingRequested"/> is true; otherwise leaves the existing console
    /// configuration untouched. Safe to call after <c>AddConsole()</c>: the console provider is
    /// registered once and only its formatter changes.
    /// </summary>
    /// <param name="logging">Logging builder.</param>
    /// <param name="configuration">Host configuration root.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddNexoJsonConsoleIfRequested(this ILoggingBuilder logging, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(logging);
        if (IsJsonConsoleLoggingRequested(configuration))
        {
            // Log shippers key on a per-line timestamp; the JSON formatter only emits one when a
            // format is set, so use ISO-8601 UTC ("O") rather than the shipper's ingest time.
            logging.AddJsonConsole(options =>
            {
                options.TimestampFormat = "O";
                options.UseUtcTimestamp = true;
            });
        }

        return logging;
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
