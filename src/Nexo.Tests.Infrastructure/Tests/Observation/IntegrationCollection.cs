using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

/// <summary>
/// Integration tests run sequentially to avoid file watcher and temp dir contention.
/// </summary>
[CollectionDefinition("Integration", DisableParallelization = true)]
public sealed class IntegrationCollection
{
}
