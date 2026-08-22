using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Assets;

namespace Ashlar.Orchestration.GameDomain.Agents.Assets;

/// <summary>
/// Agent that generates audio assets (music, sound effects, speech).
/// 
/// Specialized agent for audio generation that:
/// - Uses IAudioGenerator to create game audio assets
/// - Supports multiple audio types (music, sound effects, ambient, voice/speech)
/// - Handles prompt generation and refinement for audio
/// - Validates audio constraints (duration, format, sample rate)
/// - Manages asset storage and retrieval
/// - Implements retry logic with prompt refinement
/// 
/// Inherits from BaseAssetAgent for common asset generation functionality.
/// </summary>
public sealed class AudioAssetAgent : BaseAssetAgent
{
    private readonly IAudioGenerator _audioGenerator;

    public override AssetType AssetType => AssetType.Audio;

    public AudioAssetAgent(
        AgentSpawnSpec spec,
        IAudioGenerator audioGenerator,
        IModel? model,
        IAssetStorage assetStorage,
        ILogger<BaseAgent> logger) : base(spec, model, assetStorage, logger)
    {
        _audioGenerator = audioGenerator ?? throw new ArgumentNullException(nameof(audioGenerator));
    }

    protected override async Task<GeneratedAssetBase> GenerateAssetAsync(
        GenerationPrompt prompt,
        CancellationToken cancellationToken)
    {
        // Determine audio type from spec
        var audioType = DetermineAudioType(prompt.Parameters);
        var duration = DetermineDuration(prompt.Parameters);

        GeneratedAudio result;
        
        if (audioType == AudioType.Voice && prompt.Parameters.TryGetValue("text", out var textObj))
        {
            // Generate speech
            var speechRequest = new SpeechGenerationRequest
            {
                Text = textObj.ToString() ?? prompt.TextPrompt,
                VoiceId = prompt.Parameters.TryGetValue("voiceId", out var voiceId) 
                    ? voiceId.ToString() ?? "default" 
                    : "default",
                Speed = prompt.Parameters.TryGetValue("speed", out var speed) 
                    ? Convert.ToDouble(speed) 
                    : 1.0,
                Pitch = prompt.Parameters.TryGetValue("pitch", out var pitch) 
                    ? Convert.ToDouble(pitch) 
                    : 1.0
            };

            result = await _audioGenerator.GenerateSpeechAsync(speechRequest, cancellationToken);
        }
        else
        {
            // Generate audio (music or sound effect)
            var request = new AudioGenerationRequest
            {
                Prompt = prompt.TextPrompt,
                Type = audioType,
                DurationSeconds = duration,
                Genre = prompt.Parameters.TryGetValue("genre", out var genre) ? genre.ToString() : null,
                Bpm = prompt.Parameters.TryGetValue("bpm", out var bpm) ? Convert.ToInt32(bpm) : null
            };

            result = await _audioGenerator.GenerateAsync(request, cancellationToken);
        }

        return new GeneratedAudioAsset
        {
            FilePath = result.FilePath,
            MimeType = result.MimeType,
            DurationMs = result.DurationMs,
            SampleRate = result.SampleRate
        };
    }

    protected override string GetPromptGenerationSystemPrompt()
    {
        return """
            You are an expert at crafting prompts for AI audio generation.
            Given a game asset requirement, generate a detailed prompt that will produce
            high-quality game audio.

            Consider:
            - Audio type (music, sound effect, ambient, voice)
            - Mood and atmosphere
            - Genre and style
            - Technical requirements (duration, format, sample rate)
            - Game context and consistency

            Respond with JSON:
            {
                "prompt": "detailed generation prompt",
                "audioType": "music|soundEffect|ambient|voice",
                "duration": 10,
                "genre": "optional genre",
                "bpm": 120
            }
            """;
    }

    protected override ConstraintResult EvaluateConstraint(
        GeneratedAssetBase asset,
        AgentConstraint constraint)
    {
        var audioAsset = (GeneratedAudioAsset)asset;
        var desc = constraint.Description.ToLowerInvariant();

        // Check duration constraints
        if (desc.Contains("duration") || desc.Contains("length"))
        {
            var match = Regex.Match(desc, @"(\d+)\s*(?:seconds?|s|ms)");
            if (match.Success)
            {
                var reqDuration = int.Parse(match.Groups[1].Value);
                var actualDuration = audioAsset.DurationMs / 1000.0;

                // Allow 10% tolerance
                if (Math.Abs(actualDuration - reqDuration) / reqDuration > 0.1)
                {
                    return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
                }
            }
        }

        // Check format constraints
        if (desc.Contains("wav") && !audioAsset.MimeType.Contains("wav"))
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }
        if (desc.Contains("mp3") && !audioAsset.MimeType.Contains("mp3"))
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }

        // Check sample rate constraints
        if (desc.Contains("sample rate") && audioAsset.SampleRate.HasValue)
        {
            var match = Regex.Match(desc, @"(\d+)\s*(?:hz|khz)");
            if (match.Success)
            {
                var reqSampleRate = int.Parse(match.Groups[1].Value);
                if (audioAsset.SampleRate.Value != reqSampleRate)
                {
                    return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
                }
            }
        }

        return ConstraintResult.Passed;
    }

    protected override string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            _ => "audio/wav"
        };
    }

    private AudioType DetermineAudioType(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("audioType", out var typeObj))
        {
            var typeStr = typeObj.ToString()?.ToLowerInvariant();
            return typeStr switch
            {
                "music" => AudioType.Music,
                "soundeffect" or "sound_effect" => AudioType.SoundEffect,
                "ambient" => AudioType.Ambient,
                "voice" or "speech" => AudioType.Voice,
                _ => AudioType.SoundEffect
            };
        }

        // Infer from prompt text
        return AudioType.SoundEffect;
    }

    private int DetermineDuration(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("duration", out var durationObj))
        {
            return Convert.ToInt32(durationObj);
        }

        return 10; // Default 10 seconds
    }
}
