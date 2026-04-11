using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>
/// Runtime specification for scaffolding and stress-testing orchestrated workflow compositions.
/// </summary>
public sealed record WorkflowLabRuntimeSpec
{
    public WorkflowLabExecutionSpec Execution { get; init; } = new();
    public IReadOnlyList<WorkflowLabRequestSpec> Requests { get; init; } = Array.Empty<WorkflowLabRequestSpec>();
    public IReadOnlyList<WorkflowLabCompositionSpec> Compositions { get; init; } = Array.Empty<WorkflowLabCompositionSpec>();
    public IReadOnlyList<WorkflowLabModelProfileSpec> ModelProfiles { get; init; } = Array.Empty<WorkflowLabModelProfileSpec>();

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
                    Model = "llama3.1"
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
                    Model = "llama3.1"
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

public sealed record WorkflowLabExecutionSpec
{
    public int Iterations { get; init; } = 1;
    public bool PersistHistory { get; init; } = true;
    public string BenchmarkSet { get; init; } = "workflow-lab";
}

public sealed record WorkflowLabRequestSpec
{
    public string Id { get; init; } = "request-1";
    public string Prompt { get; init; } = string.Empty;
}

public sealed record WorkflowLabCompositionSpec
{
    public string Id { get; init; } = "composition-1";
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<WorkflowLabAgentRoleSpec> Roles { get; init; } = Array.Empty<WorkflowLabAgentRoleSpec>();
}

public sealed record WorkflowLabAgentRoleSpec
{
    public string AgentId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Domain { get; init; } = "general";
    public string Goal { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ClusterId { get; init; }
    public string? ReportsToAgentId { get; init; }
    public IReadOnlyList<string> CommandChain { get; init; } = Array.Empty<string>();
    public string? OllamaModel { get; init; }
    public string? Provider { get; init; }
    public string? Prefer { get; init; }
}

public sealed record WorkflowLabModelProfileSpec
{
    public string Id { get; init; } = "profile-1";
    public string Description { get; init; } = string.Empty;
    public ModelRuntimeSpec Default { get; init; } = new();
    public Dictionary<string, ModelRuntimeSpec> Domains { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ModelRuntimeSpec> Agents { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? DefaultModelName { get; init; }
    public Dictionary<string, string> DomainModelNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> AgentModelHints { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
