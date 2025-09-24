using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Interfaces;
using Nexo.Feature.Animation.Models;
using Nexo.Feature.Animation.Providers.Ollama.Generators;

namespace Nexo.Feature.Animation.Providers
{
    /// <summary>
    /// Orchestrator for Ollama animation generation that delegates to specialized generators.
    /// </summary>
    public class OllamaAnimationProvider : IAnimationProvider
    {
        private readonly ILogger<OllamaAnimationProvider> _logger;
        private readonly AnimationDataGenerator _dataGenerator;
        private readonly AnimationFileManager _fileManager;
        private readonly AnimationValidator _validator;
        private readonly AnimationOptimizer _optimizer;
        private bool _isInitialized;

        public string ProviderId => "ollama-animation";
        public string DisplayName => "Ollama Animation Generation";
        public bool RequiresOnline => false;
        public bool IsAvailable => _isInitialized;

        public OllamaAnimationProvider(ILogger<OllamaAnimationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataGenerator = new AnimationDataGenerator(logger);
            _fileManager = new AnimationFileManager(logger);
            _validator = new AnimationValidator(logger);
            _optimizer = new AnimationOptimizer(logger);
        }

        public async Task<AnimationGenerationResult> GenerateAnimationAsync(AnimationGenerationRequest request, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await InitializeAsync(cancellationToken);
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Generating animation with Ollama: {Prompt}", request.Prompt);

                // Generate animation data
                var animationData = await _dataGenerator.GenerateAnimationDataAsync(request, cancellationToken);
                
                // Validate animation
                var validationResult = await _validator.ValidateAnimationAsync(animationData, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return new AnimationGenerationResult
                    {
                        Success = false,
                        Error = $"Animation validation failed: {validationResult.ErrorMessage}"
                    };
                }

                // Optimize animation
                var optimizedData = await _optimizer.OptimizeAnimationAsync(animationData, request, cancellationToken);
                
                // Save animation file
                var filePath = await _fileManager.SaveAnimationAsync(optimizedData, cancellationToken);

                var generationTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new AnimationGenerationResult
                {
                    Success = true,
                    AnimationData = optimizedData,
                    FilePath = filePath,
                    GenerationTime = TimeSpan.FromMilliseconds(generationTime),
                    ProviderId = ProviderId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["prompt"] = request.Prompt,
                        ["style"] = request.Style,
                        ["duration"] = request.Duration,
                        ["fps"] = request.FrameRate
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating animation");
                return new AnimationGenerationResult
                {
                    Success = false,
                    Error = $"Animation generation failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initializing Ollama animation provider");
                
                // Simulate initialization
                await Task.Delay(100, cancellationToken);
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Ollama animation provider");
                return false;
            }
        }
    }
}
