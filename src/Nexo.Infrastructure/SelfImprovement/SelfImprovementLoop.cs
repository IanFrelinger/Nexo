using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Core.Application.SelfImprovement.Models;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Infrastructure.Adaptation;

namespace Nexo.Infrastructure.SelfImprovement;

/// <summary>
/// Self-improvement loop: query test failures, run adaptation, validate, promote.
/// </summary>
public sealed class SelfImprovementLoop : ISelfImprovementLoop
{
    private readonly ITestFailureStore _testFailureStore;
    private readonly IBrickStaticAnalyzer _analyzer;
    private readonly IImmutableCoreRegistry _immutableCoreRegistry;
    private readonly IAccessBoundary _accessBoundary;
    private readonly IRegressionTestRunner _regressionRunner;
    private readonly IAdaptationPromoter _promoter;
    private readonly IAdaptationAuditLog _auditLog;
    private readonly IRollbackManager _rollbackManager;
    private readonly ISourceCodeFixer _sourceFixer;
    private readonly AdaptationRollbackHelper _rollbackHelper;
    private readonly ILogger<SelfImprovementLoop>? _logger;
    private readonly int _maxIterationsPerRun;
    private SelfImprovementReport? _lastReport;

    public SelfImprovementLoop(
        ITestFailureStore testFailureStore,
        IBrickStaticAnalyzer analyzer,
        IImmutableCoreRegistry immutableCoreRegistry,
        IAccessBoundary accessBoundary,
        IRegressionTestRunner regressionRunner,
        IAdaptationPromoter promoter,
        IAdaptationAuditLog auditLog,
        IRollbackManager rollbackManager,
        ISourceCodeFixer sourceFixer,
        AdaptationRollbackHelper rollbackHelper,
        ILogger<SelfImprovementLoop>? logger = null,
        int maxIterationsPerRun = 5)
    {
        _testFailureStore = testFailureStore ?? throw new ArgumentNullException(nameof(testFailureStore));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _immutableCoreRegistry = immutableCoreRegistry ?? throw new ArgumentNullException(nameof(immutableCoreRegistry));
        _accessBoundary = accessBoundary ?? throw new ArgumentNullException(nameof(accessBoundary));
        _regressionRunner = regressionRunner ?? throw new ArgumentNullException(nameof(regressionRunner));
        _promoter = promoter ?? throw new ArgumentNullException(nameof(promoter));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _rollbackManager = rollbackManager ?? throw new ArgumentNullException(nameof(rollbackManager));
        _sourceFixer = sourceFixer ?? throw new ArgumentNullException(nameof(sourceFixer));
        _rollbackHelper = rollbackHelper ?? throw new ArgumentNullException(nameof(rollbackHelper));
        _logger = logger;
        _maxIterationsPerRun = maxIterationsPerRun;
    }

    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        if (_accessBoundary.IsObservationPaused)
        {
            _logger?.LogInformation("Self-improvement loop skipped: kill switch engaged (trust pause)");
            return;
        }

        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var failures = await _testFailureStore.QueryAsync(since: since, until: null, cancellationToken: ct).ConfigureAwait(false);
        if (failures.Count == 0)
        {
            _lastReport = new SelfImprovementReport(DateTimeOffset.UtcNow, 0, 0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>());
            return;
        }

        var promoted = new List<string>();
        var rejected = new List<string>();
        var failuresProcessed = 0;
        var fixesGenerated = 0;
        var fixesValidated = 0;
        var fixesPromoted = 0;
        var fixesRejected = 0;

        var block1Path = RepoPathResolver.FindBlock1ObservationPath();
        var solutionPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "Nexo.sln");

        foreach (var failure in failures.Take(_maxIterationsPerRun))
        {
            if (_accessBoundary.IsObservationPaused) break;
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(failure.FilePath))
                continue;

            if (_immutableCoreRegistry.IsInImmutableCore(failure.FilePath))
            {
                rejected.Add($"Immutable core: {failure.FilePath}");
                fixesRejected++;
                continue;
            }

            var brickId = ViolationToBrickMapper.GetBrickIdForFile(failure.FilePath);
            if (brickId == null)
                continue;

            failuresProcessed++;

            var failureDir = Path.GetDirectoryName(failure.FilePath) ?? block1Path;
            var analysisResult = await _analyzer.AnalyzeSourceAsync(failureDir, includeAnalyzers: false, ct).ConfigureAwait(false);
            if (analysisResult.Passed)
                continue;

            var violation = analysisResult.Violations.FirstOrDefault(v =>
                string.Equals(v.FilePath, failure.FilePath, StringComparison.OrdinalIgnoreCase));
            if (violation == null)
                continue;

            fixesGenerated++;

            _rollbackHelper.Snapshot(failure.FilePath);
            if (!await _sourceFixer.TryFixAsync(violation, ct).ConfigureAwait(false))
            {
                _rollbackHelper.Clear();
                rejected.Add($"Fix failed: {failure.TestName}");
                fixesRejected++;
                continue;
            }

            var regResult = await _regressionRunner.RunAsync(solutionPath, filter: null, ct).ConfigureAwait(false);
            if (!regResult.AllPassed)
            {
                _rollbackHelper.Rollback(failure.FilePath);
                _rollbackHelper.Clear();
                rejected.Add($"Regression failed: {failure.TestName}");
                fixesRejected++;
                continue;
            }

            fixesValidated++;
            _rollbackHelper.Clear();

            var record = new AdaptationRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = brickId,
                FailureType = failure.ErrorMessage ?? "TestFailure",
                FixApplied = AdaptationFixType.Source,
                FilePath = failure.FilePath,
                RegressionPassed = true,
                Promoted = true,
                Message = $"Self-improvement: fixed {failure.TestName}",
            };

            try
            {
                _rollbackManager.PrepareForInherit(record.Id, new[] { failure.FilePath });
                await _rollbackManager.BeforeInheritAsync(record.Id, ct).ConfigureAwait(false);
                await _promoter.PromoteAsync(record, ct).ConfigureAwait(false);
                promoted.Add(record.Id);
                fixesPromoted++;

                await _auditLog.LogAsync(new AdaptationAuditEntry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Timestamp = record.Timestamp,
                    AutonomyLevel = "self",
                    Outcome = "Promoted",
                    BrickId = record.BrickId,
                    FailureType = record.FailureType,
                    FilePath = record.FilePath,
                    RegressionPassed = true,
                    Promoted = true,
                    Message = record.Message,
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Promotion failed for {Id}, rolling back", record.Id);
                await _rollbackManager.RollbackAsync(record.Id, ct).ConfigureAwait(false);
                rejected.Add(ex.Message);
                fixesRejected++;
            }
        }

        _lastReport = new SelfImprovementReport(
            DateTimeOffset.UtcNow,
            failuresProcessed,
            fixesGenerated,
            fixesValidated,
            fixesPromoted,
            fixesRejected,
            promoted,
            rejected);
    }

    public Task StartContinuousAsync(CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && !_accessBoundary.IsObservationPaused)
            {
                await RunOnceAsync(ct).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(5), ct).ConfigureAwait(false);
            }
        }, ct);
    }

    public Task<SelfImprovementReport?> GetLastRunReportAsync(CancellationToken ct = default) =>
        Task.FromResult(_lastReport);
}
