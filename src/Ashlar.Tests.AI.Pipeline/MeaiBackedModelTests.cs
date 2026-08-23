using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.AI.Pipeline;
using Ashlar.AI.Pipeline.Clients;
using Ashlar.AI.Pipeline.Governance;
using Ashlar.AI.Pipeline.Models;
using Xunit;

namespace Ashlar.Tests.AI.Pipeline;

public sealed class MeaiBackedModelTests
{
    [Fact]
    public async Task CompleteAsync_routes_through_governed_chat_client()
    {
        var spy = new SpyChatClient("meai-ok");
        var auditor = new InMemoryChatInvocationAuditor();
        var services = new ServiceCollection();
        services.AddSingleton<IChatInvocationAuditor>(auditor);
        services.AddAshlarMeaiPipeline(
            ollamaInnerFactory: _ => spy,
            onnxInnerFactory: _ => new FakeChatClient("onnx"),
            registerDefaultRouter: true);

        await using var provider = services.BuildServiceProvider();
        var model = new MeaiBackedModel(
            provider.GetRequiredService<IChatClient>(),
            NullLogger<MeaiBackedModel>.Instance);

        var output = await model.CompleteAsync(
            new ModelInput(new[] { ("user", "hello from IModel") }),
            CancellationToken.None);

        output.Text.Should().Be("meai-ok");
        spy.CallCount.Should().Be(1);
        auditor.Records.Should().Contain(r => r.Outcome == "success");
    }

    [Fact]
    public async Task Offline_provider_throws_for_hot_swap_fallback()
    {
        var model = new MeaiBackedModel(new FakeChatClient("unused"), NullLogger<MeaiBackedModel>.Instance);
        var act = async () => await model.CompleteAsync(
            new ModelInput(new[]
            {
                ("system", "ashlar.model.provider=offline"),
                ("user", "hi"),
            }),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deterministic/offline*");
    }
}
