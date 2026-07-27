using Microsoft.Extensions.DependencyInjection;
using Nexo.Authoring;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.SqlProfile;

/// <summary>
/// Second-language profile plugin: SQL. Registers with
/// <c>AddNexoAgentProfile&lt;SqlAgentProfileFactory&gt;()</c> — zero edits to
/// <see cref="Nexo.Core.Application.Generation.GenerativeArtifactBrick"/>.
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
    public static IServiceCollection AddNexoSqlAgentProfile(this IServiceCollection services) =>
        services.AddNexoAgentProfile<SqlAgentProfileFactory>();
}
