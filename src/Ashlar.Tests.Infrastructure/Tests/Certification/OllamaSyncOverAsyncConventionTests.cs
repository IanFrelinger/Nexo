using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Ollama construction and <c>IsProviderAvailable("ollama")</c> must not block
/// the caller on <c>/api/tags</c>. That used to
/// <c>.GetAwaiter().GetResult()</c> on ASP.NET request threads.
/// </summary>
[Trait("Category", "Certification")]
public sealed class OllamaSyncOverAsyncConventionTests
{
    [Fact]
    public void OllamaProvider_DoesNotBlockOnGetResult()
    {
        var text = Read("src/Ashlar.Infrastructure/Execution/Ollama/OllamaProvider.cs");
        text.Should().NotContain(
            "RefreshModelsAsync(CancellationToken.None).GetAwaiter().GetResult()",
            "OllamaProvider construction must not block on /api/tags");
    }

    [Fact]
    public void ProviderFactory_OllamaAvailability_DoesNotBlockOnGetResult()
    {
        var text = Read("src/Ashlar.Infrastructure/Execution/ProviderFactory.cs");
        text.Should().NotContain(
            "GetOllamaBaseUrlAsync(CancellationToken.None).GetAwaiter().GetResult()");
        text.Should().NotContain(
            "CheckHealthAsync(CancellationToken.None).GetAwaiter().GetResult()");
    }

    private static string Read(string relative)
    {
        var root = RepoPathResolver.FindRepoRoot();
        return File.ReadAllText(Path.Combine(root, relative));
    }
}
