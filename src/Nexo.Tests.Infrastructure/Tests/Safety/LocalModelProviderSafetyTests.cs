using FluentAssertions;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Safety;

/// <summary>
/// Safety tests for LocalModelProvider.
/// Ensures explicit failure when model is unavailable — no silent fallback.
///
/// LocalModelProvider_InAirGappedMode_DoesNotAttemptNetworkCall — intentionally not implemented.
/// LlamaSharp performs in-process inference from a GGUF file on disk.
/// It does not use HttpClient or any network stack. Network isolation
/// is enforced at the OS/container level (--network none) via make test-airgapped.
/// Verified by: test-air-gapped-no-network.yml CI workflow.
/// </summary>
[Trait("Category", "Safety")]
[Trait("Category", "Unit")]
public sealed class LocalModelProviderSafetyTests
{
    [Fact]
    public void LocalModelProvider_WhenModelFileNotFound_IsAvailableReturnsFalse()
    {
        var previous = Environment.GetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", "/nonexistent/model.gguf");

            var available = LocalModelProvider.IsAvailable();

            available.Should().BeFalse("nonexistent model path must report unavailable");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", previous);
        }
    }

    [Fact]
    public async Task LocalModelProvider_WhenModelFileNotFound_ExecuteAsyncThrowsExplicitly()
    {
        var previous = Environment.GetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", "/nonexistent/model.gguf");

            var act = () => LocalModelProvider.ExecuteAsync("sys", "user", null, default);

            await act.Should().ThrowAsync<ModelUnavailableException>()
                .WithMessage("*Local model not configured*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", previous);
        }
    }

    [Fact]
    public void LocalModelProvider_WhenPathNotSet_IsAvailableReturnsFalse()
    {
        var previous = Environment.GetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", null);

            var available = LocalModelProvider.IsAvailable();

            available.Should().BeFalse("unset path must report unavailable");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", previous);
        }
    }
}
