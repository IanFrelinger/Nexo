using System.Text.Json;
using GameDirector.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace GameDirector.Agents;

public interface IActivityFeedPublisher
{
    Task PublishAsync(string source, string eventType, string summary, CancellationToken ct);
}
