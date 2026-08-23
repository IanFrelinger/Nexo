using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// Base class for domain agents with LLM integration.
/// 
/// Provides common functionality for domain-specific agents:
/// - LLM integration via IModel
/// - Domain-specific prompt building via DomainPromptBuilder
/// - Structured output generation (JSON parsing with fallback)
/// - Mock output generation when LLM is unavailable
/// 
/// Derived classes (SecurityAgent, InfrastructureAgent, and game agents supplied by a
/// package) implement domain-specific
/// system prompts and mock outputs.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public abstract class BaseDomainAgent : BaseAgent
{
    protected readonly IModel? Model;
    protected readonly DomainPromptBuilder PromptBuilder;

    protected BaseDomainAgent(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null)
        : base(spec, logger)
    {
        Model = model;
        PromptBuilder = new DomainPromptBuilder();
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        if (Model != null)
        {
            // Use real LLM to generate domain-specific design
            var prompt = PromptBuilder.BuildExecutionPrompt(Spec, dependencyOutputs);
            var systemPrompt = GetSystemPrompt();
            
            var modelInput = new ModelInput(new[]
            {
                ("system", systemPrompt),
                ("user", prompt)
            });
            
            var modelOutput = await Model.CompleteAsync(modelInput, cancellationToken);
            
            // Parse JSON output if possible, otherwise wrap in structured format
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(modelOutput.Text);
                return new
                {
                    AgentId = Spec.AgentId,
                    Domain = Spec.Domain,
                    Goal = Spec.Goal,
                    Output = jsonDoc.RootElement,
                    GeneratedAt = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                // If not JSON, wrap in structured format
                return new
                {
                    AgentId = Spec.AgentId,
                    Domain = Spec.Domain,
                    Goal = Spec.Goal,
                    Output = modelOutput.Text,
                    GeneratedAt = DateTimeOffset.UtcNow
                };
            }
        }
        
        // Fallback to mock output if no model provided
        return GetMockOutput();
    }

    /// <summary>
    /// Gets the system prompt for this domain.
    /// </summary>
    protected abstract string GetSystemPrompt();

    /// <summary>
    /// Gets mock output when no model is available.
    /// </summary>
    protected abstract object GetMockOutput();
}

