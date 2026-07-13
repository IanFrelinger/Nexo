using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI for skill advertisement, disclosure, and human-in-the-loop script approvals.
/// </summary>
public sealed class SkillsCommand
{
    private readonly INexoSkillCatalog? _catalog;
    private readonly INexoSkillExecutionContextFactory? _contextFactory;
    private readonly INexoSkillApprovalStore? _approvalStore;
    private readonly ILogger<SkillsCommand> _logger;

    /// <summary>Initializes a new skills command.</summary>
    public SkillsCommand(
        ILogger<SkillsCommand> logger,
        INexoSkillCatalog? catalog = null,
        INexoSkillExecutionContextFactory? contextFactory = null,
        INexoSkillApprovalStore? approvalStore = null)
    {
        _logger = logger;
        _catalog = catalog;
        _contextFactory = contextFactory;
        _approvalStore = approvalStore;
    }

    /// <summary>Lists skill advertisements for a trust tier.</summary>
    public async Task<int> ListAsync(string tier, bool json)
    {
        if (_catalog is null || _contextFactory is null)
        {
            Console.Error.WriteLine("Skills subsystem is not registered.");
            return 1;
        }

        var context = CreateContext(tier);
        var skills = await _catalog.AdvertiseAsync(context).ConfigureAwait(false);
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(skills, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Visible skills ({tier}):");
        foreach (var skill in skills)
            Console.WriteLine($"- {skill.Name}: {skill.Description}");
        return 0;
    }

    /// <summary>Loads full SKILL.md content for a skill.</summary>
    public async Task<int> LoadAsync(string skillName, string tier)
    {
        if (_catalog is null || _contextFactory is null)
        {
            Console.Error.WriteLine("Skills subsystem is not registered.");
            return 1;
        }

        try
        {
            var content = await _catalog.LoadAsync(skillName, CreateContext(tier)).ConfigureAwait(false);
            Console.WriteLine(content);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skill {Skill}", skillName);
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>Lists pending skill script approvals.</summary>
    public Task<int> ListApprovalsAsync(bool json)
    {
        if (_approvalStore is null)
        {
            Console.Error.WriteLine("Skill approval store is not registered.");
            return Task.FromResult(1);
        }

        var pending = _approvalStore.GetPending();
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(pending, new JsonSerializerOptions { WriteIndented = true }));
            return Task.FromResult(0);
        }

        if (pending.Count == 0)
        {
            Console.WriteLine("No pending skill script approvals.");
            return Task.FromResult(0);
        }

        foreach (var request in pending)
        {
            Console.WriteLine(
                $"{request.RequestId}  {request.Key.SkillName}:{request.Key.ScriptPath}  " +
                $"tier={request.Key.TrustTier} barrier={request.Key.BarrierLevel}  {request.Description}");
        }

        return Task.FromResult(0);
    }

    /// <summary>Approves a pending skill script request.</summary>
    public Task<int> ApproveAsync(string requestId, bool json)
        => ResolveAsync(requestId, NexoSkillApprovalStatus.Approved, json);

    /// <summary>Denies a pending skill script request.</summary>
    public Task<int> DenyAsync(string requestId, bool json)
        => ResolveAsync(requestId, NexoSkillApprovalStatus.Denied, json);

    private Task<int> ResolveAsync(string requestId, NexoSkillApprovalStatus status, bool json)
    {
        if (_approvalStore is null)
        {
            Console.Error.WriteLine("Skill approval store is not registered.");
            return Task.FromResult(1);
        }

        if (!_approvalStore.TryResolve(requestId, status, out var request) || request is null)
        {
            Console.Error.WriteLine($"Approval request '{requestId}' was not found or already resolved.");
            return Task.FromResult(1);
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));
            return Task.FromResult(0);
        }

        Console.WriteLine($"Resolved {request.RequestId} -> {request.Status}");
        return Task.FromResult(0);
    }

    private NexoSkillExecutionContext CreateContext(string tier)
    {
        var trustTier = Enum.TryParse<PeerTrustTier>(tier, ignoreCase: true, out var parsed)
            ? parsed
            : PeerTrustTier.Trusted;

        return _contextFactory!.Create(trustTier, "cli-operator");
    }
}
