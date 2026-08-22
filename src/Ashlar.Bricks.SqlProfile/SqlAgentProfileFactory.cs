using Microsoft.Extensions.DependencyInjection;
using Ashlar.Authoring;
using Ashlar.Core.Application.Generation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Bricks.SqlProfile;

/// <summary>
/// Second-language profile plugin: SQL. Registers with
/// <c>AddAshlarAgentProfile&lt;SqlAgentProfileFactory&gt;()</c> — zero edits to
/// <see cref="Ashlar.Core.Application.Generation.GenerativeArtifactBrick"/>.
/// </summary>
public sealed class SqlAgentProfileFactory : IAgentProfileFactory
{
    /// <summary>Stable target id for the SQL profile.</summary>
    public const string TargetId = "sql";

    /// <inheritdoc />
    public AgentProfile Create(IServiceProvider services)
    {
        return new AgentProfile
        {
            TargetId = TargetId,
            Drafter = new SqlArtifactDrafter(),
            DeterministicDrafter = new SqlDeterministicDrafter(),
            Validators = new object[] { new SqlParseValidator() },
            Sandbox = null,
            Deployment = null,
            // Same port as the C++ profile, no container in sight.
            Acceptance = new ExplainAcceptanceEvaluator(),
            Knowledge = new DomainKnowledge
            {
                Standards = new[] { "SQL-92 subset" },
                Rules = Array.Empty<DomainRule>()
            },
            Llm = new LLMConfig
            {
                Model = "none",
                SystemPrompt = "Draft a single SQL statement. No prose."
            },
            Tunables = new GenerationTunables
            {
                PreferDeterministic = true,
                MaxRepairAttempts = 3
            },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsSandbox = false,
                SupportsDeployment = false
            }
        };
    }
}

/// <summary>DI helpers for the SQL profile plugin.</summary>
public static class SqlProfileServiceCollectionExtensions
{
    /// <summary>Registers the SQL agent profile (plugin — no core edits).</summary>
    public static IServiceCollection AddAshlarSqlAgentProfile(this IServiceCollection services) =>
        services.AddAshlarAgentProfile<SqlAgentProfileFactory>();
}
