using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Integrates outputs from multiple agents into a unified result.
/// </summary>
public sealed class OutputIntegrator
{
    private readonly ILogger<OutputIntegrator> _logger;

    public OutputIntegrator(ILogger<OutputIntegrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Integrates outputs from multiple agents based on their specifications.
    /// </summary>
    public IntegratedOutput Integrate(
        IReadOnlyList<AgentSpawnSpec> agentSpecs,
        IReadOnlyDictionary<string, object> agentOutputs)
    {
        if (agentSpecs == null)
        {
            throw new ArgumentNullException(nameof(agentSpecs));
        }

        if (agentOutputs == null)
        {
            throw new ArgumentNullException(nameof(agentOutputs));
        }

        _logger.LogInformation("Integrating outputs from {AgentCount} agents", agentSpecs.Count);

        var integrated = new Dictionary<string, object>();
        var errors = new List<string>();

        // Group outputs by domain
        var outputsByDomain = agentSpecs
            .Where(spec => agentOutputs.ContainsKey(spec.AgentId))
            .GroupBy(spec => spec.Domain)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (domain, specs) in outputsByDomain)
        {
            try
            {
                var domainOutputs = specs
                    .Select(spec => new
                    {
                        Spec = spec,
                        Output = agentOutputs[spec.AgentId]
                    })
                    .ToList();

                var domainOutputsList = domainOutputs
                    .Select(item => (item.Spec, item.Output))
                    .ToList();
                var domainIntegrated = IntegrateDomainOutputs(domain, domainOutputsList);
                integrated[domain] = domainIntegrated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error integrating outputs for domain {Domain}", domain);
                errors.Add($"Failed to integrate {domain} domain: {ex.Message}");
            }
        }

        // Create unified output structure
        var result = new IntegratedOutput
        {
            IntegratedResults = integrated,
            AgentOutputs = agentOutputs,
            IntegrationErrors = errors,
            IntegratedAt = DateTimeOffset.UtcNow
        };

        // Validate integration
        var validationErrors = ValidateIntegration(result, agentSpecs);
        
        // Create new result with validation errors
        result = result with { ValidationErrors = validationErrors };

        _logger.LogInformation("Integration complete: {DomainCount} domains, {ErrorCount} errors",
            integrated.Count, errors.Count + validationErrors.Count);

        return result with { ValidationErrors = validationErrors };
    }

    private object IntegrateDomainOutputs(string domain, List<(AgentSpawnSpec Spec, object Output)> domainOutputs)
    {
        // Simple integration: combine outputs into a domain-specific structure
        // In a real implementation, this would use domain-specific integration logic

        if (domainOutputs.Count == 1)
        {
            return domainOutputs[0].Output;
        }

        // Multiple outputs in same domain - combine them
        var combined = new Dictionary<string, object>();
        foreach (var (spec, output) in domainOutputs)
        {
            combined[spec.AgentId] = output;
        }

        return combined;
    }

    private List<string> ValidateIntegration(IntegratedOutput result, IReadOnlyList<AgentSpawnSpec> agentSpecs)
    {
        var errors = new List<string>();

        // Check that all agents with outputs are included
        var agentsWithOutputs = result.AgentOutputs.Keys.ToHashSet();
        var allAgentIds = agentSpecs.Select(s => s.AgentId).ToHashSet();

        var missingOutputs = allAgentIds.Except(agentsWithOutputs).ToList();
        if (missingOutputs.Count > 0)
        {
            errors.Add($"Missing outputs from agents: {string.Join(", ", missingOutputs)}");
        }

        // Check schema compatibility for integrated results
        // This is simplified - in a real implementation, we'd validate against expected schemas

        return errors;
    }
}

/// <summary>
/// Integrated output from multiple agents.
/// </summary>
public sealed record IntegratedOutput
{
    /// <summary>
    /// Integrated results organized by domain.
    /// </summary>
    public required IReadOnlyDictionary<string, object> IntegratedResults { get; init; }

    /// <summary>
    /// Raw outputs from individual agents.
    /// </summary>
    public required IReadOnlyDictionary<string, object> AgentOutputs { get; init; }

    /// <summary>
    /// Errors encountered during integration.
    /// </summary>
    public IReadOnlyList<string> IntegrationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation errors in the integrated output.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when integration was performed.
    /// </summary>
    public DateTimeOffset IntegratedAt { get; init; }

    /// <summary>
    /// Whether the integration is valid (no errors).
    /// </summary>
    public bool IsValid => IntegrationErrors.Count == 0 && ValidationErrors.Count == 0;
}

