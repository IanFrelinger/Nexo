using System;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// AI agent for generating game mechanics and systems.
    /// Provides comprehensive game mechanics generation with Unity implementation and performance optimization.
    /// </summary>
    public partial class GameMechanicsGenerationAgent : ISpecializedAgent
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
    }
}
