using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.Mcp.Server;

/// <summary>
/// Fails host startup when the MCP surface is enabled but its catalog is invalid (allowlisted
/// tool missing from DI, invalid input schema, sanitized-name collision). Options validation
/// covers scalar settings; the catalog can only be checked once the service provider exists,
/// which is exactly what a hosted service's StartAsync gives us.
/// </summary>
public sealed class McpCatalogStartupValidator : IHostedService
{
    private readonly NexoMcpToolBridge _bridge;
    private readonly IOptions<NexoMcpServerOptions> _options;
    private readonly ILogger<McpCatalogStartupValidator> _logger;

    /// <summary>Creates the validator.</summary>
    public McpCatalogStartupValidator(
        NexoMcpToolBridge bridge,
        IOptions<NexoMcpServerOptions> options,
        ILogger<McpCatalogStartupValidator> logger)
    {
        _bridge = bridge;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogDebug("MCP server disabled; catalog validation skipped.");
            return;
        }

        await _bridge.ValidateCatalogAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
