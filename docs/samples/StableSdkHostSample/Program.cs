using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Hosting;
using Nexo.Hosting.Sdk;

var services = new ServiceCollection();

services.AddNexoSdk(sdk => sdk
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

services.AddNexo(options =>
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
        Name = "Sample Host Brick";
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
