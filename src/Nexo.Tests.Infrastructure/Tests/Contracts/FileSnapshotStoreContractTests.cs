using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Rollback;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

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

    protected override ISnapshotStore CreateInstance() => new FileSnapshotStore(_snapshotPath);

    protected override string GetTempFilePath(string relativePath) =>
        Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public override void Dispose()
    {
        _cleanup.Dispose();
        base.Dispose();
    }
}
