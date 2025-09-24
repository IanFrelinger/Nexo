using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Providers.Ollama.Generators
{
    public class AnimationOptimizer
    {
        private readonly ILogger<AnimationOptimizer> _logger;

        public AnimationOptimizer(ILogger<AnimationOptimizer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AnimationData> OptimizeAnimationAsync(AnimationData animationData, AnimationGenerationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Optimizing animation data");

                // Create a copy of the animation data for optimization
                var optimizedData = CloneAnimationData(animationData);

                // Optimize frames
                optimizedData.Frames = await OptimizeFramesAsync(optimizedData.Frames, cancellationToken);

                // Optimize keyframes
                if (optimizedData.Keyframes != null)
                {
                    optimizedData.Keyframes = await OptimizeKeyframesAsync(optimizedData.Keyframes, cancellationToken);
                }

                // Optimize transitions
                if (optimizedData.Transitions != null)
                {
                    optimizedData.Transitions = await OptimizeTransitionsAsync(optimizedData.Transitions, cancellationToken);
                }

                // Optimize effects
                if (optimizedData.Effects != null)
                {
                    optimizedData.Effects = await OptimizeEffectsAsync(optimizedData.Effects, cancellationToken);
                }

                // Add optimization metadata
                optimizedData.Metadata["optimized"] = true;
                optimizedData.Metadata["optimization_time"] = DateTime.UtcNow;

                _logger.LogInformation("Animation optimization completed");
                return optimizedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing animation");
                return animationData; // Return original data if optimization fails
            }
        }

        private AnimationData CloneAnimationData(AnimationData original)
        {
            return new AnimationData
            {
                Id = original.Id,
                Prompt = original.Prompt,
                Style = original.Style,
                Duration = original.Duration,
                FrameRate = original.FrameRate,
                Width = original.Width,
                Height = original.Height,
                Frames = original.Frames?.ToList(),
                Keyframes = original.Keyframes?.ToList(),
                Transitions = original.Transitions?.ToList(),
                Effects = original.Effects?.ToList(),
                Metadata = new Dictionary<string, object>(original.Metadata)
            };
        }

        private async Task<List<AnimationFrame>> OptimizeFramesAsync(List<AnimationFrame> frames, CancellationToken cancellationToken)
        {
            var optimizedFrames = new List<AnimationFrame>();

            foreach (var frame in frames)
            {
                var optimizedFrame = new AnimationFrame
                {
                    Index = frame.Index,
                    Timestamp = frame.Timestamp,
                    Content = OptimizeFrameContent(frame.Content),
                    Transform = OptimizeTransform(frame.Transform)
                };
                optimizedFrames.Add(optimizedFrame);
            }

            return optimizedFrames;
        }

        private string OptimizeFrameContent(string content)
        {
            // Simulate content optimization
            if (content.Contains("bounce"))
            {
                return content.Replace("bounce:", "optimized_bounce:");
            }
            else if (content.Contains("fade"))
            {
                return content.Replace("fade:", "optimized_fade:");
            }
            else if (content.Contains("rotate"))
            {
                return content.Replace("rotate:", "optimized_rotate:");
            }
            
            return content;
        }

        private Transform OptimizeTransform(Transform transform)
        {
            if (transform == null) return null;

            return new Transform
            {
                Position = new Vector3(
                    (float)Math.Round(transform.Position.X, 2),
                    (float)Math.Round(transform.Position.Y, 2),
                    (float)Math.Round(transform.Position.Z, 2)
                ),
                Rotation = new Vector3(
                    (float)Math.Round(transform.Rotation.X, 2),
                    (float)Math.Round(transform.Rotation.Y, 2),
                    (float)Math.Round(transform.Rotation.Z, 2)
                ),
                Scale = new Vector3(
                    (float)Math.Round(transform.Scale.X, 2),
                    (float)Math.Round(transform.Scale.Y, 2),
                    (float)Math.Round(transform.Scale.Z, 2)
                )
            };
        }

        private async Task<List<AnimationKeyframe>> OptimizeKeyframesAsync(List<AnimationKeyframe> keyframes, CancellationToken cancellationToken)
        {
            var optimizedKeyframes = new List<AnimationKeyframe>();

            foreach (var keyframe in keyframes)
            {
                var optimizedKeyframe = new AnimationKeyframe
                {
                    Time = keyframe.Time,
                    Transform = OptimizeTransform(keyframe.Transform),
                    Properties = OptimizeProperties(keyframe.Properties)
                };
                optimizedKeyframes.Add(optimizedKeyframe);
            }

            return optimizedKeyframes;
        }

        private Dictionary<string, object> OptimizeProperties(Dictionary<string, object> properties)
        {
            if (properties == null) return null;

            var optimizedProperties = new Dictionary<string, object>();

            foreach (var kvp in properties)
            {
                if (kvp.Value is float floatValue)
                {
                    optimizedProperties[kvp.Key] = (float)Math.Round(floatValue, 3);
                }
                else if (kvp.Value is double doubleValue)
                {
                    optimizedProperties[kvp.Key] = Math.Round(doubleValue, 3);
                }
                else
                {
                    optimizedProperties[kvp.Key] = kvp.Value;
                }
            }

            return optimizedProperties;
        }

        private async Task<List<AnimationTransition>> OptimizeTransitionsAsync(List<AnimationTransition> transitions, CancellationToken cancellationToken)
        {
            var optimizedTransitions = new List<AnimationTransition>();

            foreach (var transition in transitions)
            {
                var optimizedTransition = new AnimationTransition
                {
                    Type = OptimizeTransitionType(transition.Type),
                    Duration = OptimizeDuration(transition.Duration),
                    Properties = transition.Properties
                };
                optimizedTransitions.Add(optimizedTransition);
            }

            return optimizedTransitions;
        }

        private string OptimizeTransitionType(string type)
        {
            // Optimize transition types
            return type switch
            {
                "ease-in-out" => "cubic-bezier(0.4, 0, 0.2, 1)",
                "ease-in" => "cubic-bezier(0.4, 0, 1, 1)",
                "ease-out" => "cubic-bezier(0, 0, 0.2, 1)",
                "linear" => "linear",
                _ => type
            };
        }

        private TimeSpan OptimizeDuration(TimeSpan duration)
        {
            // Round to nearest 10ms for optimization
            var milliseconds = (int)Math.Round(duration.TotalMilliseconds / 10.0) * 10;
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        private async Task<List<AnimationEffect>> OptimizeEffectsAsync(List<AnimationEffect> effects, CancellationToken cancellationToken)
        {
            var optimizedEffects = new List<AnimationEffect>();

            foreach (var effect in effects)
            {
                var optimizedEffect = new AnimationEffect
                {
                    Type = effect.Type,
                    Intensity = (float)Math.Round(effect.Intensity, 2),
                    Color = effect.Color,
                    Offset = effect.Offset != null ? new Vector3(
                        (float)Math.Round(effect.Offset.X, 2),
                        (float)Math.Round(effect.Offset.Y, 2),
                        (float)Math.Round(effect.Offset.Z, 2)
                    ) : null
                };
                optimizedEffects.Add(optimizedEffect);
            }

            return optimizedEffects;
        }
    }
}
