using Xunit;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Collection to ensure tests that use network attempt tracking run sequentially
/// to avoid race conditions with the static _networkAttempts field
/// </summary>
[CollectionDefinition("NetworkIsolation", DisableParallelization = true)]
public class NetworkIsolationCollection : ICollectionFixture<NetworkIsolationFixture>
{
}

/// <summary>
/// Fixture for network isolation collection
/// </summary>
public class NetworkIsolationFixture
{
    public NetworkIsolationFixture()
    {
        // Reset state when the fixture is created
        DemoHarness.ResetAllState();
    }
}
