using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Infrastructure.Skills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class NexoScriptRunnerTests
{
    [Fact]
    public async Task RunAsync_rejects_script_not_on_allow_list()
    {
        var runner = new NexoScriptRunner(
            new LocalProcessScriptSandbox(),
            new DenyScriptsPolicyEvaluator());

        using var temp = new TempScriptRoot();
        var scriptPath = temp.WriteScript("echo ok");

        var result = await runner.RunAsync(
            "skill",
            "scripts/run.sh",
            scriptPath,
            "hash",
            null,
            new NexoScriptRunOptions(TimeSpan.FromSeconds(5), 4096, temp.WorkRoot),
            new NexoSkillExecutionContext("tester", "internal", PeerTrustTier.Trusted, "pack", "corr"));

        result.Outcome.Should().Be("denied");
    }

    [Fact]
    public async Task RunAsync_truncates_output_when_over_cap()
    {
        var runner = new NexoScriptRunner(
            new LocalProcessScriptSandbox(),
            new AllowAllScriptsPolicyEvaluator());

        using var temp = new TempScriptRoot();
        var scriptPath = temp.WriteScript("python3 - <<'PY'\nprint('x' * 5000)\nPY");

        var result = await runner.RunAsync(
            "skill",
            "scripts/run.sh",
            scriptPath,
            "hash",
            null,
            new NexoScriptRunOptions(TimeSpan.FromSeconds(10), 128, temp.WorkRoot),
            new NexoSkillExecutionContext("tester", "internal", PeerTrustTier.Trusted, "pack", "corr"));

        result.OutputTruncated.Should().BeTrue();
        result.Outcome.Should().Be("truncated");
    }

    [Fact]
    public async Task RunAsync_applies_timeout_from_options()
    {
        var sandbox = new RecordingSandbox(timeoutExitCode: 124);
        var runner = new NexoScriptRunner(sandbox, new AllowAllScriptsPolicyEvaluator());

        using var temp = new TempScriptRoot();
        var scriptPath = temp.WriteScript("echo ok");

        var result = await runner.RunAsync(
            "skill",
            "scripts/run.sh",
            scriptPath,
            "hash",
            null,
            new NexoScriptRunOptions(TimeSpan.FromMilliseconds(500), 4096, temp.WorkRoot),
            new NexoSkillExecutionContext("tester", "internal", PeerTrustTier.Trusted, "pack", "corr"));

        result.ExitCode.Should().Be(124);
        sandbox.ReceivedTimeout.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    private sealed class AllowAllScriptsPolicyEvaluator : INexoSkillPolicyEvaluator
    {
        public bool IsSkillVisible(NexoSkillDescriptor skill, NexoSkillExecutionContext context) => true;
        public bool IsScriptAllowed(string skillName, string scriptPath, NexoSkillExecutionContext context) => true;
        public bool IsScriptAutoApproved(string skillName, string scriptPath, NexoSkillExecutionContext context) => false;
        public NexoScriptRunOptions GetScriptLimits(NexoSkillExecutionContext context) => new(TimeSpan.FromSeconds(10), 4096, Path.GetTempPath());
        public SkillPolicyRules? GetActiveSkillRules() => null;
    }

    private sealed class DenyScriptsPolicyEvaluator : INexoSkillPolicyEvaluator
    {
        public bool IsSkillVisible(NexoSkillDescriptor skill, NexoSkillExecutionContext context) => true;
        public bool IsScriptAllowed(string skillName, string scriptPath, NexoSkillExecutionContext context) => false;
        public bool IsScriptAutoApproved(string skillName, string scriptPath, NexoSkillExecutionContext context) => false;
        public NexoScriptRunOptions GetScriptLimits(NexoSkillExecutionContext context) => new(TimeSpan.FromSeconds(10), 4096, Path.GetTempPath());
        public SkillPolicyRules? GetActiveSkillRules() => null;
    }

    private sealed class TempScriptRoot : IDisposable
    {
        public string WorkRoot { get; } = Path.Combine(Path.GetTempPath(), "nexo-script-" + Guid.NewGuid().ToString("N"));

        public TempScriptRoot() => Directory.CreateDirectory(WorkRoot);

        public string WriteScript(string body)
        {
            var path = Path.Combine(WorkRoot, "run.sh");
            File.WriteAllText(path, "#!/usr/bin/env bash\n" + body + "\n");
            return path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(WorkRoot))
                    Directory.Delete(WorkRoot, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class RecordingSandbox(int timeoutExitCode) : INexoScriptSandbox
    {
        public TimeSpan ReceivedTimeout { get; private set; }

        public Task<SandboxScriptResult> RunAsync(
            string scriptAbsolutePath,
            string workingDirectory,
            TimeSpan timeout,
            int maxOutputBytes,
            CancellationToken cancellationToken = default)
        {
            ReceivedTimeout = timeout;
            return Task.FromResult(new SandboxScriptResult(
                string.Empty,
                "timed out",
                timeoutExitCode,
                false,
                timeout));
        }
    }
}
