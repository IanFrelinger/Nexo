using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdapterProvenanceTests
{
    [Fact]
    public void Prepend_is_greppable_and_idempotent()
    {
        var code = "struct adapted_reader {};\n";
        var once = AdapterProvenance.Prepend(code, new AdapterProvenance.Info(
            "model", "qwen2.5-coder:7b", "sha256:abc", true, "pass", "oracle"));
        once.Should().Contain("NEXO_ADAPTER_PROVENANCE_BEGIN");
        once.Should().Contain("strategy: model");
        once.Should().Contain("model_digest: sha256:abc");
        once.Should().Contain("reviewed_by: oracle");
        var twice = AdapterProvenance.Prepend(once, new AdapterProvenance.Info(
            "model", "x", "y", false, "fail", "human"));
        twice.Should().Be(once);

        var patched = AdapterProvenance.UpdateOracle(once, "fail", "human");
        patched.Should().Contain("oracle: fail");
        patched.Should().Contain("reviewed_by: human");
    }
}
