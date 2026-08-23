using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Ashlar.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Configurable responses for <see cref="RunPodLoopbackApiServer"/>.</summary>
public sealed class RunPodLoopbackApiConfiguration
{
    public string InstanceId { get; set; } = "loopback-inst";

    public string JobId { get; set; } = "loopback-job";

    public byte[] PullBytes { get; set; } = [1];

    public Queue<RunPodLoopbackPollStatus> PollStatuses { get; } = new();
}
