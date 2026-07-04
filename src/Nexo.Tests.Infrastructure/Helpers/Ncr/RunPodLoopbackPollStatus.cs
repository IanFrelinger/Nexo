using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Nexo.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Serialized JSON shape for job poll responses.</summary>
public sealed class RunPodLoopbackPollStatus
{
    public string status { get; set; } = "completed";
    public string? message { get; set; }
}
