using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Guide;

/// <summary>
/// Defines the Nexo Guide agent card and behaviors for use with the rest of the Nexo stack (src).
/// </summary>
public static class GuideNexoSetup
{
    public const string GuideAgentId = "nexo-guide";
    public const string GuideChatBehaviorId = "nexo-guide.chat";

    public static AgentCard CreateAgentCard()
    {
        return new AgentCard
        {
            Id = GuideAgentId,
            Name = "Nexo Guide",
            Version = "1.0.0",
            Icon = "📘",
            Domain = "Guide",
            Description = "AI guide that explains Nexo using live visuals; runs locally via Ollama.",
            Platforms = [Nexo.Core.Domain.Agents.Platform.Cli],
            Behaviors = [GuideChatBehaviorId],
            Memory = new AgentMemoryConfig { SemanticCache = false, PatternLibrary = false, CrossProjectSharing = false },
            Constraints = new AgentConstraints
            {
                MaxExecutionTime = TimeSpan.FromSeconds(30),
                MaxConcurrentBehaviors = 1
            }
        };
    }

    public static Nexo.Core.Domain.Behaviors.Behavior CreateGuideChatBehavior()
    {
        return new Nexo.Core.Domain.Behaviors.Behavior
        {
            Id = GuideChatBehaviorId,
            Name = "Guide Chat",
            Version = "1.0.0",
            Description = "Single-step chat: context, audience, Ollama inference, safety, suggestions.",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "step-chat",
                    BrickId = "nexo-guide.chat",
                    Implementation = ImplementationType.Agentic,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["userMessage"] = "userMessage",
                        ["audience"] = "audience",
                        ["historyJson"] = "historyJson",
                        ["topicsCoveredJson"] = "topicsCoveredJson"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["text"] = "text",
                        ["renderPlanJson"] = "renderPlanJson",
                        ["suggestedQuestionsJson"] = "suggestedQuestionsJson",
                        ["topicCovered"] = "topicCovered"
                    }
                }
            ],
            OnStepFailure = FailurePolicy.Abort,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
