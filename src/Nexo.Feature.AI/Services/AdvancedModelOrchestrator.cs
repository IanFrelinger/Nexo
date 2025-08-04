using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Advanced model orchestrator with intelligent model selection, fallback strategies,
    /// and multi-model coordination capabilities.
    /// </summary>
    public class AdvancedModelOrchestrator : IAdvancedModelOrchestrator
    {
        private readonly ILogger<AdvancedModelOrchestrator> _logger;
        private readonly IModelOrchestrator _baseOrchestrator;
        private readonly Dictionary<string, ModelPerformanceMetrics> _modelMetrics;
        private readonly Dictionary<string, ModelCapabilityProfile> _modelCapabilities;
        private readonly List<ModelSelectionRule> _selectionRules;
        private readonly object _metricsLock = new object();

        public AdvancedModelOrchestrator(
            IModelOrchestrator baseOrchestrator,
            ILogger<AdvancedModelOrchestrator> logger)
        {
            _baseOrchestrator = baseOrchestrator ?? throw new ArgumentNullException(nameof(baseOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _modelMetrics = new Dictionary<string, ModelPerformanceMetrics>();
            _modelCapabilities = new Dictionary<string, ModelCapabilityProfile>();
            _selectionRules = new List<ModelSelectionRule>();
            
            InitializeDefaultCapabilities();
            InitializeDefaultSelectionRules();
        }

        /// <summary>
        /// Executes a request with intelligent model selection and fallback strategies.
        /// </summary>
        public async Task<AdvancedModelResponse> ExecuteAdvancedAsync(AdvancedModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing advanced model request with intelligent selection");

            try
            {
                // Select the best model based on request requirements
                var selectedModel = await SelectOptimalModelAsync(request, cancellationToken);
                _logger.LogDebug("Selected model: {SelectedModel}", selectedModel);
                
                // Execute with the selected model
                var response = await ExecuteWithModelAsync(request, selectedModel, cancellationToken);
                _logger.LogDebug("ExecuteWithModelAsync completed, Success: {Success}", response.Success);
                
                // Update performance metrics
                UpdateModelMetrics(selectedModel, response);
                
                // Apply post-processing if needed
                var finalResponse = await ApplyPostProcessingAsync(response, request, cancellationToken);
                _logger.LogDebug("ApplyPostProcessingAsync completed, Success: {Success}", finalResponse.Success);
                
                _logger.LogDebug("Final response - Success: {Success}, ModelUsed: {ModelUsed}, Content: {Content}", 
                    finalResponse.Success, finalResponse.ModelUsed, finalResponse.Content?.Substring(0, Math.Min(50, finalResponse.Content?.Length ?? 0)));
                
                return finalResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced model execution: {ErrorMessage}", ex.Message);
                return new AdvancedModelResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FallbackUsed = true
                };
            }
        }

        /// <summary>
        /// Executes a multi-model workflow with coordination between different models.
        /// </summary>
        public async Task<MultiModelResponse> ExecuteMultiModelWorkflowAsync(MultiModelWorkflow workflow, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing multi-model workflow with {StepCount} steps", workflow.Steps.Count);

            var startTime = DateTime.UtcNow;
            var results = new List<WorkflowStepResult>();
            var context = new WorkflowContext();

            try
            {
                foreach (var step in workflow.Steps)
                {
                    _logger.LogDebug("Executing workflow step: {StepName}", step.Name);
                    
                    var stepResult = await ExecuteWorkflowStepAsync(step, context, cancellationToken);
                    results.Add(stepResult);
                    
                    // Update context with step results
                    context.StepResults[step.Name] = stepResult;
                    
                    // Check if we should continue based on step conditions
                    if (!stepResult.Success && step.IsCritical)
                    {
                        _logger.LogWarning("Critical workflow step failed: {StepName}", step.Name);
                        break;
                    }
                }

                var totalProcessingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var totalCost = results.Sum(r => r.Cost);

                return new MultiModelResponse
                {
                    Success = results.All(r => r.Success || !workflow.Steps.First(s => s.Name == r.StepName).IsCritical),
                    StepResults = results,
                    WorkflowContext = context,
                    TotalProcessingTimeMs = totalProcessingTime,
                    TotalCost = totalCost
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in multi-model workflow execution");
                var totalProcessingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return new MultiModelResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    StepResults = results,
                    TotalProcessingTimeMs = totalProcessingTime,
                    TotalCost = results.Sum(r => r.Cost)
                };
            }
        }

        /// <summary>
        /// Analyzes and optimizes model performance based on historical data.
        /// </summary>
        public async Task<ModelOptimizationResult> AnalyzeAndOptimizeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing model performance and generating optimization recommendations");

            try
            {
                var analysis = new ModelOptimizationResult
                {
                    Success = true
                };
                
                // Analyze performance patterns
                analysis.PerformanceAnalysis = AnalyzePerformancePatterns();
                
                // Identify bottlenecks
                analysis.Bottlenecks = IdentifyBottlenecks();
                
                // Generate optimization recommendations
                analysis.Recommendations = GenerateOptimizationRecommendations(analysis.PerformanceAnalysis, analysis.Bottlenecks);
                
                // Update selection rules based on analysis
                await UpdateSelectionRulesAsync(analysis, cancellationToken);
                
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in model analysis and optimization");
                return new ModelOptimizationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Adds a custom model selection rule.
        /// </summary>
        public void AddSelectionRule(ModelSelectionRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            
            lock (_metricsLock)
            {
                _selectionRules.Add(rule);
            }
            
            _logger.LogInformation("Added custom model selection rule: {RuleName}", rule.Name);
        }

        /// <summary>
        /// Updates model capabilities profile.
        /// </summary>
        public void UpdateModelCapabilities(string modelName, ModelCapabilityProfile capabilities)
        {
            if (string.IsNullOrEmpty(modelName)) throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            
            lock (_metricsLock)
            {
                _modelCapabilities[modelName] = capabilities;
            }
            
            _logger.LogInformation("Updated capabilities for model: {ModelName}", modelName);
        }

        private async Task<string> SelectOptimalModelAsync(AdvancedModelRequest request, CancellationToken cancellationToken)
        {
            var candidates = new List<ModelCandidate>();

            // Evaluate each available model
            foreach (var model in _modelCapabilities.Keys)
            {
                var score = await EvaluateModelForRequestAsync(model, request, cancellationToken);
                candidates.Add(new ModelCandidate { ModelName = model, Score = score });
            }

            // Check if we have any candidates
            if (!candidates.Any())
            {
                _logger.LogWarning("No models available for selection");
                return "gpt-4"; // Fallback to default model
            }

            // Sort by score and return the best model
            var bestModel = candidates.OrderByDescending(c => c.Score).First();
            _logger.LogDebug("Selected model {ModelName} with score {Score}", bestModel.ModelName, bestModel.Score);
            
            return bestModel.ModelName;
        }

        private async Task<double> EvaluateModelForRequestAsync(string modelName, AdvancedModelRequest request, CancellationToken cancellationToken)
        {
            if (!_modelCapabilities.ContainsKey(modelName))
            {
                _logger.LogWarning("Model {ModelName} not found in capabilities, skipping evaluation", modelName);
                return 0.0;
            }
            
            var capabilities = _modelCapabilities[modelName];
            var metrics = _modelMetrics.ContainsKey(modelName) ? _modelMetrics[modelName] : new ModelPerformanceMetrics();
            
            double score = 0.0;

            // Capability matching (40% weight)
            score += EvaluateCapabilityMatch(capabilities, request) * 0.4;
            
            // Performance metrics (30% weight)
            score += EvaluatePerformanceMetrics(metrics) * 0.3;
            
            // Cost efficiency (20% weight)
            score += EvaluateCostEfficiency(metrics, request) * 0.2;
            
            // Availability and reliability (10% weight)
            score += EvaluateReliability(metrics) * 0.1;

            return score;
        }

        private double EvaluateCapabilityMatch(ModelCapabilityProfile capabilities, AdvancedModelRequest request)
        {
            double matchScore = 0.0;
            int totalCapabilities = 0;

            // Check language support
            if (request.RequiredLanguages?.Any(lang => capabilities.SupportedLanguages.Contains(lang)) == true)
            {
                matchScore += 1.0;
            }
            totalCapabilities++;

            // Check task type support
            if (!string.IsNullOrEmpty(request.TaskType) && capabilities.SupportedTasks.Contains(request.TaskType))
            {
                matchScore += 1.0;
            }
            totalCapabilities++;

            // Check complexity support
            if (capabilities.MaxComplexity >= request.ComplexityLevel)
            {
                matchScore += 1.0;
            }
            totalCapabilities++;

            return totalCapabilities > 0 ? matchScore / totalCapabilities : 0.0;
        }

        private double EvaluatePerformanceMetrics(ModelPerformanceMetrics metrics)
        {
            if (metrics.TotalRequests == 0) return 0.5; // Default score for new models

            // Calculate performance score based on response time and success rate
            var responseTimeScore = Math.Max(0, 1.0 - (metrics.AverageResponseTimeMs / 10000.0)); // Normalize to 10 seconds
            var successRateScore = metrics.SuccessRate;
            
            return (responseTimeScore + successRateScore) / 2.0;
        }

        private double EvaluateCostEfficiency(ModelPerformanceMetrics metrics, AdvancedModelRequest request)
        {
            if (metrics.TotalRequests == 0) return 0.5;

            // Calculate cost per token efficiency
            var costEfficiency = 1.0 / ((double)metrics.AverageCostPerToken + 0.001); // Avoid division by zero
            return Math.Min(1.0, costEfficiency); // Normalize to 0-1 range
        }

        private double EvaluateReliability(ModelPerformanceMetrics metrics)
        {
            if (metrics.TotalRequests == 0) return 0.5;

            // Calculate reliability based on success rate and uptime
            var successRate = metrics.SuccessRate;
            var uptimeScore = Math.Max(0, 1.0 - (metrics.ErrorRate));
            
            return (successRate + uptimeScore) / 2.0;
        }

        private async Task<AdvancedModelResponse> ExecuteWithModelAsync(AdvancedModelRequest request, string modelName, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogDebug("[ExecuteWithModelAsync] Starting execution for model: {ModelName}", modelName);
            try
            {
                // Convert to base model request
                var baseRequest = new ModelRequest
                {
                    Input = request.Input,
                    MaxTokens = request.MaxTokens,
                    Temperature = request.Temperature,
                    Metadata = request.Metadata
                };

                // Execute with base orchestrator
                var baseResponse = await _baseOrchestrator.ExecuteAsync(baseRequest, cancellationToken);
                _logger.LogDebug("[ExecuteWithModelAsync] Base orchestrator response: Content={Content}, TokensUsed={TokensUsed}, Cost={Cost}", baseResponse.Content, baseResponse.TokensUsed, baseResponse.Cost);
                
                var response = new AdvancedModelResponse
                {
                    Content = baseResponse.Content,
                    Success = true,
                    ModelUsed = modelName,
                    ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    TokensUsed = baseResponse.TokensUsed,
                    Cost = baseResponse.Cost
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExecuteWithModelAsync] Exception for model {ModelName}: {ErrorMessage}", modelName, ex.Message);
                // Try fallback model
                var fallbackModel = await SelectFallbackModelAsync(request, modelName, cancellationToken);
                if (fallbackModel != null)
                {
                    return await ExecuteWithModelAsync(request, fallbackModel, cancellationToken);
                }
                throw;
            }
        }

        private async Task<string> SelectFallbackModelAsync(AdvancedModelRequest request, string failedModel, CancellationToken cancellationToken)
        {
            // Select alternative model excluding the failed one
            var candidates = _modelCapabilities.Keys
                .Where(m => m != failedModel)
                .ToList();

            if (!candidates.Any()) return null;

            // Use the same selection logic but exclude failed model
            var bestFallback = await SelectOptimalModelAsync(request, cancellationToken);
            return bestFallback != failedModel ? bestFallback : candidates.First();
        }

        private async Task<WorkflowStepResult> ExecuteWorkflowStepAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Prepare step input using context
                var stepInput = await PrepareStepInputAsync(step, context, cancellationToken);
                
                // Execute step
                var stepRequest = new AdvancedModelRequest
                {
                    Input = stepInput,
                    TaskType = step.TaskType,
                    ComplexityLevel = step.ComplexityLevel,
                    RequiredLanguages = step.RequiredLanguages,
                    MaxTokens = step.MaxTokens,
                    Temperature = step.Temperature
                };

                var stepResponse = await ExecuteAdvancedAsync(stepRequest, cancellationToken);
                
                return new WorkflowStepResult
                {
                    StepName = step.Name,
                    Success = stepResponse.Success,
                    Content = stepResponse.Content,
                    ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    ModelUsed = stepResponse.ModelUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow step: {StepName}", step.Name);
                return new WorkflowStepResult
                {
                    StepName = step.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> PrepareStepInputAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken)
        {
            var input = step.InputTemplate;
            
            // Replace placeholders with context values
            foreach (var placeholder in step.InputPlaceholders)
            {
                string value;
                
                if (placeholder.SourceStep == "initial" && placeholder.ExtractionMethod == "static")
                {
                    // Use static value for initial step
                    value = placeholder.StaticValue;
                }
                else if (context.StepResults.TryGetValue(placeholder.SourceStep, out var sourceResult))
                {
                    // Extract value from previous step result
                    value = placeholder.ExtractValue(sourceResult);
                }
                else
                {
                    // Skip placeholder if no source found
                    continue;
                }
                
                input = input.Replace($"{{{placeholder.Name}}}", value);
            }
            
            return input;
        }

        private async Task<AdvancedModelResponse> ApplyPostProcessingAsync(AdvancedModelResponse response, AdvancedModelRequest request, CancellationToken cancellationToken)
        {
            // Apply any post-processing based on request requirements
            if (request.PostProcessingOptions?.Any() == true)
            {
                foreach (var option in request.PostProcessingOptions)
                {
                    response = await ApplyPostProcessingOptionAsync(response, option, cancellationToken);
                }
            }
            
            return response;
        }

        private async Task<AdvancedModelResponse> ApplyPostProcessingOptionAsync(AdvancedModelResponse response, PostProcessingOption option, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var postProcessingResult = new PostProcessingResult
            {
                Type = option.Type,
                Success = true,
                ProcessingTimeMs = 0
            };

            try
            {
                switch (option.Type)
                {
                    case PostProcessingType.Formatting:
                        response.Content = await FormatContentAsync(response.Content, option.Parameters, cancellationToken);
                        break;
                    case PostProcessingType.Validation:
                        var isValid = await ValidateContentAsync(response.Content, option.Parameters, cancellationToken);
                        if (!isValid)
                        {
                            response.Success = false;
                            response.ErrorMessage = "Content validation failed";
                            postProcessingResult.Success = false;
                            postProcessingResult.ErrorMessage = "Content validation failed";
                        }
                        break;
                    case PostProcessingType.Enhancement:
                        response.Content = await EnhanceContentAsync(response.Content, option.Parameters, cancellationToken);
                        break;
                }

                postProcessingResult.ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                postProcessingResult.Success = false;
                postProcessingResult.ErrorMessage = ex.Message;
                postProcessingResult.ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            }

            // Add the post-processing result to the response
            response.PostProcessingResults.Add(postProcessingResult);
            
            return response;
        }

        private async Task<string> FormatContentAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Apply formatting based on parameters
            var formatType = parameters.ContainsKey("formatType") ? parameters["formatType"].ToString() : "default";
            
            switch (formatType.ToLower())
            {
                case "json":
                    return await FormatAsJsonAsync(content, cancellationToken);
                case "markdown":
                    return await FormatAsMarkdownAsync(content, cancellationToken);
                case "code":
                    return await FormatAsCodeAsync(content, parameters, cancellationToken);
                default:
                    return content;
            }
        }

        private async Task<string> FormatAsJsonAsync(string content, CancellationToken cancellationToken)
        {
            // Simple JSON formatting - in a real implementation, this would be more sophisticated
            try
            {
                // Attempt to parse and re-format as JSON
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                return System.Text.Json.JsonSerializer.Serialize(jsonDoc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return content; // Return original if not valid JSON
            }
        }

        private async Task<string> FormatAsMarkdownAsync(string content, CancellationToken cancellationToken)
        {
            // Simple markdown formatting
            return content.Replace("\n", "\n\n").Trim();
        }

        private async Task<string> FormatAsCodeAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var language = parameters.ContainsKey("language") ? parameters["language"].ToString() : "text";
            return $"```{language}\n{content}\n```";
        }

        private async Task<bool> ValidateContentAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var validationType = parameters.ContainsKey("validationType") ? parameters["validationType"].ToString() : "basic";
            
            switch (validationType.ToLower())
            {
                case "json":
                    return IsValidJson(content);
                case "xml":
                    return IsValidXml(content);
                case "code":
                    return await ValidateCodeAsync(content, parameters, cancellationToken);
                default:
                    return true; // Default to valid
            }
        }

        private bool IsValidJson(string content)
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidXml(string content)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ValidateCodeAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Basic code validation - check for common syntax issues
            var language = parameters.ContainsKey("language") ? parameters["language"].ToString() : "csharp";
            
            // Simple validation based on language
            switch (language.ToLower())
            {
                case "csharp":
                    return content.Contains("using") || content.Contains("namespace") || content.Contains("class");
                case "javascript":
                    return content.Contains("function") || content.Contains("const") || content.Contains("let");
                default:
                    return true; // Default to valid
            }
        }

        private async Task<string> EnhanceContentAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var enhancementType = parameters.ContainsKey("enhancementType") ? parameters["enhancementType"].ToString() : "none";
            
            switch (enhancementType.ToLower())
            {
                case "summarize":
                    return await SummarizeContentAsync(content, cancellationToken);
                case "expand":
                    // Map enhancementType to expansionType for the ExpandContentAsync method
                    var expandParameters = new Dictionary<string, object>(parameters) { ["expansionType"] = "details" };
                    return await ExpandContentAsync(content, expandParameters, cancellationToken);
                case "enhancement":
                    // For enhancement type, use the expand functionality with details
                    var enhancedParameters = new Dictionary<string, object>(parameters) { ["expansionType"] = "details" };
                    return await ExpandContentAsync(content, enhancedParameters, cancellationToken);
                default:
                    return content;
            }
        }

        private async Task<string> SummarizeContentAsync(string content, CancellationToken cancellationToken)
        {
            // Simple summarization - in a real implementation, this would use AI
            if (content.Length > 500)
            {
                return content.Substring(0, 500) + "...";
            }
            return content;
        }

        private async Task<string> ExpandContentAsync(string content, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Simple expansion - in a real implementation, this would use AI
            var expansionType = parameters.ContainsKey("expansionType") ? parameters["expansionType"].ToString() : "basic";
            
            switch (expansionType.ToLower())
            {
                case "examples":
                    return content + "\n\nExamples:\n- Example 1\n- Example 2";
                case "details":
                    return content + "\n\nAdditional Details";
                default:
                    return content;
            }
        }

        private void UpdateModelMetrics(string modelName, AdvancedModelResponse response)
        {
            lock (_metricsLock)
            {
                if (!_modelMetrics.ContainsKey(modelName))
                {
                    _modelMetrics[modelName] = new ModelPerformanceMetrics();
                }

                var metrics = _modelMetrics[modelName];
                metrics.TotalRequests++;
                metrics.TotalProcessingTimeMs += response.ProcessingTimeMs;
                metrics.AverageResponseTimeMs = metrics.TotalProcessingTimeMs / metrics.TotalRequests;
                
                if (response.Success)
                {
                    metrics.SuccessfulRequests++;
                }
                else
                {
                    metrics.FailedRequests++;
                }
                
                metrics.SuccessRate = (double)metrics.SuccessfulRequests / metrics.TotalRequests;
                metrics.ErrorRate = (double)metrics.FailedRequests / metrics.TotalRequests;
                
                if (response.TokensUsed > 0)
                {
                    metrics.TotalTokens += response.TokensUsed;
                    metrics.AverageTokensPerRequest = metrics.TotalTokens / metrics.TotalRequests;
                }
                
                if (response.Cost > 0)
                {
                    metrics.TotalCost += response.Cost;
                    metrics.AverageCostPerToken = metrics.TotalCost / metrics.TotalTokens;
                }
            }
        }

        private List<PerformancePattern> AnalyzePerformancePatterns()
        {
            var patterns = new List<PerformancePattern>();
            
            lock (_metricsLock)
            {
                foreach (var kvp in _modelMetrics)
                {
                    var modelName = kvp.Key;
                    var metrics = kvp.Value;
                    
                    patterns.Add(new PerformancePattern
                    {
                        ModelName = modelName,
                        AverageResponseTime = metrics.AverageResponseTimeMs,
                        SuccessRate = metrics.SuccessRate,
                        CostEfficiency = metrics.AverageCostPerToken,
                        RequestVolume = metrics.TotalRequests
                    });
                }
            }
            
            return patterns;
        }

        private List<string> IdentifyBottlenecks()
        {
            var bottlenecks = new List<string>();
            
            lock (_metricsLock)
            {
                foreach (var kvp in _modelMetrics)
                {
                    var modelName = kvp.Key;
                    var metrics = kvp.Value;
                    
                    if (metrics.AverageResponseTimeMs > 5000) // 5 seconds threshold
                    {
                        bottlenecks.Add($"High response time for {modelName}: {metrics.AverageResponseTimeMs}ms");
                    }
                    
                    if (metrics.SuccessRate < 0.9) // 90% success rate threshold
                    {
                        bottlenecks.Add($"Low success rate for {modelName}: {metrics.SuccessRate:P}");
                    }
                    
                    if (metrics.AverageCostPerToken > 0.01M) // Cost threshold
                    {
                        bottlenecks.Add($"High cost for {modelName}: ${metrics.AverageCostPerToken:F4} per token");
                    }
                }
            }
            
            return bottlenecks;
        }

        private List<OptimizationRecommendation> GenerateOptimizationRecommendations(List<PerformancePattern> patterns, List<string> bottlenecks)
        {
            var recommendations = new List<OptimizationRecommendation>();
            
            // Generate recommendations based on bottlenecks
            foreach (var bottleneck in bottlenecks)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = OptimizationType.Performance,
                    Description = bottleneck,
                    Priority = OptimizationPriority.High,
                    EstimatedImpact = "Medium"
                });
            }
            
            // Generate recommendations based on patterns
            var slowModels = patterns.Where(p => p.AverageResponseTime > 3000).ToList();
            if (slowModels.Any())
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = OptimizationType.Performance,
                    Description = $"Consider caching for slow models: {string.Join(", ", slowModels.Select(m => m.ModelName))}",
                    Priority = OptimizationPriority.Medium,
                    EstimatedImpact = "High"
                });
            }
            
            var expensiveModels = patterns.Where(p => p.CostEfficiency > 0.005M).ToList();
            if (expensiveModels.Any())
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = OptimizationType.Cost,
                    Description = $"Consider alternative models for cost optimization: {string.Join(", ", expensiveModels.Select(m => m.ModelName))}",
                    Priority = OptimizationPriority.Medium,
                    EstimatedImpact = "High"
                });
            }
            
            return recommendations;
        }

        private async Task UpdateSelectionRulesAsync(ModelOptimizationResult analysis, CancellationToken cancellationToken)
        {
            // Update selection rules based on analysis results
            lock (_metricsLock)
            {
                // Remove outdated rules
                _selectionRules.RemoveAll(r => r.IsDynamic && r.LastUpdated < DateTime.UtcNow.AddHours(-1));
                
                // Add new rules based on analysis
                foreach (var recommendation in analysis.Recommendations)
                {
                    if (recommendation.Type == OptimizationType.Performance && recommendation.Priority == OptimizationPriority.High)
                    {
                        _selectionRules.Add(new ModelSelectionRule
                        {
                            Name = $"Performance_Optimization_{DateTime.UtcNow.Ticks}",
                            Priority = 1,
                            Condition = (request, model) => Task.FromResult(true), // Simplified condition
                            IsDynamic = true,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        private void InitializeDefaultCapabilities()
        {
            // Initialize default model capabilities
            _modelCapabilities["gpt-4"] = new ModelCapabilityProfile
            {
                SupportedLanguages = new[] { "csharp", "javascript", "python", "java", "typescript" },
                SupportedTasks = new[] { "code_generation", "code_analysis", "documentation", "refactoring", "testing" },
                MaxComplexity = 5,
                MaxTokens = 8192,
                CostPerToken = 0.00003M
            };
            
            _modelCapabilities["gpt-3.5-turbo"] = new ModelCapabilityProfile
            {
                SupportedLanguages = new[] { "csharp", "javascript", "python" },
                SupportedTasks = new[] { "code_generation", "code_analysis", "documentation" },
                MaxComplexity = 3,
                MaxTokens = 4096,
                CostPerToken = 0.000002M
            };
            
            _modelCapabilities["claude-3"] = new ModelCapabilityProfile
            {
                SupportedLanguages = new[] { "csharp", "javascript", "python", "java", "typescript", "rust" },
                SupportedTasks = new[] { "code_generation", "code_analysis", "documentation", "refactoring", "testing", "security_analysis" },
                MaxComplexity = 5,
                MaxTokens = 100000,
                CostPerToken = 0.000015M
            };
        }

        private void InitializeDefaultSelectionRules()
        {
            // Add default selection rules
            _selectionRules.Add(new ModelSelectionRule
            {
                Name = "High_Complexity_Code_Generation",
                Priority = 1,
                Condition = async (request, model) =>
                {
                    var capabilities = _modelCapabilities.ContainsKey(model) ? _modelCapabilities[model] : new ModelCapabilityProfile();
                    return request.ComplexityLevel >= 4 && capabilities.MaxComplexity >= 4;
                }
            });
            
            _selectionRules.Add(new ModelSelectionRule
            {
                Name = "Cost_Optimization",
                Priority = 2,
                Condition = async (request, model) =>
                {
                    var capabilities = _modelCapabilities.ContainsKey(model) ? _modelCapabilities[model] : new ModelCapabilityProfile();
                    return request.MaxTokens > 1000 && capabilities.CostPerToken < 0.00001M;
                }
            });
        }
    }
} 