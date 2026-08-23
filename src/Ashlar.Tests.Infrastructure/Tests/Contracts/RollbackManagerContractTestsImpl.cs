using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Rollback;
using Ashlar.Tests.Application.Helpers;
using Ashlar.Tests.Contracts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for rollback manager contract tests impl.</summary>
public sealed class RollbackManagerContractTestsImpl : RollbackManagerContractTests, IDisposable
{
    private readonly string _tempDir;
    private readonly string _snapshotPath;
    private readonly string _auditPath;
    private readonly IDisposable _cleanup;
    private readonly IRollbackManager _rollbackManager;

    public RollbackManagerContractTestsImpl()
    {
        (_tempDir, _cleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-rollback-contract");
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

    /// <summary>Creates instance.</summary>
    protected override IRollbackManager CreateInstance() => _rollbackManager;

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
