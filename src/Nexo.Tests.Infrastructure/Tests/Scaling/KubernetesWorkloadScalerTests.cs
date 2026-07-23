using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Scaling.Models;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Scaling;

public sealed class KubernetesWorkloadScalerTests
{
    [Fact]
    public async Task ScaleAsync_InvokesKubectlScaleWithClampedReplicas()
    {
        var runner = new FakeRunner((_, _) =>
            Task.FromResult(new ProcessCommandResult(0, "deployment.apps/nexo-mesh-worker scaled", "")));
        var sut = CreateScaler(runner, min: 1, max: 5);

        var result = await sut.ScaleAsync(new WorkloadScaleRequest("mesh-worker", 99, "test"));

        result.Success.Should().BeTrue();
        result.DesiredReplicas.Should().Be(5);
        runner.LastArgs.Should().Contain("scale");
        runner.LastArgs.Should().Contain("deployment/nexo-mesh-worker");
        runner.LastArgs.Should().Contain("--replicas=5");
        runner.LastArgs.Should().Contain("-n");
        runner.LastArgs.Should().Contain("nexo");
    }

    [Fact]
    public async Task GetReplicasAsync_ParsesJsonPathCsv()
    {
        var runner = new FakeRunner((_, _) =>
            Task.FromResult(new ProcessCommandResult(0, "3,2,2", "")));
        var sut = CreateScaler(runner);

        var snap = await sut.GetReplicasAsync("mesh-worker");

        snap.DesiredReplicas.Should().Be(3);
        snap.CurrentReplicas.Should().Be(2);
        snap.ReadyReplicas.Should().Be(2);
    }

    [Fact]
    public async Task ListWorkloadsAsync_ReturnsConfiguredBindings()
    {
        var sut = CreateScaler(new FakeRunner((_, _) =>
            Task.FromResult(new ProcessCommandResult(0, "", ""))));

        var list = await sut.ListWorkloadsAsync();

        list.Should().ContainSingle(w => w.WorkloadId == "mesh-worker" && w.MaxReplicas == 5);
    }

    private static KubernetesWorkloadScaler CreateScaler(
        IProcessCommandRunner runner,
        int min = 0,
        int max = 5)
    {
        var options = Options.Create(new WorkloadScalerOptions
        {
            Provider = "kubernetes",
            Enabled = true,
            Kubernetes = new KubernetesWorkloadScalerOptions
            {
                Namespace = "nexo",
                Workloads =
                {
                    ["mesh-worker"] = new KubernetesWorkloadBinding
                    {
                        Deployment = "nexo-mesh-worker",
                        MinReplicas = min,
                        MaxReplicas = max,
                        DisplayName = "Mesh Worker",
                    },
                },
            },
        });
        return new KubernetesWorkloadScaler(options, runner, NullLogger<KubernetesWorkloadScaler>.Instance);
    }

    private sealed class FakeRunner : IProcessCommandRunner
    {
        private readonly Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> _handler;

        public FakeRunner(Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> handler) =>
            _handler = handler;

        public IReadOnlyList<string> LastArgs { get; private set; } = Array.Empty<string>();

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            LastArgs = arguments.ToArray();
            return _handler(fileName, arguments);
        }
    }
}
