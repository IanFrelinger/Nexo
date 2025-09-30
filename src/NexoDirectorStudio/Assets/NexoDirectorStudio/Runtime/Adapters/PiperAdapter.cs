using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Diagnostics;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Implementation of ITtsAdapter for offline text-to-speech.
    /// Provides audio generation capabilities using Piper TTS.
    /// </summary>
    public sealed class PiperAdapter : ITtsAdapter, IDisposable
    {
        private readonly string _piperPath;
        private readonly string _outputPath;
        private readonly string _modelPath;
        private bool _disposed;
        
        public string Id => "piper";
        public string Name => "Piper TTS Adapter";
        public string Description => "Offline text-to-speech adapter using Piper for game audio generation";
        public bool IsAvailable { get; private set; }
        public DateTime LastHealthCheck { get; private set; }
        
        public PiperAdapter(string piperPath = "piper", string outputPath = "Assets/Generated/Audio", string modelPath = "models")
        {
            _piperPath = piperPath ?? throw new ArgumentNullException(nameof(piperPath));
            _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
        }
        
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check if Piper is available and working
                var result = await RunPiperCommandAsync("--help", cancellationToken);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (result.ExitCode == 0)
                {
                    IsAvailable = true;
                    LastHealthCheck = DateTime.UtcNow;
                    
                    return new HealthCheckResult
                    {
                        IsHealthy = true,
                        Message = "Piper TTS is available",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Piper Path: {_piperPath}, Output Path: {_outputPath}, Model Path: {_modelPath}"
                    };
                }
                else
                {
                    IsAvailable = false;
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Message = $"Piper TTS is not working: {result.Error}",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Piper Path: {_piperPath}, Error: {result.Error}"
                    };
                }
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = $"Piper TTS is not available: {ex.Message}",
                    ResponseTimeMs = (long)responseTime,
                    Details = $"Piper Path: {_piperPath}, Error: {ex.Message}"
                };
            }
        }
        
        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Piper is available
                var result = await RunPiperCommandAsync("--help", cancellationToken);
                
                if (result.ExitCode == 0)
                {
                    // Ensure output directory exists
                    Directory.CreateDirectory(_outputPath);
                    
                    // Check if models are available
                    var modelsAvailable = await CheckModelsAvailableAsync(cancellationToken);
                    
                    return new InitializationResult
                    {
                        IsSuccessful = true,
                        Message = "Piper TTS adapter initialized successfully",
                        Details = $"Piper Path: {_piperPath}, Output Path: {_outputPath}, Models Available: {modelsAvailable}"
                    };
                }
                else
                {
                    return new InitializationResult
                    {
                        IsSuccessful = false,
                        Message = $"Failed to initialize Piper TTS: {result.Error}",
                        Details = $"Piper Path: {_piperPath}, Error: {result.Error}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new InitializationResult
                {
                    IsSuccessful = false,
                    Message = $"Initialization failed: {ex.Message}",
                    Details = $"Piper Path: {_piperPath}, Error: {ex.Message}"
                };
            }
        }
        
        public async Task<AudioClip[]> SynthesizeAsync(string[] lines, string voice, CancellationToken cancellationToken = default)
        {
            if (lines == null || lines.Length == 0)
                throw new ArgumentException("Lines cannot be null or empty", nameof(lines));
            
            if (string.IsNullOrEmpty(voice))
                throw new ArgumentException("Voice cannot be null or empty", nameof(voice));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Piper TTS adapter is not available");
            }
            
            try
            {
                var audioClips = new List<AudioClip>();
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    
                    var audioClip = await SynthesizeLineAsync(line, voice, cancellationToken);
                    audioClips.Add(audioClip);
                }
                
                return audioClips.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to synthesize audio: {ex.Message}", ex);
            }
        }
        
        public async Task<AudioClip> SynthesizeLineAsync(string text, string voice, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            
            if (string.IsNullOrEmpty(voice))
                throw new ArgumentException("Voice cannot be null or empty", nameof(voice));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Piper TTS adapter is not available");
            }
            
            try
            {
                var fileName = $"audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.wav";
                var outputPath = Path.Combine(_outputPath, fileName);
                
                // Create a temporary text file for Piper
                var tempTextFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempTextFile, text, cancellationToken);
                
                try
                {
                    // Run Piper to generate audio
                    var result = await RunPiperCommandAsync($"--model {voice} --output_file {outputPath} {tempTextFile}", cancellationToken);
                    
                    if (result.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Piper synthesis failed: {result.Error}");
                    }
                    
                    // Get file information
                    var fileInfo = new FileInfo(outputPath);
                    var duration = await GetAudioDurationAsync(outputPath, cancellationToken);
                    
                    return new AudioClip
                    {
                        Name = $"Audio_{DateTime.UtcNow:yyyyMMddHHmmss}",
                        Description = $"Generated audio for: {text}",
                        FilePath = outputPath,
                        DurationSeconds = duration,
                        SampleRate = 22050, // Default for Piper
                        Channels = 1,
                        Format = "WAV",
                        FileSizeBytes = fileInfo.Length,
                        Text = text,
                        Voice = voice
                    };
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempTextFile))
                    {
                        File.Delete(tempTextFile);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to synthesize line: {ex.Message}", ex);
            }
        }
        
        public async Task<IReadOnlyList<Voice>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var voices = new List<Voice>();
                
                // Check for available models in the model path
                if (Directory.Exists(_modelPath))
                {
                    var modelFiles = Directory.GetFiles(_modelPath, "*.onnx", SearchOption.AllDirectories);
                    
                    foreach (var modelFile in modelFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(modelFile);
                        var voice = new Voice
                        {
                            Id = fileName,
                            Name = fileName,
                            Description = $"Piper voice model: {fileName}",
                            Language = "en", // Default to English
                            Gender = "neutral",
                            AgeRange = "adult",
                            IsAvailable = true,
                            QualityRating = 4
                        };
                        voices.Add(voice);
                    }
                }
                
                // If no models found, return default voices
                if (voices.Count == 0)
                {
                    voices.Add(new Voice
                    {
                        Id = "default",
                        Name = "Default Voice",
                        Description = "Default Piper voice",
                        Language = "en",
                        Gender = "neutral",
                        AgeRange = "adult",
                        IsAvailable = true,
                        QualityRating = 3
                    });
                }
                
                return voices;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available voices: {ex.Message}", ex);
            }
        }
        
        public async Task<AudioCollection> GenerateGameAudioAsync(DialogueContent dialogue, CancellationToken cancellationToken = default)
        {
            if (dialogue == null)
                throw new ArgumentNullException(nameof(dialogue));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Piper TTS adapter is not available");
            }
            
            try
            {
                var audioClips = new List<AudioClip>();
                
                foreach (var line in dialogue.Lines)
                {
                    if (string.IsNullOrWhiteSpace(line.Text))
                        continue;
                    
                    var audioClip = await SynthesizeLineAsync(line.Text, line.Voice, cancellationToken);
                    audioClips.Add(audioClip);
                }
                
                var totalDuration = audioClips.Sum(ac => ac.DurationSeconds);
                var totalSize = audioClips.Sum(ac => ac.FileSizeBytes);
                
                return new AudioCollection
                {
                    Name = dialogue.Name,
                    Description = dialogue.Description,
                    AudioClips = audioClips,
                    TotalClips = audioClips.Count,
                    TotalDurationSeconds = totalDuration,
                    TotalSizeBytes = totalSize
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate game audio: {ex.Message}", ex);
            }
        }
        
        private async Task<CommandResult> RunPiperCommandAsync(string arguments, CancellationToken cancellationToken)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _piperPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                
                await process.WaitForExitAsync(cancellationToken);
                
                return new CommandResult
                {
                    ExitCode = process.ExitCode,
                    Output = output,
                    Error = error
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    ExitCode = -1,
                    Output = "",
                    Error = ex.Message
                };
            }
        }
        
        private async Task<bool> CheckModelsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(_modelPath))
                    return false;
                
                var modelFiles = Directory.GetFiles(_modelPath, "*.onnx", SearchOption.AllDirectories);
                return modelFiles.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<float> GetAudioDurationAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                // This is a simplified implementation
                // In a real implementation, you would use an audio library to get the actual duration
                var fileInfo = new FileInfo(filePath);
                var estimatedDuration = fileInfo.Length / 1000.0f; // Rough estimate
                return Math.Max(1.0f, estimatedDuration);
            }
            catch
            {
                return 1.0f; // Default duration
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Result of running a command.
    /// </summary>
    internal sealed record CommandResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = "";
        public string Error { get; init; } = "";
    }
}
