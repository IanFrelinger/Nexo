using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Providers.Ollama.Generators
{
    public partial class AnimationFileManager
    {
        private readonly ILogger<AnimationFileManager> _logger;

        public AnimationFileManager(ILogger<AnimationFileManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAnimationAsync(AnimationData animationData, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Saving animation data to file");

                var fileName = $"generated_animation_{Guid.NewGuid():N}.json";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                
                var json = JsonSerializer.Serialize(animationData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
                
                _logger.LogInformation("Animation saved to: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving animation file");
                throw;
            }
        }

        public async Task<AnimationData> LoadAnimationAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading animation from: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Animation file not found: {filePath}");
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var animationData = JsonSerializer.Deserialize<AnimationData>(json);
                
                return animationData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading animation file");
                throw;
            }
        }

        public async Task<bool> DeleteAnimationAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Animation file deleted: {FilePath}", filePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting animation file");
                return false;
            }
        }

        public async Task<string[]> ListAnimationFilesAsync(string directory, CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    return Array.Empty<string>();
                }

                var files = Directory.GetFiles(directory, "generated_animation_*.json");
                _logger.LogInformation("Found {Count} animation files in {Directory}", files.Length, directory);
                
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing animation files");
                return Array.Empty<string>();
            }
        }
    }
}
