using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// E2E tests run sequentially to avoid resource contention (CLI subprocesses, file watchers, temp dirs).
/// </summary>
[CollectionDefinition("E2E", DisableParallelization = true)]
public sealed class E2ECollection
{
}
