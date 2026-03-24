using LiteDB;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Durable LiteDB-backed pipeline run store.
/// </summary>
public sealed class LiteDbPipelineRunStore : IPipelineRunStore
{
    private const string CollectionName = "pipeline_runs";
    private readonly string _databasePath;
    private readonly object _gate = new();

    public LiteDbPipelineRunStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        _databasePath = databasePath;
    }

    public Task SaveAsync(PipelineRun run, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (run == null) throw new ArgumentNullException(nameof(run));

        lock (_gate)
        {
            using var db = new LiteDatabase(_databasePath);
            var col = db.GetCollection<PipelineRunDocument>(CollectionName);
            col.EnsureIndex(x => x.RunId, unique: true);
            col.Upsert(ToDocument(run));
        }

        return Task.CompletedTask;
    }

    public Task<PipelineRun?> GetAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));

        PipelineRun? run = null;
        lock (_gate)
        {
            using var db = new LiteDatabase(_databasePath);
            var col = db.GetCollection<PipelineRunDocument>(CollectionName);
            var doc = col.FindById(runId);
            if (doc != null)
                run = FromDocument(doc);
        }

        return Task.FromResult(run);
    }

    private static PipelineRunDocument ToDocument(PipelineRun run)
    {
        return new PipelineRunDocument
        {
            RunId = run.RunId,
            TemplateId = run.TemplateId,
            State = run.State.ToString(),
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            StageRuns = run.StageRuns.Select(stage => new PipelineStageRunDocument
            {
                StageId = stage.StageId,
                State = stage.State.ToString(),
                Attempt = stage.Attempt,
                WorkerId = stage.WorkerId,
                WorkerType = stage.WorkerType?.ToString(),
                Output = stage.Output,
                Error = stage.Error
            }).ToList()
        };
    }

    private static PipelineRun FromDocument(PipelineRunDocument doc)
    {
        return new PipelineRun
        {
            RunId = doc.RunId,
            TemplateId = doc.TemplateId,
            State = ParseEnum<PipelineRunState>(doc.State, PipelineRunState.Pending),
            StartedAt = doc.StartedAt,
            CompletedAt = doc.CompletedAt,
            StageRuns = doc.StageRuns.Select(stage => new PipelineStageRun
            {
                StageId = stage.StageId,
                State = ParseEnum<PipelineStageRunState>(stage.State, PipelineStageRunState.Pending),
                Attempt = stage.Attempt,
                WorkerId = stage.WorkerId,
                WorkerType = string.IsNullOrWhiteSpace(stage.WorkerType)
                    ? null
                    : ParseEnum<PipelineWorkerType>(stage.WorkerType, PipelineWorkerType.Deterministic),
                Output = stage.Output,
                Error = stage.Error
            }).ToArray()
        };
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private sealed class PipelineRunDocument
    {
        [BsonId]
        public string RunId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public List<PipelineStageRunDocument> StageRuns { get; set; } = new();
    }

    private sealed class PipelineStageRunDocument
    {
        public string StageId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Attempt { get; set; }
        public string? WorkerId { get; set; }
        public string? WorkerType { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
    }
}
