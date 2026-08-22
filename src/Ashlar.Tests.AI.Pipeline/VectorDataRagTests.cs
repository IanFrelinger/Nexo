using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.AI.Pipeline;
using Ashlar.AI.Pipeline.Clients;
using Ashlar.AI.Pipeline.Governance;
using Ashlar.AI.Pipeline.Rag;
using Xunit;

namespace Ashlar.Tests.AI.Pipeline;

public sealed class VectorDataRagTests
{
    [Fact]
    public async Task Round_trip_index_and_search()
    {
        await using var provider = BuildProvider();
        var rag = provider.GetRequiredService<VectorDataRagService>();

        await rag.IndexAsync("a", "alpha wolverine protocol", trustTier: "Public");
        await rag.IndexAsync("b", "beta unrelated notes", trustTier: "Public");

        var hits = await rag.SearchAsync("wolverine protocol", callerMaxTrustTier: "Internal", top: 5);
        hits.Should().NotBeEmpty();
        hits[0].Record.Key.Should().Be("a");
    }

    [Fact]
    public async Task Trust_tier_filter_excludes_higher_tier_chunks()
    {
        await using var provider = BuildProvider();
        var rag = provider.GetRequiredService<VectorDataRagService>();

        await rag.IndexAsync("pub", "shared playbook guidance", trustTier: "Public");
        await rag.IndexAsync("sec", "shared playbook guidance", trustTier: "Secret");

        var hits = await rag.SearchAsync("playbook guidance", callerMaxTrustTier: "Internal", top: 10);
        hits.Should().Contain(h => h.Record.Key == "pub");
        hits.Should().NotContain(h => h.Record.Key == "sec");
    }

    [Fact]
    public async Task Embedding_calls_appear_in_audit()
    {
        var auditor = new InMemoryChatInvocationAuditor();
        await using var provider = BuildProvider(auditor);
        var rag = provider.GetRequiredService<VectorDataRagService>();

        await rag.IndexAsync("1", "audited embedding content with sk-TESTSECRETKEY12345678", trustTier: "Public");
        await rag.SearchAsync("audited embedding", callerMaxTrustTier: "Public");

        auditor.Records.Should().Contain(r => r.TargetKey == "embed:local" && r.Outcome == "success");
        auditor.Records.Should().Contain(r => r.TargetKey == "rag:search" && r.Outcome == "success");
        auditor.Records.Should().Contain(r => r.TargetKey == "embed:local" && r.RedactionCount >= 1);
    }

    [Fact]
    public async Task Reindex_utility_writes_all_chunks()
    {
        await using var provider = BuildProvider();
        var rag = provider.GetRequiredService<VectorDataRagService>();

        var count = await rag.ReindexAsync(new[]
        {
            ("1", "one", (string?)null, "Public"),
            ("2", "two", "file://x", "Internal"),
        });

        count.Should().Be(2);
        var hits = await rag.SearchAsync("one", "Public");
        hits.Should().Contain(h => h.Record.Key == "1");
    }

    private static ServiceProvider BuildProvider(InMemoryChatInvocationAuditor? auditor = null)
    {
        var services = new ServiceCollection();
        if (auditor is not null)
        {
            services.AddSingleton<IChatInvocationAuditor>(auditor);
        }

        services.AddAshlarMeaiPipeline(
            ollamaInnerFactory: _ => new FakeChatClient(),
            onnxInnerFactory: _ => new FakeChatClient(),
            registerDefaultRouter: false);
        return services.BuildServiceProvider();
    }
}
