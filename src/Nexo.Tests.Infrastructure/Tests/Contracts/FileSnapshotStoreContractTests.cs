using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Rollback;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for file snapshot store contract.</summary>
public sealed class FileSnapshotStoreContractTests : SnapshotStoreContractTests
{
    private readonly string _tempDir;
    private readonly string _snapshotPath;
    private readonly IDisposable _cleanup;

    public FileSnapshotStoreContractTests()
    {
        (_tempDir, _cleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-snapshot-contract");
        _snapshotPath = Path.Combine(_tempDir, "snapshots");
    }

    /// <summary>Creates instance.</summary>
    protected override ISnapshotStore CreateInstance() => new FileSnapshotStore(_snapshotPath);

    /// <summary>Gets temp file path.</summary>
    /// <param name="relativePath">Relative path.</param>
    protected override string GetTempFilePath(string relativePath) =>
        Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public override void Dispose()
    {
        _cleanup.Dispose();
        base.Dispose();
    }
}
