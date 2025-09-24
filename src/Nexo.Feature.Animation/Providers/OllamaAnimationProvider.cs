using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Animation.Interfaces;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Providers;

/// <summary>
/// Offline animation generation provider using Ollama (simulated)
/// </summary>
public class OllamaAnimationProvider : IAnimationProvider
{
    private readonly ILogger<OllamaAnimationProvider> _logger;
    private bool _isInitialized;

    public string ProviderId => "ollama-animation";
    public string DisplayName => "Ollama Animation Generation";
    public bool RequiresOnline => false;
    public bool IsAvailable => _isInitialized;

    public OllamaAnimationProvider(ILogger<OllamaAnimationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Simulate animation generation
            await Task.Delay(800, cancellationToken);

            var animationData = GenerateSimulatedAnimation(request);
            
            // Save to file
            var fileName = $"generated_animation_{Guid.NewGuid():N}.json";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            var json = JsonSerializer.Serialize(animationData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            var generationTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new AnimationGenerationResult
            {
                Success = true,
                AnimationData = animationData,
                FilePath = filePath,
                Format = "JSON",
                Duration = request.Duration,
                FrameRate = request.FrameRate,
                GenerationTimeMs = (long)generationTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate animation: {Prompt}", request.Prompt);
            return new AnimationGenerationResult
            {
                Success = false,
                Error = ex.Message,
                GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<RiggingResult> GenerateRigAsync(RiggingRequest request, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Generating rig with Ollama: {Prompt}", request.Prompt);

            // Simulate rig generation
            await Task.Delay(600, cancellationToken);

            var rigData = GenerateSimulatedRig(request);
            
            // Save to file
            var fileName = $"generated_rig_{Guid.NewGuid():N}.json";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            var json = JsonSerializer.Serialize(rigData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            var generationTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new RiggingResult
            {
                Success = true,
                RigData = rigData,
                FilePath = filePath,
                GenerationTimeMs = (long)generationTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate rig: {Prompt}", request.Prompt);
            return new RiggingResult
            {
                Success = false,
                Error = ex.Message,
                GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<IEnumerable<AnimationGenerationResult>> GenerateAnimationBatchAsync(IEnumerable<AnimationGenerationRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<AnimationGenerationResult>();
        
        foreach (var request in requests)
        {
            var result = await GenerateAnimationAsync(request, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Ollama animation generation provider");
            await Task.Delay(100, cancellationToken); // Simulate initialization
            _isInitialized = true;
            _logger.LogInformation("Ollama animation generation provider initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Ollama animation generation provider");
            throw;
        }
    }

    private AnimationData GenerateSimulatedAnimation(AnimationGenerationRequest request)
    {
        var animationData = new AnimationData
        {
            Name = $"Generated_{request.AnimationType}_{Guid.NewGuid():N}",
            Duration = request.Duration,
            FrameRate = request.FrameRate
        };

        // Generate animation tracks based on animation type
        List<AnimationTrack> tracks;
        switch (request.AnimationType)
        {
            case AnimationType.Walk:
                tracks = GenerateWalkAnimation(request);
                break;
            case AnimationType.Run:
                tracks = GenerateRunAnimation(request);
                break;
            case AnimationType.Idle:
                tracks = GenerateIdleAnimation(request);
                break;
            case AnimationType.Jump:
                tracks = GenerateJumpAnimation(request);
                break;
            case AnimationType.Attack:
                tracks = GenerateAttackAnimation(request);
                break;
            case AnimationType.Death:
                tracks = GenerateDeathAnimation(request);
                break;
            default:
                tracks = GenerateCustomAnimation(request);
                break;
        }

        // Add animation events
        var events = GenerateAnimationEvents(request);

        // Create new AnimationData with the generated data
        animationData = new AnimationData
        {
            Tracks = tracks,
            Events = events
        };

        return animationData;
    }

    private Dictionary<string, AnimationTrack> GenerateWalkAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Root bone (hip) - forward movement
        tracks["Hip"] = new AnimationTrack
        {
            BoneName = "Hip",
            PositionKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, new Vector3(0, 0, 0.5f), 1.0f),
            RotationKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, Quaternion.Identity, 0.1f)
        };

        // Left leg
        tracks["LeftLeg"] = new AnimationTrack
        {
            BoneName = "LeftLeg",
            RotationKeyframes = GenerateLegSwingKeyframes(frameCount, request.FrameRate, 30.0f)
        };

        // Right leg
        tracks["RightLeg"] = new AnimationTrack
        {
            BoneName = "RightLeg",
            RotationKeyframes = GenerateLegSwingKeyframes(frameCount, request.FrameRate, -30.0f, Math.PI)
        };

        // Arms - opposite to legs
        tracks["LeftArm"] = new AnimationTrack
        {
            BoneName = "LeftArm",
            RotationKeyframes = GenerateArmSwingKeyframes(frameCount, request.FrameRate, -20.0f, Math.PI)
        };

        tracks["RightArm"] = new AnimationTrack
        {
            BoneName = "RightArm",
            RotationKeyframes = GenerateArmSwingKeyframes(frameCount, request.FrameRate, 20.0f)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateRunAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Root bone - faster forward movement
        tracks["Hip"] = new AnimationTrack
        {
            BoneName = "Hip",
            PositionKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, new Vector3(0, 0, 1.0f), 2.0f),
            RotationKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, Quaternion.Identity, 0.2f)
        };

        // Legs - faster and more exaggerated
        tracks["LeftLeg"] = new AnimationTrack
        {
            BoneName = "LeftLeg",
            RotationKeyframes = GenerateLegSwingKeyframes(frameCount, request.FrameRate, 45.0f, 0, 2.0f)
        };

        tracks["RightLeg"] = new AnimationTrack
        {
            BoneName = "RightLeg",
            RotationKeyframes = GenerateLegSwingKeyframes(frameCount, request.FrameRate, -45.0f, Math.PI, 2.0f)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateIdleAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Subtle breathing motion
        tracks["Chest"] = new AnimationTrack
        {
            BoneName = "Chest",
            PositionKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, new Vector3(0, 0.02f, 0), 0.5f)
        };

        // Slight head movement
        tracks["Head"] = new AnimationTrack
        {
            BoneName = "Head",
            RotationKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, Quaternion.Identity, 0.05f)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateJumpAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Root bone - jump arc
        tracks["Hip"] = new AnimationTrack
        {
            BoneName = "Hip",
            PositionKeyframes = GenerateJumpArcKeyframes(frameCount, request.FrameRate)
        };

        // Legs - crouch and extend
        tracks["LeftLeg"] = new AnimationTrack
        {
            BoneName = "LeftLeg",
            RotationKeyframes = GenerateJumpLegKeyframes(frameCount, request.FrameRate)
        };

        tracks["RightLeg"] = new AnimationTrack
        {
            BoneName = "RightLeg",
            RotationKeyframes = GenerateJumpLegKeyframes(frameCount, request.FrameRate)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateAttackAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Right arm - attack motion
        tracks["RightArm"] = new AnimationTrack
        {
            BoneName = "RightArm",
            RotationKeyframes = GenerateAttackKeyframes(frameCount, request.FrameRate)
        };

        // Body - slight forward lean
        tracks["Spine"] = new AnimationTrack
        {
            BoneName = "Spine",
            RotationKeyframes = GenerateBodyLeanKeyframes(frameCount, request.FrameRate)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateDeathAnimation(AnimationGenerationRequest request)
    {
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Root bone - fall to ground
        tracks["Hip"] = new AnimationTrack
        {
            BoneName = "Hip",
            PositionKeyframes = GenerateFallKeyframes(frameCount, request.FrameRate),
            RotationKeyframes = GenerateFallRotationKeyframes(frameCount, request.FrameRate)
        };

        return tracks;
    }

    private Dictionary<string, AnimationTrack> GenerateCustomAnimation(AnimationGenerationRequest request)
    {
        // Generate a simple custom animation based on the prompt
        var tracks = new Dictionary<string, AnimationTrack>();
        var frameCount = (int)(request.Duration * request.FrameRate);

        // Generic movement
        tracks["Hip"] = new AnimationTrack
        {
            BoneName = "Hip",
            PositionKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, new Vector3(0, 0.1f, 0), 1.0f),
            RotationKeyframes = GenerateSineWaveKeyframes(frameCount, request.FrameRate, Quaternion.Identity, 0.1f)
        };

        return tracks;
    }

    private List<Keyframe<Vector3>> GenerateSineWaveKeyframes(int frameCount, int frameRate, Vector3 amplitude, float frequency)
    {
        var keyframes = new List<Keyframe<Vector3>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var value = new Vector3(
                amplitude.X * (float)Math.Sin(time * frequency * Math.PI * 2),
                amplitude.Y * (float)Math.Sin(time * frequency * Math.PI * 2),
                amplitude.Z * (float)Math.Sin(time * frequency * Math.PI * 2)
            );
            
            keyframes.Add(new Keyframe<Vector3>
            {
                Time = time,
                Value = value,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateSineWaveKeyframes(int frameCount, int frameRate, Quaternion baseRotation, float amplitude)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var angle = amplitude * (float)Math.Sin(time * Math.PI * 2);
            var rotation = baseRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateLegSwingKeyframes(int frameCount, int frameRate, float maxAngle, double phaseOffset = 0, float frequency = 1.0f)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var angle = maxAngle * (float)Math.Sin((time * frequency * Math.PI * 2) + phaseOffset);
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateArmSwingKeyframes(int frameCount, int frameRate, float maxAngle, double phaseOffset = 0)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var angle = maxAngle * (float)Math.Sin((time * Math.PI * 2) + phaseOffset);
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Vector3>> GenerateJumpArcKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Vector3>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 2.0f; // 2 second jump
            
            float y;
            if (progress < 0.5f)
            {
                // Going up
                y = 4.0f * progress * progress;
            }
            else
            {
                // Coming down
                y = 4.0f * (1.0f - progress) * (1.0f - progress);
            }
            
            keyframes.Add(new Keyframe<Vector3>
            {
                Time = time,
                Value = new Vector3(0, y, 0),
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateJumpLegKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 2.0f;
            
            float angle;
            if (progress < 0.3f)
            {
                // Crouch
                angle = -60.0f * (progress / 0.3f);
            }
            else if (progress < 0.7f)
            {
                // Extend
                angle = -60.0f + 90.0f * ((progress - 0.3f) / 0.4f);
            }
            else
            {
                // Land
                angle = 30.0f - 30.0f * ((progress - 0.7f) / 0.3f);
            }
            
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateAttackKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 1.0f; // 1 second attack
            
            float angle;
            if (progress < 0.2f)
            {
                // Wind up
                angle = -90.0f * (progress / 0.2f);
            }
            else if (progress < 0.4f)
            {
                // Strike
                angle = -90.0f + 180.0f * ((progress - 0.2f) / 0.2f);
            }
            else
            {
                // Return
                angle = 90.0f - 90.0f * ((progress - 0.4f) / 0.6f);
            }
            
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateBodyLeanKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 1.0f;
            
            float angle;
            if (progress < 0.3f)
            {
                // Lean forward
                angle = 15.0f * (progress / 0.3f);
            }
            else if (progress < 0.6f)
            {
                // Hold
                angle = 15.0f;
            }
            else
            {
                // Return
                angle = 15.0f - 15.0f * ((progress - 0.6f) / 0.4f);
            }
            
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Vector3>> GenerateFallKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Vector3>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 3.0f; // 3 second fall
            
            var y = Math.Max(0, 1.8f - 1.8f * progress * progress); // Fall to ground
            
            keyframes.Add(new Keyframe<Vector3>
            {
                Time = time,
                Value = new Vector3(0, y, 0),
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<Keyframe<Quaternion>> GenerateFallRotationKeyframes(int frameCount, int frameRate)
    {
        var keyframes = new List<Keyframe<Quaternion>>();
        
        for (int i = 0; i < frameCount; i++)
        {
            var time = (float)i / frameRate;
            var progress = time / 3.0f;
            
            var angle = 90.0f * progress; // Rotate to lying down
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(angle));
            
            keyframes.Add(new Keyframe<Quaternion>
            {
                Time = time,
                Value = rotation,
                Interpolation = InterpolationType.Linear
            });
        }
        
        return keyframes;
    }

    private List<AnimationEvent> GenerateAnimationEvents(AnimationGenerationRequest request)
    {
        var events = new List<AnimationEvent>();
        
        // Add footstep events for walk/run animations
        if (request.AnimationType == AnimationType.Walk || request.AnimationType == AnimationType.Run)
        {
            var stepInterval = request.AnimationType == AnimationType.Run ? 0.3f : 0.6f;
            for (float time = 0; time < request.Duration; time += stepInterval)
            {
                events.Add(new AnimationEvent
                {
                    Time = time,
                    Name = "Footstep",
                    Parameters = new Dictionary<string, object>
                    {
                        ["foot"] = (time / stepInterval) % 2 == 0 ? "left" : "right",
                        ["volume"] = request.AnimationType == AnimationType.Run ? 0.8f : 0.6f
                    }
                });
            }
        }
        
        // Add attack event for attack animation
        if (request.AnimationType == AnimationType.Attack)
        {
            events.Add(new AnimationEvent
            {
                Time = 0.3f,
                Name = "AttackHit",
                Parameters = new Dictionary<string, object>
                {
                    ["damage"] = 10,
                    ["range"] = 2.0f
                }
            });
        }
        
        return events;
    }

    private RigData GenerateSimulatedRig(RiggingRequest request)
    {
        var rigData = new RigData
        {
            Name = $"Generated_{request.RigType}_{Guid.NewGuid():N}",
            Type = request.RigType
        };

        // Generate bones based on rig type
        List<BoneData> bones;
        List<ConstraintData> constraints;
        switch (request.RigType)
        {
            case RigType.Humanoid:
                bones = GenerateHumanoidBones();
                constraints = GenerateHumanoidConstraints();
                break;
            case RigType.Quadruped:
                bones = GenerateQuadrupedBones();
                constraints = GenerateQuadrupedConstraints();
                break;
            default:
                bones = GenerateCustomBones();
                constraints = GenerateCustomConstraints();
                break;
        }

        // Create new RigData with the generated data
        rigData = new RigData
        {
            Name = $"Generated_{request.RigType}_{Guid.NewGuid():N}",
            Type = request.RigType,
            Bones = bones,
            Constraints = constraints
        };

        return rigData;
    }

    private List<Bone> GenerateHumanoidBones()
    {
        return new List<Bone>
        {
            new() { Name = "Hip", Position = new Vector3(0, 1, 0), Rotation = Quaternion.Identity },
            new() { Name = "Spine", ParentName = "Hip", Position = new Vector3(0, 0.5f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Chest", ParentName = "Spine", Position = new Vector3(0, 0.5f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Neck", ParentName = "Chest", Position = new Vector3(0, 0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Head", ParentName = "Neck", Position = new Vector3(0, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftShoulder", ParentName = "Chest", Position = new Vector3(-0.3f, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftArm", ParentName = "LeftShoulder", Position = new Vector3(-0.3f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftForearm", ParentName = "LeftArm", Position = new Vector3(-0.3f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftHand", ParentName = "LeftForearm", Position = new Vector3(-0.2f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightShoulder", ParentName = "Chest", Position = new Vector3(0.3f, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightArm", ParentName = "RightShoulder", Position = new Vector3(0.3f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightForearm", ParentName = "RightArm", Position = new Vector3(0.3f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightHand", ParentName = "RightForearm", Position = new Vector3(0.2f, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftThigh", ParentName = "Hip", Position = new Vector3(-0.1f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftLeg", ParentName = "LeftThigh", Position = new Vector3(0, -0.4f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftFoot", ParentName = "LeftLeg", Position = new Vector3(0, -0.4f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightThigh", ParentName = "Hip", Position = new Vector3(0.1f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightLeg", ParentName = "RightThigh", Position = new Vector3(0, -0.4f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightFoot", ParentName = "RightLeg", Position = new Vector3(0, -0.4f, 0), Rotation = Quaternion.Identity }
        };
    }

    private List<Bone> GenerateQuadrupedBones()
    {
        return new List<Bone>
        {
            new() { Name = "Root", Position = new Vector3(0, 0.5f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Spine1", ParentName = "Root", Position = new Vector3(0, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Spine2", ParentName = "Spine1", Position = new Vector3(0, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Neck", ParentName = "Spine2", Position = new Vector3(0, 0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "Head", ParentName = "Neck", Position = new Vector3(0, 0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftFrontLeg", ParentName = "Spine1", Position = new Vector3(-0.2f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftFrontKnee", ParentName = "LeftFrontLeg", Position = new Vector3(0, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftFrontFoot", ParentName = "LeftFrontKnee", Position = new Vector3(0, -0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightFrontLeg", ParentName = "Spine1", Position = new Vector3(0.2f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightFrontKnee", ParentName = "RightFrontLeg", Position = new Vector3(0, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightFrontFoot", ParentName = "RightFrontKnee", Position = new Vector3(0, -0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftHindLeg", ParentName = "Root", Position = new Vector3(-0.2f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftHindKnee", ParentName = "LeftHindLeg", Position = new Vector3(0, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "LeftHindFoot", ParentName = "LeftHindKnee", Position = new Vector3(0, -0.2f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightHindLeg", ParentName = "Root", Position = new Vector3(0.2f, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightHindKnee", ParentName = "RightHindLeg", Position = new Vector3(0, -0.3f, 0), Rotation = Quaternion.Identity },
            new() { Name = "RightHindFoot", ParentName = "RightHindKnee", Position = new Vector3(0, -0.2f, 0), Rotation = Quaternion.Identity }
        };
    }

    private List<Bone> GenerateCustomBones()
    {
        return new List<Bone>
        {
            new() { Name = "Root", Position = new Vector3(0, 0, 0), Rotation = Quaternion.Identity },
            new() { Name = "Bone1", ParentName = "Root", Position = new Vector3(0, 1, 0), Rotation = Quaternion.Identity },
            new() { Name = "Bone2", ParentName = "Bone1", Position = new Vector3(0, 1, 0), Rotation = Quaternion.Identity }
        };
    }

    private List<Constraint> GenerateHumanoidConstraints()
    {
        return new List<Constraint>
        {
            new() { Name = "LeftFootIK", Type = ConstraintType.IK, SourceBone = "LeftFoot", TargetBone = "LeftLeg" },
            new() { Name = "RightFootIK", Type = ConstraintType.IK, SourceBone = "RightFoot", TargetBone = "RightLeg" },
            new() { Name = "HeadLookAt", Type = ConstraintType.LookAt, SourceBone = "Head", TargetBone = "Neck" }
        };
    }

    private List<Constraint> GenerateQuadrupedConstraints()
    {
        return new List<Constraint>
        {
            new() { Name = "LeftFrontFootIK", Type = ConstraintType.IK, SourceBone = "LeftFrontFoot", TargetBone = "LeftFrontLeg" },
            new() { Name = "RightFrontFootIK", Type = ConstraintType.IK, SourceBone = "RightFrontFoot", TargetBone = "RightFrontLeg" },
            new() { Name = "LeftHindFootIK", Type = ConstraintType.IK, SourceBone = "LeftHindFoot", TargetBone = "LeftHindLeg" },
            new() { Name = "RightHindFootIK", Type = ConstraintType.IK, SourceBone = "RightHindFoot", TargetBone = "RightHindLeg" }
        };
    }

    private List<Constraint> GenerateCustomConstraints()
    {
        return new List<Constraint>
        {
            new() { Name = "Bone1Constraint", Type = ConstraintType.Position, SourceBone = "Bone1", TargetBone = "Root" }
        };
    }
}

/// <summary>
/// Math helper class
/// </summary>
public static class MathHelper
{
    public static float DegreesToRadians(float degrees)
    {
        return degrees * (float)(Math.PI / 180.0);
    }
}
