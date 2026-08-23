using System.Threading.Channels;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;

namespace Ashlar.Infrastructure.Observation;
/// <summary>
/// Merges multiple event sources into a single stream.
/// </summary>
public sealed class CompositeEventSource : IObservableEventSource
{
    private const string SourceIdValue = "composite";
    private readonly IReadOnlyList<IObservableEventSource> _sources;
    /// <summary>
    /// Creates a composite source that merges events from the given sources.
    /// </summary>
    public CompositeEventSource(IEnumerable<IObservableEventSource> sources)
    {
        _sources = sources?.ToList() ?? throw new ArgumentNullException(nameof(sources));
    }

    /// <inheritdoc/>
    public string SourceId => SourceIdValue;

    /// <inheritdoc/>
    public async IAsyncEnumerable<NormalizedEvent> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_sources.Count == 0)
            yield break;
        var channel = Channel.CreateUnbounded<NormalizedEvent>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false, });
        var writer = channel.Writer;
        var tasks = _sources.Select(async source =>
        {
            try
            {
                await foreach (var evt in source.SubscribeAsync(cancellationToken).WithCancellation(cancellationToken))
                {
                    await writer.WriteAsync(evt, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when subscription is cancelled
            }
            catch (Exception)
            {
                // Swallow to avoid breaking the composite stream
            }
        }).ToList();
        _ = Task.WhenAll(tasks).ContinueWith(_ => writer.Complete(), CancellationToken.None);
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }
}