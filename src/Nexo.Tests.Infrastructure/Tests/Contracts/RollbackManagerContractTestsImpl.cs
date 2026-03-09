using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Rollback;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

public sealed class RollbackManagerContractTestsImpl : RollbackManagerContractTests, IDisposable
{
    private readonly string _tempDir;
    private readonly string _snapshotPath;
    private readonly string _auditPath;
    private readonly IDisposable _cleanup;
    private readonly IRollbackManager _rollbackManager;

    public RollbackManagerContractTestsImpl()
    {
        (_tempDir, _cleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-rollback-contract");
        _snapshotPath = Path.Combine(_tempDir, "snapshots");
        _auditPath = Path.Combine(_tempDir, "audit.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(_ => new LiteDbAdaptationAuditLog(_auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(_snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        _rollbackManager = services.GetRequiredService<IRollbackManager>();
    }

    protected override IRollbackManager CreateInstance() => _rollbackManager;

    protected override string GetTempFilePath(string relativePath) =>
        Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public override void Dispose()
    {
        _cleanup.Dispose();
        base.Dispose();
    }
}
