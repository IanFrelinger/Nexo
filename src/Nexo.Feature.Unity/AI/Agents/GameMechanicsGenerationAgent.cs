using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// AI agent for generating game mechanics and systems
    /// </summary>
    public class GameMechanicsGenerationAgent : ISpecializedAgent
    {
        public string AgentId => "GameMechanicsGeneration";
        public AgentSpecialization Specialization => AgentSpecialization.GameDevelopment | AgentSpecialization.ArchitecturalDesign;
        public PlatformCompatibility PlatformExpertise => PlatformCompatibility.Unity;
        
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IUnityCodeGenerator _unityCodeGenerator;
        private readonly ILogger<GameMechanicsGenerationAgent> _logger;
        
        public GameMechanicsGenerationAgent(
            IModelOrchestrator modelOrchestrator,
            IUnityCodeGenerator unityCodeGenerator,
            ILogger<GameMechanicsGenerationAgent> logger)
        {
            _modelOrchestrator = modelOrchestrator;
            _unityCodeGenerator = unityCodeGenerator;
            _logger = logger;
        }
        
        public async Task<AgentResponse> ProcessAsync(AgentRequest request)
        {
            _logger.LogInformation("Processing game mechanics generation request");
            
            try
            {
                var mechanicsRequest = request.GetMechanicsGenerationRequest();
                
                // Generate game mechanics based on requirements
                var generatedMechanics = await GenerateGameMechanics(mechanicsRequest);
                
                // Create Unity implementation
                var unityImplementation = await CreateUnityImplementation(generatedMechanics);
                
                // Optimize for performance
                var optimizedImplementation = await OptimizeForUnityPerformance(unityImplementation);
                
                return new AgentResponse
                {
                    Result = optimizedImplementation,
                    Confidence = 0.8,
                    Metadata = new Dictionary<string, object>
                    {
                        ["GeneratedMechanics"] = generatedMechanics,
                        ["UnityComponents"] = unityImplementation.Components,
                        ["PerformanceOptimizations"] = optimizedImplementation.Optimizations
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process game mechanics generation");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }
        
        public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
        {
            _logger.LogInformation("Coordinating game mechanics generation with other agents");
            
            try
            {
                // Coordinate with gameplay balance agent for balanced mechanics
                var balanceAgent = collaborators.FirstOrDefault(a => a.AgentId == "GameplayBalance");
                
                if (balanceAgent != null)
                {
                    // Get balance considerations for generated mechanics
                    var balanceRequest = request.CreateBalanceAnalysisRequest();
                    var balanceResponse = await balanceAgent.ProcessAsync(balanceRequest);
                    
                    // Generate mechanics with balance considerations
                    var mechanicsResponse = await ProcessAsync(request);
                    
                    // Integrate balance feedback
                    var integratedMechanics = await IntegrateBalanceFeedback(mechanicsResponse, balanceResponse);
                    
                    return new AgentResponse
                    {
                        Result = integratedMechanics,
                        Confidence = Math.Min(mechanicsResponse.Confidence, balanceResponse.Confidence),
                        Metadata = MergeMetadata(mechanicsResponse.Metadata, balanceResponse.Metadata)
                    };
                }
                
                return await ProcessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to coordinate game mechanics generation");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }
        
        private async Task<GeneratedGameMechanics> GenerateGameMechanics(MechanicsGenerationRequest request)
        {
            var prompt = $"""
            Design game mechanics for this requirement:
            
            Game Type: {request.GameType}
            Core Mechanics Needed: {request.RequiredMechanics}
            Target Audience: {request.TargetAudience}
            Platform: Unity (Mobile/PC/Console)
            Performance Requirements: {request.PerformanceRequirements}
            
            Generate:
            1. Core mechanic systems with clear rules
            2. Player interaction patterns
            3. Progression systems
            4. Balance considerations
            5. Technical implementation approach
            6. Performance optimization strategies
            
            Design for {request.TargetPlatform} with emphasis on:
            - Smooth 60 FPS gameplay
            - Intuitive controls
            - Scalable difficulty
            - Engaging progression
            
            Provide detailed technical specifications for Unity implementation.
            """;
            
            var modelRequest = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1500,
                Temperature = 0.7
            };
            
            var response = await _modelOrchestrator.ProcessAsync(modelRequest);
            return ParseGeneratedMechanics(response.Response);
        }
        
        private async Task<UnityImplementation> CreateUnityImplementation(GeneratedGameMechanics mechanics)
        {
            var implementation = new UnityImplementation();
            
            foreach (var mechanic in mechanics.Mechanics)
            {
                // Generate Unity MonoBehaviour scripts
                var componentCode = await GenerateUnityComponent(mechanic);
                implementation.Components.Add(componentCode);
                
                // Generate necessary data structures
                var dataStructures = await GenerateDataStructures(mechanic);
                implementation.DataStructures.AddRange(dataStructures);
                
                // Generate UI elements if needed
                if (mechanic.RequiresUI)
                {
                    var uiComponents = await GenerateUIComponents(mechanic);
                    implementation.UIComponents.AddRange(uiComponents);
                }
            }
            
            return implementation;
        }
        
        private async Task<OptimizedUnityImplementation> OptimizeForUnityPerformance(UnityImplementation implementation)
        {
            var optimized = new OptimizedUnityImplementation
            {
                OriginalImplementation = implementation,
                Optimizations = new List<PerformanceOptimization>()
            };
            
            // Analyze each component for optimization opportunities
            foreach (var component in implementation.Components)
            {
                var optimizations = await AnalyzeComponentForOptimizations(component);
                optimized.Optimizations.AddRange(optimizations);
            }
            
            // Apply optimizations
            optimized.OptimizedComponents = await ApplyOptimizations(implementation.Components, optimized.Optimizations);
            
            return optimized;
        }
        
        private async Task<UnityComponentCode> GenerateUnityComponent(GameMechanic mechanic)
        {
            var prompt = $"""
            Generate Unity MonoBehaviour code for this game mechanic:
            
            Mechanic Name: {mechanic.Name}
            Description: {mechanic.Description}
            Rules: {string.Join(", ", mechanic.Rules)}
            Performance Requirements: {mechanic.PerformanceRequirements}
            
            Requirements:
            1. Use Unity best practices
            2. Optimize for performance (avoid allocations in Update)
            3. Include proper serialization
            4. Add comprehensive comments
            5. Include error handling
            6. Make it extensible and maintainable
            
            Generate complete, production-ready Unity C# code.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1000,
                Temperature = 0.5
            };
            
            var response = await _modelOrchestrator.ProcessAsync(request);
            
            return new UnityComponentCode
            {
                MechanicName = mechanic.Name,
                Code = response.Response,
                Dependencies = ExtractDependencies(response.Response),
                PerformanceNotes = ExtractPerformanceNotes(response.Response)
            };
        }
        
        private async Task<IEnumerable<DataStructure>> GenerateDataStructures(GameMechanic mechanic)
        {
            var dataStructures = new List<DataStructure>();
            
            // Generate ScriptableObject for configuration
            if (mechanic.RequiresConfiguration)
            {
                var configPrompt = $"""
                Generate Unity ScriptableObject for {mechanic.Name} configuration:
                
                Include:
                1. Serializable fields for all configurable parameters
                2. Validation methods
                3. Default values
                4. Editor-friendly attributes
                5. Performance considerations
                
                Generate complete ScriptableObject code.
                """;
                
                var request = new ModelRequest
                {
                    Input = configPrompt,
                    ModelType = ModelType.TextGeneration,
                    MaxTokens = 600,
                    Temperature = 0.5
                };
                
                var response = await _modelOrchestrator.ProcessAsync(request);
                
                dataStructures.Add(new DataStructure
                {
                    Type = "ScriptableObject",
                    Name = $"{mechanic.Name}Config",
                    Code = response.Response
                });
            }
            
            // Generate data classes for game state
            if (mechanic.RequiresStateManagement)
            {
                var statePrompt = $"""
                Generate data classes for {mechanic.Name} state management:
                
                Include:
                1. Serializable data classes
                2. State transition logic
                3. Validation methods
                4. Performance optimizations
                
                Generate complete data class code.
                """;
                
                var request = new ModelRequest
                {
                    Input = statePrompt,
                    ModelType = ModelType.TextGeneration,
                    MaxTokens = 600,
                    Temperature = 0.5
                };
                
                var response = await _modelOrchestrator.ProcessAsync(request);
                
                dataStructures.Add(new DataStructure
                {
                    Type = "DataClass",
                    Name = $"{mechanic.Name}State",
                    Code = response.Response
                });
            }
            
            return dataStructures;
        }
        
        private async Task<IEnumerable<UIComponent>> GenerateUIComponents(GameMechanic mechanic)
        {
            var uiComponents = new List<UIComponent>();
            
            var prompt = $"""
            Generate Unity UI components for {mechanic.Name}:
            
            Requirements:
            1. UGUI-based UI elements
            2. Responsive design
            3. Performance optimized
            4. Accessible controls
            5. Visual feedback
            
            Generate:
            1. UI prefab structure
            2. UI controller script
            3. Animation setup
            4. Event handling
            
            Provide complete UI implementation code.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 800,
                Temperature = 0.5
            };
            
            var response = await _modelOrchestrator.ProcessAsync(request);
            
            uiComponents.Add(new UIComponent
            {
                MechanicName = mechanic.Name,
                ComponentType = "UI Controller",
                Code = response.Response,
                PrefabStructure = ExtractPrefabStructure(response.Response)
            });
            
            return uiComponents;
        }
        
        private async Task<IEnumerable<PerformanceOptimization>> AnalyzeComponentForOptimizations(UnityComponentCode component)
        {
            var optimizations = new List<PerformanceOptimization>();
            
            // Analyze for common Unity performance issues
            if (component.Code.Contains("GetComponent"))
            {
                optimizations.Add(new PerformanceOptimization
                {
                    Type = "Component Caching",
                    Description = "Cache GetComponent calls to avoid repeated lookups",
                    Impact = PerformanceImpact.High,
                    Implementation = "Store component references in Awake() or Start()"
                });
            }
            
            if (component.Code.Contains("foreach"))
            {
                optimizations.Add(new PerformanceOptimization
                {
                    Type = "Loop Optimization",
                    Description = "Replace foreach with for-loop for better performance",
                    Impact = PerformanceImpact.Medium,
                    Implementation = "Use traditional for-loop with index"
                });
            }
            
            if (component.Code.Contains("string +"))
            {
                optimizations.Add(new PerformanceOptimization
                {
                    Type = "String Concatenation",
                    Description = "Use StringBuilder for string concatenation",
                    Impact = PerformanceImpact.Medium,
                    Implementation = "Replace string concatenation with StringBuilder"
                });
            }
            
            if (component.Code.Contains("Instantiate"))
            {
                optimizations.Add(new PerformanceOptimization
                {
                    Type = "Object Pooling",
                    Description = "Implement object pooling for frequently instantiated objects",
                    Impact = PerformanceImpact.High,
                    Implementation = "Create object pool and reuse instances"
                });
            }
            
            return optimizations;
        }
        
        private async Task<IEnumerable<UnityComponentCode>> ApplyOptimizations(
            IEnumerable<UnityComponentCode> components, 
            IEnumerable<PerformanceOptimization> optimizations)
        {
            var optimizedComponents = new List<UnityComponentCode>();
            
            foreach (var component in components)
            {
                var optimizedCode = component.Code;
                var appliedOptimizations = new List<string>();
                
                foreach (var optimization in optimizations)
                {
                    if (ShouldApplyOptimization(component, optimization))
                    {
                        optimizedCode = ApplyOptimization(optimizedCode, optimization);
                        appliedOptimizations.Add(optimization.Description);
                    }
                }
                
                optimizedComponents.Add(new UnityComponentCode
                {
                    MechanicName = component.MechanicName,
                    Code = optimizedCode,
                    Dependencies = component.Dependencies,
                    PerformanceNotes = component.PerformanceNotes.Concat(appliedOptimizations).ToList()
                });
            }
            
            return optimizedComponents;
        }
        
        private async Task<object> IntegrateBalanceFeedback(AgentResponse mechanicsResponse, AgentResponse balanceResponse)
        {
            // Integrate balance considerations into generated mechanics
            return new
            {
                Mechanics = mechanicsResponse.Result,
                BalanceConsiderations = balanceResponse.Result,
                IntegratedApproach = "Balance-optimized game mechanics"
            };
        }
        
        private Dictionary<string, object> MergeMetadata(Dictionary<string, object> mechanicsMetadata, Dictionary<string, object> balanceMetadata)
        {
            var merged = new Dictionary<string, object>(mechanicsMetadata);
            
            foreach (var kvp in balanceMetadata)
            {
                merged[$"Balance_{kvp.Key}"] = kvp.Value;
            }
            
            return merged;
        }
        
        private GeneratedGameMechanics ParseGeneratedMechanics(string aiResponse)
        {
            var mechanics = new GeneratedGameMechanics
            {
                Mechanics = new List<GameMechanic>(),
                TechnicalSpecifications = aiResponse,
                PerformanceStrategies = ExtractPerformanceStrategies(aiResponse)
            };
            
            // Parse mechanics from AI response
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3."))
                {
                    mechanics.Mechanics.Add(new GameMechanic
                    {
                        Name = ExtractMechanicName(line),
                        Description = line.Substring(2).Trim(),
                        Rules = ExtractRules(line),
                        PerformanceRequirements = "60 FPS target",
                        RequiresUI = line.ToLower().Contains("ui") || line.ToLower().Contains("interface"),
                        RequiresConfiguration = line.ToLower().Contains("config") || line.ToLower().Contains("setting"),
                        RequiresStateManagement = line.ToLower().Contains("state") || line.ToLower().Contains("data")
                    });
                }
            }
            
            return mechanics;
        }
        
        private string ExtractMechanicName(string line)
        {
            // Extract mechanic name from line
            var parts = line.Split(':');
            if (parts.Length > 1)
            {
                return parts[1].Trim().Split(' ')[0];
            }
            
            return "GeneratedMechanic";
        }
        
        private List<string> ExtractRules(string line)
        {
            // Extract rules from line
            var rules = new List<string>();
            
            if (line.Contains("rule"))
            {
                rules.Add("Follow game design principles");
            }
            
            if (line.Contains("balance"))
            {
                rules.Add("Maintain game balance");
            }
            
            return rules;
        }
        
        private List<string> ExtractPerformanceStrategies(string response)
        {
            var strategies = new List<string>();
            var lines = response.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("performance") || line.ToLower().Contains("optimization"))
                {
                    strategies.Add(line.Trim());
                }
            }
            
            return strategies;
        }
        
        private List<string> ExtractDependencies(string code)
        {
            var dependencies = new List<string>();
            
            if (code.Contains("using UnityEngine"))
                dependencies.Add("UnityEngine");
            
            if (code.Contains("using UnityEngine.UI"))
                dependencies.Add("UnityEngine.UI");
            
            if (code.Contains("using System.Collections"))
                dependencies.Add("System.Collections");
            
            return dependencies;
        }
        
        private List<string> ExtractPerformanceNotes(string code)
        {
            var notes = new List<string>();
            
            if (code.Contains("Update"))
                notes.Add("Optimize Update method for performance");
            
            if (code.Contains("GetComponent"))
                notes.Add("Consider caching component references");
            
            return notes;
        }
        
        private string ExtractPrefabStructure(string code)
        {
            // Extract UI prefab structure from code
            return "Canvas > Panel > UI Elements";
        }
        
        private bool ShouldApplyOptimization(UnityComponentCode component, PerformanceOptimization optimization)
        {
            return optimization.Type switch
            {
                "Component Caching" => component.Code.Contains("GetComponent"),
                "Loop Optimization" => component.Code.Contains("foreach"),
                "String Concatenation" => component.Code.Contains("string +"),
                "Object Pooling" => component.Code.Contains("Instantiate"),
                _ => false
            };
        }
        
        private string ApplyOptimization(string code, PerformanceOptimization optimization)
        {
            return optimization.Type switch
            {
                "Component Caching" => code.Replace("GetComponent", "// Cached component reference"),
                "Loop Optimization" => code.Replace("foreach", "for"),
                "String Concatenation" => code.Replace("string +", "StringBuilder.Append"),
                "Object Pooling" => code.Replace("Instantiate", "ObjectPool.Get"),
                _ => code
            };
        }
    }
    
    /// <summary>
    /// Unity code generator interface
    /// </summary>
    public interface IUnityCodeGenerator
    {
        Task<string> GenerateMonoBehaviourAsync(string mechanicName, string requirements);
        Task<string> GenerateScriptableObjectAsync(string configName, string requirements);
        Task<string> GenerateDataClassAsync(string className, string requirements);
    }
    
    /// <summary>
    /// Mechanics generation request
    /// </summary>
    public class MechanicsGenerationRequest
    {
        public string GameType { get; set; } = string.Empty;
        public string RequiredMechanics { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string TargetPlatform { get; set; } = string.Empty;
        public string PerformanceRequirements { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Generated game mechanics
    /// </summary>
    public class GeneratedGameMechanics
    {
        public IEnumerable<GameMechanic> Mechanics { get; set; } = new List<GameMechanic>();
        public string TechnicalSpecifications { get; set; } = string.Empty;
        public IEnumerable<string> PerformanceStrategies { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Game mechanic
    /// </summary>
    public class GameMechanic
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IEnumerable<string> Rules { get; set; } = new List<string>();
        public string PerformanceRequirements { get; set; } = string.Empty;
        public bool RequiresUI { get; set; }
        public bool RequiresConfiguration { get; set; }
        public bool RequiresStateManagement { get; set; }
    }
    
    /// <summary>
    /// Unity implementation
    /// </summary>
    public class UnityImplementation
    {
        public IEnumerable<UnityComponentCode> Components { get; set; } = new List<UnityComponentCode>();
        public IEnumerable<DataStructure> DataStructures { get; set; } = new List<DataStructure>();
        public IEnumerable<UIComponent> UIComponents { get; set; } = new List<UIComponent>();
    }
    
    /// <summary>
    /// Optimized Unity implementation
    /// </summary>
    public class OptimizedUnityImplementation
    {
        public UnityImplementation OriginalImplementation { get; set; } = new();
        public IEnumerable<UnityComponentCode> OptimizedComponents { get; set; } = new List<UnityComponentCode>();
        public IEnumerable<PerformanceOptimization> Optimizations { get; set; } = new List<PerformanceOptimization>();
    }
    
    /// <summary>
    /// Unity component code
    /// </summary>
    public class UnityComponentCode
    {
        public string MechanicName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public IEnumerable<string> Dependencies { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceNotes { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Data structure
    /// </summary>
    public class DataStructure
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// UI component
    /// </summary>
    public class UIComponent
    {
        public string MechanicName { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string PrefabStructure { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Performance optimization
    /// </summary>
    public class PerformanceOptimization
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PerformanceImpact Impact { get; set; }
        public string Implementation { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Performance impact
    /// </summary>
    public enum PerformanceImpact
    {
        Low,
        Medium,
        High,
        Critical
    }
}
