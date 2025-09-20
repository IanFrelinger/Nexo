using Xunit;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Collection fixture to ensure tests sharing static state run sequentially
/// </summary>
[CollectionDefinition("StaticState", DisableParallelization = true)]
public class StaticStateCollection : ICollectionFixture<StaticStateFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class StaticStateFixture : IDisposable
{
    public StaticStateFixture()
    {
        // Reset all static state before any test in this collection runs
        DemoHarness.ResetAllState();
    }

    public void Dispose()
    {
        // Reset all static state after all tests in this collection complete
        DemoHarness.ResetAllState();
    }
}
