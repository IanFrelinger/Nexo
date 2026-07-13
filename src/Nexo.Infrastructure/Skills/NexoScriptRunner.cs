using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Skills;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Routes skill script execution through Nexo execution-target selection with local sandbox enforcement.
/// </summary>
public sealed class NexoScriptRunner : INexoScriptRunner
{
    private readonly INexoScriptSandbox _sandbox;
    private readonly INexoSkillPolicyEvaluator _policyEvaluator;
    private readonly ICapabilityRouter? _capabilityRouter;

    /// <summary>Initializes a new Nexo script runner.</summary>
    public NexoScriptRunner(
        INexoScriptSandbox sandbox,
        INexoSkillPolicyEvaluator policyEvaluator,
        ICapabilityRouter? capabilityRouter = null)
    {
        _sandbox = sandbox;
        _policyEvaluator = policyEvaluator;
        _capabilityRouter = capabilityRouter;
    }

    /// <inheritdoc />
    public async Task<NexoScriptRunResult> RunAsync(
        string skillName,
        string scriptPath,
        string scriptAbsolutePath,
        string scriptContentHash,
        JsonElement? args,
        NexoScriptRunOptions options,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_policyEvaluator.IsScriptAllowed(skillName, scriptPath, context))
        {
            return new NexoScriptRunResult(
                Output: "Script is not on the policy allow-list.",
                ExitCode: 1,
                Duration: TimeSpan.Zero,
                OutputTruncated: false,
                ScriptContentHash: scriptContentHash,
                Outcome: "denied");
        }

        if (!File.Exists(scriptAbsolutePath))
        {
            return new NexoScriptRunResult(
                Output: $"Script not found: {scriptAbsolutePath}",
                ExitCode: 1,
                Duration: TimeSpan.Zero,
                OutputTruncated: false,
                ScriptContentHash: scriptContentHash,
                Outcome: "failure");
        }

        var hash = string.IsNullOrWhiteSpace(scriptContentHash)
            ? SkillContentHasher.ComputeFileHash(scriptAbsolutePath)
            : scriptContentHash;

        var target = _capabilityRouter?.ResolveExecutionTarget(new JobRequirements
        {
            MinimumVramBytes = 0,
            ComputeClass = GpuComputeClass.Low,
            RemoteExecutionPreference = RemoteExecutionPreference.UseSystemDefault,
        });

        if (target is ExecutionTarget.Remote)
        {
            return new NexoScriptRunResult(
                Output: "Remote skill script execution is not supported in this phase.",
                ExitCode: 1,
                Duration: TimeSpan.Zero,
                OutputTruncated: false,
                ScriptContentHash: hash,
                Outcome: "denied");
        }

        var workingDirectory = CreateIsolatedWorkingDirectory(options.WorkingDirectoryRoot, skillName, hash);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            WriteArgsFile(workingDirectory, args);
            var sandboxResult = await _sandbox.RunAsync(
                scriptAbsolutePath,
                workingDirectory,
                options.Timeout,
                options.MaxOutputBytes,
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var output = CombineOutput(sandboxResult.StandardOutput, sandboxResult.StandardError);
            var outcome = sandboxResult.ExitCode == 0
                ? sandboxResult.OutputTruncated ? "truncated" : "success"
                : "failure";

            return new NexoScriptRunResult(
                Output: output,
                ExitCode: sandboxResult.ExitCode,
                Duration: stopwatch.Elapsed,
                OutputTruncated: sandboxResult.OutputTruncated,
                ScriptContentHash: hash,
                Outcome: outcome);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    private static string CreateIsolatedWorkingDirectory(string root, string skillName, string hash)
    {
        var directory = Path.Combine(
            root,
            "nexo-skill-runs",
            Sanitize(skillName),
            hash[..Math.Min(12, hash.Length)]);

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteArgsFile(string workingDirectory, JsonElement? args)
    {
        if (args == null)
            return;

        var path = Path.Combine(workingDirectory, "args.json");
        File.WriteAllText(path, args.Value.GetRawText());
    }

    private static string CombineOutput(string stdout, string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return stdout;
        if (string.IsNullOrWhiteSpace(stdout))
            return stderr;
        return stdout + Environment.NewLine + stderr;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        return builder.ToString();
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
