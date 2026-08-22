using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Hosting;
using Ashlar.Hosting.Sdk;
using Ashlar.Hosting.Sdk.Extensions;

var services = new ServiceCollection();

services.AddAshlarSdk(sdk => sdk
    .RegisterBrick<SampleHostBrick>()
    .RegisterAgent<SampleHostAgent>()
    .RegisterAgentCard(new AgentCard
    {
        Id = "sample-host-agent",
        Name = "Sample Host Agent",
        Domain = "samples",
        Description = "Reference agent card for stable host-side SDK integration.",
        Behaviors = ["analyze", "summarize"]
    }));

services.AddAshlar(options =>
{
    options.RegisterBackgroundAgentHostedService = false;
});

using var provider = services.BuildServiceProvider();
Console.WriteLine("Stable SDK host sample bootstrapped successfully.");

public sealed class SampleHostBrick : Brick
{
    public SampleHostBrick()
    {
        Id = "sample.host.brick";
        Name = "Sample Host DomainBrick";
        Description = "Sample stable-host SDK brick.";
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BrickOutput { Summary = "Sample brick executed." });
    }
}

public sealed class SampleHostAgent : IAgent
{
    public string Name => "sample-host-agent";

    public Task<AgentActions> ThinkAsync(
        AgentObservation obs,
        IToolbox tools,
        IAgentMemory mem,
        CancellationToken ct)
    {
        return Task.FromResult(AgentActions.None);
    }
}
