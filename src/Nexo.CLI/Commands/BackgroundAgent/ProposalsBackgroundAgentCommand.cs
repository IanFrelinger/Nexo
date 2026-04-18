using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Forge;
using Nexo.Tools.Dev;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Operator-facing CLI for the forge change-proposal queue. Mirrors the
/// objectives CLI in shape (list / show / approve / reject / apply / stats)
/// because the two stores have the same lifecycle vocabulary, and operators
/// shouldn't have to learn two mental models.
///
/// <para>Apply intentionally writes the file via plain <see cref="File.WriteAllText(string, string)"/>
/// rather than going through the agent toolbox: at apply time the operator has
/// already approved the diff, so we don't want a write policy to second-guess
/// them. The base-sha drift check is the safety net.</para>
/// </summary>
public class ProposalsBackgroundAgentCommand
{
    private readonly IChangeProposalStore _store;
    private readonly ILogger<ProposalsBackgroundAgentCommand> _logger;

    public ProposalsBackgroundAgentCommand(
        IChangeProposalStore store,
        ILogger<ProposalsBackgroundAgentCommand> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<int> ListAsync(string? status, string? targetPrefix, bool formatJson, CancellationToken ct = default)
        => ListAsync(status, targetPrefix, formatJson, Console.Out, Console.Error, ct);

    public Task<int> ListAsync(
        string? status,
        string? targetPrefix,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            ChangeProposalStatus? filter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ChangeProposalStatus>(status, ignoreCase: true, out var parsed))
                {
                    var allowed = string.Join(", ", Enum.GetNames<ChangeProposalStatus>());
                    var msg = $"Unknown --status '{status}'. Allowed: {allowed}.";
                    if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                    else stderr.WriteLine(msg);
                    return Task.FromResult(2);
                }
                filter = parsed;
            }

            var rows = _store.List(filter)
                .Where(p => string.IsNullOrWhiteSpace(targetPrefix) ||
                            p.TargetPath.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => (int)p.Status)
                .ThenByDescending(p => p.UpdatedAt)
                .ToList();

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, path = _store.Location, count = rows.Count, proposals = rows.Select(p => Project(p)) },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Source: {_store.Location}");
            if (rows.Count == 0) { stdout.WriteLine("No matching proposals."); return Task.FromResult(0); }
            stdout.WriteLine($"Proposals: {rows.Count}");
            stdout.WriteLine();
            stdout.WriteLine("Id                                Status     Target                                   Updated(UTC)         Summary");
            stdout.WriteLine("--------------------------------- ---------- ---------------------------------------- -------------------- -------------------------------------------");
            foreach (var p in rows)
            {
                stdout.WriteLine(
                    $"{Truncate(p.Id, 33),-33} {p.Status,-10} {Truncate(p.TargetPath, 40),-40} {p.UpdatedAt:u}   {Truncate(p.Summary, 50)}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("List proposals failed", ex, formatJson, stdout, stderr);
        }
    }

    public Task<int> ShowAsync(string id, bool showDiff, bool formatJson, CancellationToken ct = default)
        => ShowAsync(id, showDiff, formatJson, Console.Out, Console.Error, ct);

    public Task<int> ShowAsync(string id, bool showDiff, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var p = _store.Find(id);
            if (p is null)
            {
                var msg = $"Proposal '{id}' not found.";
                if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                else stderr.WriteLine(msg);
                return Task.FromResult(1);
            }

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, proposal = Project(p, includeContent: showDiff) },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Id:          {p.Id}");
            stdout.WriteLine($"Status:      {p.Status}");
            stdout.WriteLine($"Target:      {p.TargetPath}");
            stdout.WriteLine($"Summary:     {p.Summary}");
            if (!string.IsNullOrWhiteSpace(p.Reason)) stdout.WriteLine($"Reason:      {p.Reason}");
            if (!string.IsNullOrWhiteSpace(p.AgentId)) stdout.WriteLine($"Agent:       {p.AgentId}");
            if (!string.IsNullOrWhiteSpace(p.ObjectiveId)) stdout.WriteLine($"Objective:   {p.ObjectiveId}");
            if (!string.IsNullOrWhiteSpace(p.BaseSha256)) stdout.WriteLine($"Base SHA:    {p.BaseSha256}");
            if (!string.IsNullOrWhiteSpace(p.ResolvedBy)) stdout.WriteLine($"Resolved by: {p.ResolvedBy}");
            if (!string.IsNullOrWhiteSpace(p.ResolutionNote)) stdout.WriteLine($"Note:        {p.ResolutionNote}");
            stdout.WriteLine($"Created:     {p.CreatedAt:u}");
            stdout.WriteLine($"Updated:     {p.UpdatedAt:u}");
            if (showDiff)
            {
                stdout.WriteLine();
                stdout.WriteLine("--- Proposed content ---");
                stdout.WriteLine(p.NewContent);
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Show proposal failed", ex, formatJson, stdout, stderr);
        }
    }

    public Task<int> ApproveAsync(string id, string? approver, string? note, bool formatJson, CancellationToken ct = default)
        => Transition(_store.Approve, id, approver, note, "approved", formatJson);

    public Task<int> RejectAsync(string id, string? reviewer, string? note, bool formatJson, CancellationToken ct = default)
        => Transition(_store.Reject, id, reviewer, note, "rejected", formatJson);

    /// <summary>
    /// Apply an Approved proposal: write its <c>NewContent</c> to disk and then
    /// transition to <c>Applied</c>. Re-reads the file and verifies the recorded
    /// <c>BaseSha256</c> still matches before writing — bails with a structured
    /// error on drift so the operator can re-review. Optional verification: <paramref name="verifyBuild"/>
    /// runs <c>dotnet build -c Release</c>; <paramref name="verifyTest"/> runs that build then
    /// <c>dotnet test</c> (TRX, <c>--no-build</c>). Exit **4** if the build fails, **5** if tests fail after a successful build (file remains applied).
    /// </summary>
    public Task<int> ApplyAsync(
        string id,
        string repoRoot,
        bool force,
        bool formatJson,
        bool verifyBuild = false,
        bool verifyTest = false,
        CancellationToken ct = default)
        => ApplyAsync(id, repoRoot, force, formatJson, verifyBuild, verifyTest, Console.Out, Console.Error, ct);

    public async Task<int> ApplyAsync(
        string id,
        string repoRoot,
        bool force,
        bool formatJson,
        bool verifyBuild,
        bool verifyTest,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            var p = _store.Find(id) ?? throw new InvalidOperationException($"Proposal '{id}' not found.");
            if (p.Status != ChangeProposalStatus.Approved)
                throw new InvalidOperationException($"Proposal '{id}' is {p.Status}; expected Approved before apply.");
            if (string.IsNullOrWhiteSpace(repoRoot))
                throw new ArgumentException("--repo-root is required");

            var fullPath = Path.GetFullPath(Path.Combine(repoRoot, p.TargetPath));
            // Drift check: refuse to overwrite if file changed since the proposal
            // was raised, unless the operator passes --force.
            if (!force && !string.IsNullOrWhiteSpace(p.BaseSha256) && File.Exists(fullPath))
            {
                using var stream = File.OpenRead(fullPath);
                var current = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
                if (!string.Equals(current, p.BaseSha256, StringComparison.OrdinalIgnoreCase))
                {
                    var msg = $"Drift detected: {p.TargetPath} sha changed since proposal (was {p.BaseSha256[..12]}…, now {current[..12]}…). Re-review or pass --force.";
                    if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, id, error = msg }));
                    else stderr.WriteLine(msg);
                    return 3;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, p.NewContent);
            var applied = _store.MarkApplied(id, note: $"applied to {fullPath}");

            var needBuild = verifyBuild || verifyTest;
            if (!needBuild)
            {
                if (formatJson)
                    stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "applied", proposal = Project(applied) }));
                else
                    stdout.WriteLine($"Applied proposal '{applied.Id}' → {fullPath}");
                return 0;
            }

            if (!formatJson)
                stdout.WriteLine($"Applied proposal '{applied.Id}' → {fullPath}");

            var (buildCode, buildOut, buildErr, buildTimedOut) = await DotnetBuildTool.RunReleaseBuildAsync(repoRoot, ct)
                .ConfigureAwait(false);
            var buildOk = buildCode == 0 && !buildTimedOut;
            object buildObj = new
            {
                ok = buildOk,
                exit_code = buildCode,
                timed_out = buildTimedOut,
                stdout = buildOut,
                stderr = buildErr
            };

            if (!buildOk)
            {
                if (formatJson)
                {
                    stdout.WriteLine(JsonSerializer.Serialize(new
                    {
                        ok = false,
                        action = "applied",
                        proposal = Project(applied),
                        build = buildObj
                    }));
                }
                else
                {
                    stdout.WriteLine(buildTimedOut ? "dotnet build -c Release: timed out." : $"dotnet build -c Release: failed (exit {buildCode}).");
                    if (!string.IsNullOrWhiteSpace(buildErr)) stdout.WriteLine(buildErr);
                }

                return 4;
            }

            if (!verifyTest)
            {
                if (formatJson)
                {
                    stdout.WriteLine(JsonSerializer.Serialize(new
                    {
                        ok = true,
                        action = "applied",
                        proposal = Project(applied),
                        build = buildObj
                    }));
                }
                else
                    stdout.WriteLine("dotnet build -c Release: succeeded.");

                return 0;
            }

            var (testCode, testOut, testErr, testTimedOut) = await DotnetTestTool.RunTrxTestsNoBuildAsync(repoRoot, ct)
                .ConfigureAwait(false);
            var testOk = testCode == 0 && !testTimedOut;
            object testObj = new
            {
                ok = testOk,
                exit_code = testCode,
                timed_out = testTimedOut,
                stdout = testOut,
                stderr = testErr
            };
            var overallOk = testOk;

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = overallOk,
                    action = "applied",
                    proposal = Project(applied),
                    build = buildObj,
                    test = testObj
                }));
            }
            else
            {
                stdout.WriteLine("dotnet build -c Release: succeeded.");
                if (testOk)
                    stdout.WriteLine("dotnet test: succeeded.");
                else
                {
                    stdout.WriteLine(testTimedOut ? "dotnet test: timed out." : $"dotnet test: failed (exit {testCode}).");
                    if (!string.IsNullOrWhiteSpace(testErr)) stdout.WriteLine(testErr);
                }
            }

            return overallOk ? 0 : 5;
        }
        catch (Exception ex)
        {
            return await Fail("Apply proposal failed", ex, formatJson, stdout, stderr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Run <c>dotnet build -c Release</c> from <paramref name="repoRoot"/> (operator convenience
    /// aligned with <c>forge.build</c> / <c>dotnet.build</c> agent tools).
    /// </summary>
    public Task<int> BuildAsync(string repoRoot, bool formatJson, CancellationToken ct = default)
        => BuildAsync(repoRoot, formatJson, Console.Out, Console.Error, ct);

    public async Task<int> BuildAsync(
        string repoRoot,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
                throw new ArgumentException("--repo-root is required");

            var (code, outText, errText, timedOut) = await DotnetBuildTool.RunReleaseBuildAsync(repoRoot, ct)
                .ConfigureAwait(false);
            var ok = code == 0 && !timedOut;
            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(new
                {
                    ok,
                    exit_code = code,
                    timed_out = timedOut,
                    stdout = outText,
                    stderr = errText
                }));
            }
            else
            {
                if (ok) stdout.WriteLine("Build succeeded.");
                else stdout.WriteLine(timedOut ? "Build timed out." : $"Build failed (exit {code}).");
                if (!string.IsNullOrWhiteSpace(outText)) stdout.WriteLine(outText);
                if (!string.IsNullOrWhiteSpace(errText)) stderr.WriteLine(errText);
            }

            return ok ? 0 : 1;
        }
        catch (Exception ex)
        {
            return await Fail("Build failed", ex, formatJson, stdout, stderr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs <c>dotnet build -c Release</c> then the same TRX <c>dotnet test --no-build</c> as
    /// <see cref="DotnetTestTool"/> (aligned with <c>forge.test</c> / <c>dotnet.test</c>).
    /// </summary>
    public Task<int> TestAsync(string repoRoot, bool formatJson, CancellationToken ct = default)
        => TestAsync(repoRoot, formatJson, Console.Out, Console.Error, ct);

    public async Task<int> TestAsync(
        string repoRoot,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
                throw new ArgumentException("--repo-root is required");

            var (bCode, bOut, bErr, bTimedOut) = await DotnetBuildTool.RunReleaseBuildAsync(repoRoot, ct).ConfigureAwait(false);
            var buildOk = bCode == 0 && !bTimedOut;
            object buildObj = new
            {
                ok = buildOk,
                exit_code = bCode,
                timed_out = bTimedOut,
                stdout = bOut,
                stderr = bErr
            };

            if (!buildOk)
            {
                if (formatJson)
                    stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, build = buildObj, test = (object?)null }));
                else
                {
                    stdout.WriteLine(bTimedOut ? "Build timed out." : $"Build failed (exit {bCode}).");
                    if (!string.IsNullOrWhiteSpace(bErr)) stderr.WriteLine(bErr);
                }

                return 1;
            }

            var (tCode, tOut, tErr, tTimedOut) = await DotnetTestTool.RunTrxTestsNoBuildAsync(repoRoot, ct).ConfigureAwait(false);
            var testOk = tCode == 0 && !tTimedOut;
            object testObj = new
            {
                ok = testOk,
                exit_code = tCode,
                timed_out = tTimedOut,
                stdout = tOut,
                stderr = tErr
            };
            var ok = testOk;

            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok, build = buildObj, test = testObj }));
            else
            {
                stdout.WriteLine("Build succeeded.");
                if (testOk)
                    stdout.WriteLine("Tests succeeded.");
                else
                {
                    stdout.WriteLine(tTimedOut ? "Tests timed out." : $"Tests failed (exit {tCode}).");
                    if (!string.IsNullOrWhiteSpace(tErr)) stderr.WriteLine(tErr);
                }
            }

            return ok ? 0 : 1;
        }
        catch (Exception ex)
        {
            return await Fail("Test failed", ex, formatJson, stdout, stderr).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Run the janitor sweep once: anything past its TTL moves to Stale. The
    /// sweep is idempotent and safe to invoke from cron / a CI job.
    /// </summary>
    public Task<int> JanitorAsync(double? proposedTtlHours, double? approvedTtlHours, bool formatJson, CancellationToken ct = default)
        => JanitorAsync(proposedTtlHours, approvedTtlHours, formatJson, Console.Out, Console.Error, ct);

    public Task<int> JanitorAsync(double? proposedTtlHours, double? approvedTtlHours, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var janitor = new ProposalJanitor(
                _store,
                proposedTtl: proposedTtlHours is > 0 ? TimeSpan.FromHours(proposedTtlHours.Value) : (TimeSpan?)null,
                approvedTtl: approvedTtlHours is > 0 ? TimeSpan.FromHours(approvedTtlHours.Value) : (TimeSpan?)null);
            var staled = janitor.Sweep();
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, staled, count = staled.Count },
                    new JsonSerializerOptions { WriteIndented = true }));
            else
                stdout.WriteLine($"Staled {staled.Count} proposal(s).{(staled.Count == 0 ? "" : " ids=" + string.Join(", ", staled))}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Proposal janitor failed", ex, formatJson, stdout, stderr);
        }
    }

    public Task<int> StatsAsync(bool formatJson, CancellationToken ct = default)
        => StatsAsync(formatJson, Console.Out, Console.Error, ct);

    public Task<int> StatsAsync(bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var all = _store.List();
            var byStatus = Enum.GetValues<ChangeProposalStatus>()
                .ToDictionary(s => s.ToString(), s => all.Count(p => p.Status == s));

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, path = _store.Location, total = all.Count, byStatus },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Source: {_store.Location}");
            stdout.WriteLine($"Total proposals: {all.Count}");
            stdout.WriteLine();
            stdout.WriteLine("Status     Count");
            stdout.WriteLine("---------- ------");
            foreach (var (s, c) in byStatus)
                stdout.WriteLine($"{s,-10} {c,6}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Proposals stats failed", ex, formatJson, stdout, stderr);
        }
    }

    private Task<int> Transition(
        Func<string, string?, string?, ChangeProposal> verb,
        string id, string? actor, string? note, string action, bool formatJson)
    {
        try
        {
            var saved = verb(id, actor, note);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, action, proposal = Project(saved) }));
            else
                Console.Out.WriteLine($"{action[..1].ToUpperInvariant()}{action[1..]} proposal '{saved.Id}'.");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail($"Proposal {action} failed", ex, formatJson, Console.Out, Console.Error);
        }
    }

    private Task<int> Fail(string label, Exception ex, bool formatJson, TextWriter stdout, TextWriter stderr)
    {
        _logger.LogError(ex, "{Label}", label);
        if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        else stderr.WriteLine(ex.Message);
        return Task.FromResult(1);
    }

    private static object Project(ChangeProposal p, bool includeContent = false) => includeContent
        ? (object)new
        {
            id = p.Id, status = p.Status.ToString(),
            target_path = p.TargetPath,
            summary = p.Summary, reason = p.Reason,
            agent_id = p.AgentId, objective_id = p.ObjectiveId,
            base_sha256 = p.BaseSha256, resolved_by = p.ResolvedBy, resolution_note = p.ResolutionNote,
            created_at = p.CreatedAt, updated_at = p.UpdatedAt,
            new_content = p.NewContent
        }
        : new
        {
            id = p.Id, status = p.Status.ToString(),
            target_path = p.TargetPath,
            summary = p.Summary, reason = p.Reason,
            agent_id = p.AgentId, objective_id = p.ObjectiveId,
            base_sha256 = p.BaseSha256, resolved_by = p.ResolvedBy, resolution_note = p.ResolutionNote,
            created_at = p.CreatedAt, updated_at = p.UpdatedAt
        };

    private static string Truncate(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= len ? s : s.Substring(0, len - 1) + "…";
    }
}
