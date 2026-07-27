using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdapterContentScanTests : IDisposable
{
    private readonly string? _allow;
    private readonly string? _reason;

    public AdapterContentScanTests()
    {
        _allow = Environment.GetEnvironmentVariable(AdapterContentScan.AllowUnsafeEnv);
        _reason = Environment.GetEnvironmentVariable(AdapterContentScan.UnsafeReasonEnv);
        Environment.SetEnvironmentVariable(AdapterContentScan.AllowUnsafeEnv, null);
        Environment.SetEnvironmentVariable(AdapterContentScan.UnsafeReasonEnv, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AdapterContentScan.AllowUnsafeEnv, _allow);
        Environment.SetEnvironmentVariable(AdapterContentScan.UnsafeReasonEnv, _reason);
    }

    [Fact]
    public void Scaffold_path_is_not_scanned()
    {
        var r = AdapterContentScan.Scan("system(\"rm -rf /\");", isModelAuthored: false);
        r.Ok.Should().BeTrue();
    }

    [Fact]
    public void Model_system_call_is_blocked()
    {
        var r = AdapterContentScan.Scan("void f(){ system(\"id\"); }", isModelAuthored: true);
        r.Ok.Should().BeFalse();
        r.Hits.Should().Contain(h => h.Rule.Contains("system"));
    }

    [Fact]
    public void Override_requires_reason()
    {
        Environment.SetEnvironmentVariable(AdapterContentScan.AllowUnsafeEnv, "1");
        var r = AdapterContentScan.Scan("fork();", isModelAuthored: true);
        r.Ok.Should().BeFalse();

        Environment.SetEnvironmentVariable(AdapterContentScan.UnsafeReasonEnv, "lab only");
        r = AdapterContentScan.Scan("fork();", isModelAuthored: true);
        r.Ok.Should().BeTrue();
        r.OverrideUsed.Should().BeTrue();
    }
}
