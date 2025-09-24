using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Providers.Ollama.Generators
{
    public class AnimationValidator
    {
        private readonly ILogger<AnimationValidator> _logger;

        public AnimationValidator(ILogger<AnimationValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ValidationResult> ValidateAnimationAsync(AnimationData animationData, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Validating animation data");

                var errors = new List<string>();

                // Validate basic properties
                if (string.IsNullOrEmpty(animationData.Id))
                {
                    errors.Add("Animation ID is required");
                }

                if (string.IsNullOrEmpty(animationData.Prompt))
                {
                    errors.Add("Animation prompt is required");
                }

                if (animationData.Duration <= TimeSpan.Zero)
                {
                    errors.Add("Animation duration must be positive");
                }

                if (animationData.FrameRate <= 0)
                {
                    errors.Add("Animation frame rate must be positive");
                }

                if (animationData.Width <= 0 || animationData.Height <= 0)
                {
                    errors.Add("Animation dimensions must be positive");
                }

                // Validate frames
                if (animationData.Frames == null || !animationData.Frames.Any())
                {
                    errors.Add("Animation must have at least one frame");
                }
                else
                {
                    var frameErrors = ValidateFrames(animationData.Frames);
                    errors.AddRange(frameErrors);
                }

                // Validate keyframes
                if (animationData.Keyframes != null)
                {
                    var keyframeErrors = ValidateKeyframes(animationData.Keyframes);
                    errors.AddRange(keyframeErrors);
                }

                // Validate transitions
                if (animationData.Transitions != null)
                {
                    var transitionErrors = ValidateTransitions(animationData.Transitions);
                    errors.AddRange(transitionErrors);
                }

                // Validate effects
                if (animationData.Effects != null)
                {
                    var effectErrors = ValidateEffects(animationData.Effects);
                    errors.AddRange(effectErrors);
                }

                var result = new ValidationResult
                {
                    IsValid = !errors.Any(),
                    Errors = errors,
                    ErrorMessage = errors.Any() ? string.Join("; ", errors) : null
                };

                if (result.IsValid)
                {
                    _logger.LogInformation("Animation validation passed");
                }
                else
                {
                    _logger.LogWarning("Animation validation failed: {Errors}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating animation");
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}"
                };
            }
        }

        private List<string> ValidateFrames(List<AnimationFrame> frames)
        {
            var errors = new List<string>();

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                
                if (frame.Index != i)
                {
                    errors.Add($"Frame {i} has incorrect index: {frame.Index}");
                }

                if (frame.Timestamp < TimeSpan.Zero)
                {
                    errors.Add($"Frame {i} has negative timestamp: {frame.Timestamp}");
                }

                if (string.IsNullOrEmpty(frame.Content))
                {
                    errors.Add($"Frame {i} has empty content");
                }

                if (frame.Transform == null)
                {
                    errors.Add($"Frame {i} has null transform");
                }
            }

            return errors;
        }

        private List<string> ValidateKeyframes(List<AnimationKeyframe> keyframes)
        {
            var errors = new List<string>();

            for (int i = 0; i < keyframes.Count; i++)
            {
                var keyframe = keyframes[i];
                
                if (keyframe.Time < TimeSpan.Zero)
                {
                    errors.Add($"Keyframe {i} has negative time: {keyframe.Time}");
                }

                if (keyframe.Transform == null)
                {
                    errors.Add($"Keyframe {i} has null transform");
                }

                if (keyframe.Properties == null || !keyframe.Properties.Any())
                {
                    errors.Add($"Keyframe {i} has no properties");
                }
            }

            return errors;
        }

        private List<string> ValidateTransitions(List<AnimationTransition> transitions)
        {
            var errors = new List<string>();

            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                
                if (string.IsNullOrEmpty(transition.Type))
                {
                    errors.Add($"Transition {i} has empty type");
                }

                if (transition.Duration <= TimeSpan.Zero)
                {
                    errors.Add($"Transition {i} has non-positive duration: {transition.Duration}");
                }

                if (transition.Properties == null || !transition.Properties.Any())
                {
                    errors.Add($"Transition {i} has no properties");
                }
            }

            return errors;
        }

        private List<string> ValidateEffects(List<AnimationEffect> effects)
        {
            var errors = new List<string>();

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                
                if (string.IsNullOrEmpty(effect.Type))
                {
                    errors.Add($"Effect {i} has empty type");
                }

                if (effect.Intensity < 0 || effect.Intensity > 1)
                {
                    errors.Add($"Effect {i} has invalid intensity: {effect.Intensity}");
                }
            }

            return errors;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public string ErrorMessage { get; set; }
    }
}
