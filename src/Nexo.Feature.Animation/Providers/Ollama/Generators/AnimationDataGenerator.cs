using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Providers.Ollama.Generators
{
    public class AnimationDataGenerator
    {
        private readonly ILogger<AnimationDataGenerator> _logger;

        public AnimationDataGenerator(ILogger<AnimationDataGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AnimationData> GenerateAnimationDataAsync(AnimationGenerationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating animation data for prompt: {Prompt}", request.Prompt);

                // Simulate animation generation
                await Task.Delay(500, cancellationToken);

                var animationData = new AnimationData
                {
                    Id = Guid.NewGuid().ToString(),
                    Prompt = request.Prompt,
                    Style = request.Style,
                    Duration = request.Duration,
                    FrameRate = request.FrameRate,
                    Width = request.Width,
                    Height = request.Height,
                    Frames = GenerateFrames(request),
                    Keyframes = GenerateKeyframes(request),
                    Transitions = GenerateTransitions(request),
                    Effects = GenerateEffects(request),
                    Metadata = new Dictionary<string, object>
                    {
                        ["generated_at"] = DateTime.UtcNow,
                        ["provider"] = "ollama",
                        ["version"] = "1.0"
                    }
                };

                return animationData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating animation data");
                throw;
            }
        }

        private List<AnimationFrame> GenerateFrames(AnimationGenerationRequest request)
        {
            var frames = new List<AnimationFrame>();
            var totalFrames = (int)(request.Duration.TotalSeconds * request.FrameRate);

            for (int i = 0; i < totalFrames; i++)
            {
                var frame = new AnimationFrame
                {
                    Index = i,
                    Timestamp = TimeSpan.FromSeconds(i / request.FrameRate),
                    Content = GenerateFrameContent(request, i, totalFrames),
                    Transform = GenerateFrameTransform(i, totalFrames)
                };
                frames.Add(frame);
            }

            return frames;
        }

        private string GenerateFrameContent(AnimationGenerationRequest request, int frameIndex, int totalFrames)
        {
            // Simulate frame content generation based on prompt
            var progress = (double)frameIndex / totalFrames;
            
            if (request.Prompt.Contains("bounce"))
            {
                return GenerateBounceFrame(progress);
            }
            else if (request.Prompt.Contains("fade"))
            {
                return GenerateFadeFrame(progress);
            }
            else if (request.Prompt.Contains("rotate"))
            {
                return GenerateRotateFrame(progress);
            }
            else
            {
                return GenerateDefaultFrame(progress);
            }
        }

        private string GenerateBounceFrame(double progress)
        {
            var bounce = Math.Sin(progress * Math.PI * 4) * 0.5 + 0.5;
            return $"bounce: {bounce:F2}";
        }

        private string GenerateFadeFrame(double progress)
        {
            var fade = Math.Sin(progress * Math.PI);
            return $"fade: {fade:F2}";
        }

        private string GenerateRotateFrame(double progress)
        {
            var rotation = progress * 360;
            return $"rotate: {rotation:F1}deg";
        }

        private string GenerateDefaultFrame(double progress)
        {
            return $"frame_progress: {progress:F2}";
        }

        private Transform GenerateFrameTransform(int frameIndex, int totalFrames)
        {
            var progress = (double)frameIndex / totalFrames;
            
            return new Transform
            {
                Position = new Vector3(
                    (float)(Math.Sin(progress * Math.PI * 2) * 100),
                    (float)(Math.Cos(progress * Math.PI * 2) * 100),
                    0
                ),
                Rotation = new Vector3(0, 0, (float)(progress * 360)),
                Scale = new Vector3(1.0f + (float)(Math.Sin(progress * Math.PI) * 0.2f))
            };
        }

        private List<AnimationKeyframe> GenerateKeyframes(AnimationGenerationRequest request)
        {
            var keyframes = new List<AnimationKeyframe>();
            var keyframeCount = Math.Max(3, (int)(request.Duration.TotalSeconds / 2));

            for (int i = 0; i < keyframeCount; i++)
            {
                var keyframe = new AnimationKeyframe
                {
                    Time = TimeSpan.FromSeconds(i * request.Duration.TotalSeconds / keyframeCount),
                    Transform = GenerateFrameTransform(i * 10, 100),
                    Properties = new Dictionary<string, object>
                    {
                        ["opacity"] = 1.0f - (i * 0.1f),
                        ["intensity"] = 1.0f + (i * 0.2f)
                    }
                };
                keyframes.Add(keyframe);
            }

            return keyframes;
        }

        private List<AnimationTransition> GenerateTransitions(AnimationGenerationRequest request)
        {
            return new List<AnimationTransition>
            {
                new AnimationTransition
                {
                    Type = "ease-in-out",
                    Duration = TimeSpan.FromMilliseconds(500),
                    Properties = new[] { "opacity", "transform" }
                },
                new AnimationTransition
                {
                    Type = "bounce",
                    Duration = TimeSpan.FromMilliseconds(300),
                    Properties = new[] { "position" }
                }
            };
        }

        private List<AnimationEffect> GenerateEffects(AnimationGenerationRequest request)
        {
            var effects = new List<AnimationEffect>();

            if (request.Prompt.Contains("glow"))
            {
                effects.Add(new AnimationEffect
                {
                    Type = "glow",
                    Intensity = 0.8f,
                    Color = "yellow"
                });
            }

            if (request.Prompt.Contains("shadow"))
            {
                effects.Add(new AnimationEffect
                {
                    Type = "shadow",
                    Intensity = 0.6f,
                    Offset = new Vector3(5, 5, 0)
                });
            }

            return effects;
        }
    }
}
