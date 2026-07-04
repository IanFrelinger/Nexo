using Nexo.Core.Application.Observation.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for lite db pattern store contract tests impl.</summary>
public sealed class LiteDbPatternStoreContractTestsImpl : PatternStoreContractTests, IDisposable
{
    private readonly string _dbPath;
    private readonly IDisposable _cleanup;

    public LiteDbPatternStoreContractTestsImpl()
    {
        (_dbPath, _cleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-pattern-contract");
        _dbPath = Path.Combine(_dbPath, "patterns.db");
    }

    /// <summary>Creates instance.</summary>
    protected override IPatternStore CreateInstance() => new LiteDbPatternStore(_dbPath);

    /// <summary>Dispose.</summary>
    public void Dispose() => _cleanup.Dispose();
}
