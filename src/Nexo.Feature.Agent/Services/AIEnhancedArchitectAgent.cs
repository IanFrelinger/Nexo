using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Represents an AI-enhanced agent with a focus on architectural tasks,
    /// inheriting foundational functionalities from the BaseAIEnhancedAgent class.
    /// </summary>
    public partial class AiEnhancedArchitectAgent : BaseAiEnhancedAgent
    {
        /// <summary>
        /// Represents an AI-enhanced agent specializing in architectural disciplines. This agent focuses on
        /// designing and optimizing various system architectures while leveraging its AI-driven analytical capabilities.
        /// </summary>
        /// <remarks>
        /// The AI-Enhanced Architect Agent extends the functionality of the BaseAIEnhancedAgent class
        /// by incorporating specific capabilities and focus areas related to system, solution, and enterprise architecture.
        /// It is designed to perform tasks that require architectural expertise and provide AI-powered insights
        /// for tasks such as code analysis, problem-solving, and process optimization.
        /// </remarks>
        public AiEnhancedArchitectAgent(
            IModelOrchestrator modelOrchestrator,
            ILogger<AiEnhancedArchitectAgent> logger)
            : base(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("AI-Enhanced Architect Agent"),
                new AgentRole("Architect"),
                modelOrchestrator,
                logger)
        {
            // Initialize architect-specific capabilities
            Capabilities.AddRange(new[]
            {
                "System Architecture Design",
                "Solution Architecture",
                "Technical Architecture",
                "Enterprise Architecture",
                "Cloud Architecture",
                "Microservices Design",
                "API Design",
                "Database Architecture",
                "Security Architecture",
                "Performance Architecture",
                "Scalability Design",
                "Integration Architecture"
            });

            FocusAreas.AddRange(new[]
            {
                "Enterprise Architecture",
                "Cloud-Native Architecture",
                "Microservices",
                "Event-Driven Architecture",
                "Domain-Driven Design",
                "Clean Architecture",
                "Hexagonal Architecture",
                "CQRS Pattern",
                "Event Sourcing",
                "API-First Design",
                "Security by Design",
                "Performance Optimization"
            });

            // Configure AI capabilities
            AiCapabilities.CanAnalyzeCode = true;
            AiCapabilities.CanGenerateCode = false; // Architects focus on design, not implementation
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanSolveProblems = true;
            AiCapabilities.PreferredModel = "gpt-4";
            AiCapabilities.ProcessingStrategy = AiProcessingStrategy.Advanced;
        }
    }
}