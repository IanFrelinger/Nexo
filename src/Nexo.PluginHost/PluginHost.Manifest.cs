using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nexo.PluginHost.Schemas;

namespace Nexo.PluginHost
{
    /// <summary>
    /// Plugin manifest loading functionality
    /// </summary>
    public partial class PluginHost
    {
        private async Task<PluginManifest> LoadPluginManifestAsync(string manifestPath, CancellationToken cancellationToken)
        {
            try
            {
                if (!_fileSystem.FileExists(manifestPath))
                {
                    _logger.LogWarning("Plugin manifest not found: {ManifestPath}", manifestPath);
                    return null;
                }

                var manifestJson = await _fileSystem.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);
                
                if (manifest == null || string.IsNullOrEmpty(manifest.Name))
                {
                    _logger.LogError("Invalid plugin manifest: {ManifestPath}", manifestPath);
                    return null;
                }

                return manifest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin manifest: {ManifestPath}", manifestPath);
                return null;
            }
        }
    }
}
