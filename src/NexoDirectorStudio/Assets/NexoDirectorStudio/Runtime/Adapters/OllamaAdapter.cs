using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using System.Text.Json;
using System.Net.Http;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Implementation of IOllamaAdapter for offline LLM integration.
    /// Provides game plan generation and analysis using Ollama.
    /// </summary>
    public sealed class OllamaAdapter : IOllamaAdapter, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;
        private bool _disposed;
        
        public string Id => "ollama";
        public string Name => "Ollama LLM Adapter";
        public string Description => "Offline LLM adapter using Ollama for game plan generation and analysis";
        public bool IsAvailable { get; private set; }
        public DateTime LastHealthCheck { get; private set; }
        
        public OllamaAdapter(string baseUrl = "http://localhost:11434", string model = "llama2")
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }
        
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (response.IsSuccessStatusCode)
                {
                    IsAvailable = true;
                    LastHealthCheck = DateTime.UtcNow;
                    
                    return new HealthCheckResult
                    {
                        IsHealthy = true,
                        Message = "Ollama service is available",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Model: {_model}, Base URL: {_baseUrl}"
                    };
                }
                else
                {
                    IsAvailable = false;
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Message = $"Ollama service returned status {response.StatusCode}",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Base URL: {_baseUrl}"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                IsAvailable = false;
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = $"Ollama service is unreachable: {ex.Message}",
                    ResponseTimeMs = (long)responseTime,
                    Details = $"Base URL: {_baseUrl}, Error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                IsAvailable = false;
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "Health check timed out",
                    ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    Details = $"Base URL: {_baseUrl}"
                };
            }
        }
        
        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the model is available
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var tagsResponse = JsonSerializer.Deserialize<TagsResponse>(content);
                    
                    if (tagsResponse?.Models?.Any(m => m.Name.Contains(_model)) == true)
                    {
                        return new InitializationResult
                        {
                            IsSuccessful = true,
                            Message = $"Ollama adapter initialized successfully with model {_model}",
                            Details = $"Base URL: {_baseUrl}, Model: {_model}"
                        };
                    }
                    else
                    {
                        return new InitializationResult
                        {
                            IsSuccessful = false,
                            Message = $"Model {_model} not found in Ollama",
                            Details = $"Available models: {string.Join(", ", tagsResponse?.Models?.Select(m => m.Name) ?? Array.Empty<string>())}"
                        };
                    }
                }
                else
                {
                    return new InitializationResult
                    {
                        IsSuccessful = false,
                        Message = $"Failed to connect to Ollama: {response.StatusCode}",
                        Details = $"Base URL: {_baseUrl}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new InitializationResult
                {
                    IsSuccessful = false,
                    Message = $"Initialization failed: {ex.Message}",
                    Details = $"Base URL: {_baseUrl}, Error: {ex.Message}"
                };
            }
        }
        
        public async Task<GamePlan> PlanAsync(DesignBrief brief, int seed, string genreId, CancellationToken cancellationToken = default)
        {
            if (brief == null)
                throw new ArgumentNullException(nameof(brief));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available");
            }
            
            try
            {
                var prompt = GeneratePlanningPrompt(brief, genreId);
                var response = await GenerateResponseAsync(prompt, cancellationToken);
                return ParseGamePlanResponse(response, brief, seed);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate game plan: {ex.Message}", ex);
            }
        }
        
        public async Task<GamePlanAnalysis> AnalyzeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default)
        {
            if (gamePlan == null)
                throw new ArgumentNullException(nameof(gamePlan));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available");
            }
            
            try
            {
                var prompt = GenerateAnalysisPrompt(gamePlan);
                var response = await GenerateResponseAsync(prompt, cancellationToken);
                return ParseAnalysisResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze game plan: {ex.Message}", ex);
            }
        }
        
        public async Task<AutoFixSuggestions> SuggestFixesAsync(ValidationReport validationReport, CancellationToken cancellationToken = default)
        {
            if (validationReport == null)
                throw new ArgumentNullException(nameof(validationReport));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available");
            }
            
            try
            {
                var prompt = GenerateFixSuggestionPrompt(validationReport);
                var response = await GenerateResponseAsync(prompt, cancellationToken);
                return ParseFixSuggestionResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to suggest fixes: {ex.Message}", ex);
            }
        }
        
        public async Task<GamePlan> EnhanceNarrativeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default)
        {
            if (gamePlan == null)
                throw new ArgumentNullException(nameof(gamePlan));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available");
            }
            
            try
            {
                var prompt = GenerateNarrativeEnhancementPrompt(gamePlan);
                var response = await GenerateResponseAsync(prompt, cancellationToken);
                return ParseNarrativeEnhancementResponse(response, gamePlan);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enhance narrative: {ex.Message}", ex);
            }
        }
        
        private async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 2048
                }
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var generateResponse = JsonSerializer.Deserialize<GenerateResponse>(responseContent);
            
            return generateResponse?.Response ?? "";
        }
        
        private static string GeneratePlanningPrompt(DesignBrief brief, string genreId)
        {
            return $@"Generate a game plan for the following design brief:

Brief: {brief.Description}
Genre: {genreId}
Duration: {brief.TargetDurationMinutes} minutes
Difficulty: {brief.DifficultyLevel}/5

Please provide a JSON response with the following structure:
{{
  ""id"": ""generated-plan-{DateTime.UtcNow:yyyyMMddHHmmss}"",
  ""genre"": ""{genreId}"",
  ""description"": ""Brief description of the game slice"",
  ""coreMechanics"": [""mechanic1"", ""mechanic2""],
  ""playerExperience"": [""experience1"", ""experience2""],
  ""estimatedDurationMinutes"": {brief.TargetDurationMinutes},
  ""narrativeBeats"": [""beat1"", ""beat2""],
  ""requiredAssets"": [
    {{
      ""assetType"": ""Weapon"",
      ""name"": ""Primary Weapon"",
      ""description"": ""Main weapon for the player"",
      ""isRequired"": true,
      ""priority"": 5
    }}
  ]
}}";
        }
        
        private static string GenerateAnalysisPrompt(GamePlan gamePlan)
        {
            return $@"Analyze the following game plan and provide feedback:

Game Plan:
- Genre: {gamePlan.Genre}
- Description: {gamePlan.Description}
- Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}
- Duration: {gamePlan.EstimatedDurationMinutes} minutes
- Assets: {gamePlan.RequiredAssets.Length} required assets

Please provide a JSON response with the following structure:
{{
  ""qualityScore"": 85,
  ""summary"": ""Overall assessment of the game plan"",
  ""details"": ""Detailed analysis"",
  ""suggestions"": [""suggestion1"", ""suggestion2""],
  ""issues"": [""issue1"", ""issue2""],
  ""strengths"": [""strength1"", ""strength2""]
}}";
        }
        
        private static string GenerateFixSuggestionPrompt(ValidationReport validationReport)
        {
            var issues = string.Join("\n", validationReport.AllIssues.Select(i => $"- {i.Severity}: {i.Title}: {i.Description}"));
            var suggestions = string.Join("\n", validationReport.AllSuggestions.Select(s => $"- {s.Title}: {s.Description}"));
            
            return $@"Based on the following validation report, suggest specific fixes:

Issues:
{issues}

Suggestions:
{suggestions}

Overall Score: {validationReport.OverallScore}/100
Status: {validationReport.Status}

Please provide a JSON response with the following structure:
{{
  ""suggestions"": [
    {{
      ""title"": ""Fix Title"",
      ""description"": ""Description of the fix"",
      ""priority"": 4,
      ""effort"": ""Medium"",
      ""changes"": ""Specific changes to make"",
      ""confidence"": 85
    }}
  ],
  ""confidenceScore"": 80,
  ""summary"": ""Summary of suggested fixes""
}}";
        }
        
        private static string GenerateNarrativeEnhancementPrompt(GamePlan gamePlan)
        {
            return $@"Enhance the narrative elements of the following game plan:

Current Game Plan:
- Genre: {gamePlan.Genre}
- Description: {gamePlan.Description}
- Narrative Beats: {string.Join(", ", gamePlan.NarrativeBeats)}

Please provide enhanced narrative beats and story elements in JSON format:
{{
  ""narrativeBeats"": [""enhanced_beat1"", ""enhanced_beat2"", ""enhanced_beat3""],
  ""storyElements"": [""element1"", ""element2""],
  ""characterArcs"": [""arc1"", ""arc2""]
}}";
        }
        
        private static GamePlan ParseGamePlanResponse(string response, DesignBrief brief, int seed)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response);
                
                return new GamePlan
                {
                    Id = json.GetProperty("id").GetString() ?? $"generated-plan-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    SourceBrief = brief,
                    Genre = json.GetProperty("genre").GetString() ?? brief.GenreHint ?? "Unknown",
                    Description = json.GetProperty("description").GetString() ?? "Generated game plan",
                    CoreMechanics = json.GetProperty("coreMechanics").EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    PlayerExperience = json.GetProperty("playerExperience").EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    EstimatedDurationMinutes = json.GetProperty("estimatedDurationMinutes").GetInt32(),
                    NarrativeBeats = json.GetProperty("narrativeBeats").EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    RequiredAssets = ParseAssets(json.GetProperty("requiredAssets")),
                    Seed = seed
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse game plan response: {ex.Message}", ex);
            }
        }
        
        private static AssetRequirement[] ParseAssets(JsonElement assetsElement)
        {
            var assets = new List<AssetRequirement>();
            
            foreach (var assetElement in assetsElement.EnumerateArray())
            {
                assets.Add(new AssetRequirement
                {
                    AssetType = assetElement.GetProperty("assetType").GetString() ?? "Unknown",
                    Name = assetElement.GetProperty("name").GetString() ?? "Unknown Asset",
                    Description = assetElement.GetProperty("description").GetString() ?? "",
                    IsRequired = assetElement.GetProperty("isRequired").GetBoolean(),
                    Priority = assetElement.GetProperty("priority").GetInt32()
                });
            }
            
            return assets.ToArray();
        }
        
        private static GamePlanAnalysis ParseAnalysisResponse(string response)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response);
                
                return new GamePlanAnalysis
                {
                    QualityScore = json.GetProperty("qualityScore").GetInt32(),
                    Summary = json.GetProperty("summary").GetString() ?? "",
                    Details = json.GetProperty("details").GetString() ?? "",
                    Suggestions = json.GetProperty("suggestions").EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    Issues = json.GetProperty("issues").EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    Strengths = json.GetProperty("strengths").EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse analysis response: {ex.Message}", ex);
            }
        }
        
        private static AutoFixSuggestions ParseFixSuggestionResponse(string response)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response);
                
                var suggestions = new List<AutoFixSuggestion>();
                foreach (var suggestionElement in json.GetProperty("suggestions").EnumerateArray())
                {
                    suggestions.Add(new AutoFixSuggestion
                    {
                        Title = suggestionElement.GetProperty("title").GetString() ?? "",
                        Description = suggestionElement.GetProperty("description").GetString() ?? "",
                        Priority = suggestionElement.GetProperty("priority").GetInt32(),
                        Effort = suggestionElement.GetProperty("effort").GetString() ?? "Medium",
                        Changes = suggestionElement.GetProperty("changes").GetString() ?? "",
                        Confidence = suggestionElement.GetProperty("confidence").GetInt32()
                    });
                }
                
                return new AutoFixSuggestions
                {
                    Suggestions = suggestions,
                    ConfidenceScore = json.GetProperty("confidenceScore").GetInt32(),
                    Summary = json.GetProperty("summary").GetString() ?? ""
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse fix suggestion response: {ex.Message}", ex);
            }
        }
        
        private static GamePlan ParseNarrativeEnhancementResponse(string response, GamePlan originalPlan)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response);
                
                return originalPlan with
                {
                    NarrativeBeats = json.GetProperty("narrativeBeats").EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse narrative enhancement response: {ex.Message}", ex);
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Response from Ollama tags API.
    /// </summary>
    internal sealed record TagsResponse
    {
        public IReadOnlyList<ModelInfo> Models { get; init; } = Array.Empty<ModelInfo>();
    }
    
    /// <summary>
    /// Model information from Ollama.
    /// </summary>
    internal sealed record ModelInfo
    {
        public string Name { get; init; } = "";
        public string Size { get; init; } = "";
        public string ModifiedAt { get; init; } = "";
    }
    
    /// <summary>
    /// Response from Ollama generate API.
    /// </summary>
    internal sealed record GenerateResponse
    {
        public string Response { get; init; } = "";
        public bool Done { get; init; }
        public string Model { get; init; } = "";
    }
}
