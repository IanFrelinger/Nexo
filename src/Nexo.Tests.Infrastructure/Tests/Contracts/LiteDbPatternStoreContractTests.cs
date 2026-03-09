using Nexo.Core.Application.Observation.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

public sealed class LiteDbPatternStoreContractTestsImpl : PatternStoreContractTests, IDisposable
{
    private readonly string _dbPath;
    private readonly IDisposable _cleanup;

    public LiteDbPatternStoreContractTestsImpl()
    {
        (_dbPath, _cleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-pattern-contract");
        _dbPath = Path.Combine(_dbPath, "patterns.db");
    }

    protected override IPatternStore CreateInstance() => new LiteDbPatternStore(_dbPath);

    public void Dispose() => _cleanup.Dispose();
}
