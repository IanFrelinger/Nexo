using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Runtime;

/// <summary>
/// Runtime specification for scaffolding and stress-testing orchestrated workflow compositions.
/// </summary>
public sealed record WorkflowLabRuntimeSpec
{
    /// <summary>Execution controls for stress and benchmark runs.</summary>
    public WorkflowLabExecutionSpec Execution { get; init; } = new();

    /// <summary>Prompt scenarios exercised during stress runs.</summary>
    public IReadOnlyList<WorkflowLabRequestSpec> Requests { get; init; } = Array.Empty<WorkflowLabRequestSpec>();

    /// <summary>Agent compositions evaluated during stress runs.</summary>
    public IReadOnlyList<WorkflowLabCompositionSpec> Compositions { get; init; } = Array.Empty<WorkflowLabCompositionSpec>();

    /// <summary>Model routing profiles paired with compositions during stress runs.</summary>
    public IReadOnlyList<WorkflowLabModelProfileSpec> ModelProfiles { get; init; } = Array.Empty<WorkflowLabModelProfileSpec>();

    /// <summary>Returns the built-in default workflow lab runtime specification.</summary>
    public static WorkflowLabRuntimeSpec Default() => new()
    {
        Execution = new WorkflowLabExecutionSpec
        {
            Iterations = 2,
            PersistHistory = true,
            BenchmarkSet = "workflow-lab"
        },
        Requests = new[]
        {
            new WorkflowLabRequestSpec
            {
                Id = "fullstack-feature",
                Prompt = "Plan and deliver a small full-stack feature with tests, docs, and rollout notes."
            },
            new WorkflowLabRequestSpec
            {
                Id = "incident-triage",
                Prompt = "Triage a production incident, identify root cause, propose a fix, and outline risk controls."
            }
        },
        Compositions = new[]
        {
            new WorkflowLabCompositionSpec
            {
                Id = "single-specialist",
                Description = "One agent handles planning, implementation, and validation end-to-end.",
                Roles = new[]
                {
                    new WorkflowLabAgentRoleSpec
                    {
                        AgentId = "specialist-1",
                        Role = "builder",
                        Domain = "engineering",
                        Goal = "Deliver the objective with complete implementation and validation evidence.",
                        ClusterId = "solo"
                    }
                }
            },
            new WorkflowLabCompositionSpec
            {
                Id = "hierarchy-squad",
                Description = "Hierarchical command chain with planner -> implementer -> qa.",
                Roles = new[]
                {
                    new WorkflowLabAgentRoleSpec
                    {
                        AgentId = "planner-1",
                        Role = "planner",
                        Domain = "coordination",
                        Goal = "Break the objective into clear execution steps and constraints.",
                        ClusterId = "core"
                    },
                    new WorkflowLabAgentRoleSpec
                    {
                        AgentId = "builder-1",
                        Role = "builder",
                        Domain = "engineering",
                        Goal = "Implement the requested change-set with maintainable code.",
                        ClusterId = "core",
                        ReportsToAgentId = "planner-1",
                        CommandChain = new[] { "planner-1" }
                    },
                    new WorkflowLabAgentRoleSpec
                    {
                        AgentId = "qa-1",
                        Role = "qa-functional",
                        Domain = "quality",
                        Goal = "Run focused tests and provide actionable pass/fail evidence.",
                        ClusterId = "quality",
                        ReportsToAgentId = "planner-1",
                        CommandChain = new[] { "planner-1" }
                    }
                }
            }
        },
        ModelProfiles = new[]
        {
            new WorkflowLabModelProfileSpec
            {
                Id = "ollama-balanced",
                Description = "Balanced local Ollama setup across all agents.",
                Default = new ModelRuntimeSpec
                {
                    Prefer = "agentic",
                    Provider = "ollama",
                    Model = "llama3.1:latest"
                }
            },
            new WorkflowLabModelProfileSpec
            {
                Id = "ollama-mixed",
                Description = "Mix planners/builders/reviewers onto different Ollama models.",
                Default = new ModelRuntimeSpec
                {
                    Prefer = "agentic",
                    Provider = "ollama",
                    Model = "llama3.1:latest"
                },
                Agents = new Dictionary<string, ModelRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
                {
                    ["planner-1"] = new ModelRuntimeSpec { Prefer = "agentic", Provider = "ollama", Model = "qwen2.5:7b" },
                    ["builder-1"] = new ModelRuntimeSpec { Prefer = "agentic", Provider = "ollama", Model = "codellama:13b" },
                    ["qa-1"] = new ModelRuntimeSpec { Prefer = "deterministic", Provider = "ollama", Model = "mistral:7b" }
                }
            }
        }
    };
}
