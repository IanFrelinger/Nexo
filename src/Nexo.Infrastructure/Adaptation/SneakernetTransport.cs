using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Sneakernet transport: export shared adaptations to a single file, import and adopt.
/// P3.3: nexo mesh export, nexo mesh import.
/// </summary>
public sealed class SneakernetTransport : ISneakernetTransport
{
    private readonly ISharedAdaptationSync _sync;
    private readonly ILogger<SneakernetTransport>? _logger;

    public SneakernetTransport(ISharedAdaptationSync sync, ILogger<SneakernetTransport>? logger = null)
    {
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExportAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var entries = await _sync.PullAsync(cancellationToken).ConfigureAwait(false);
        var dto = new SneakernetExportDto
        {
            Version = 1,
            ExportedAt = DateTimeOffset.UtcNow,
            EntryCount = entries.Count,
            Entries = entries.Select(e => new SneakernetEntryDto
            {
                Id = e.Id,
                Record = e.Record,
                Files = e.Files.ToDictionary(kv => kv.Key, kv => Convert.ToBase64String(kv.Value)),
                SourcePeerId = e.SourcePeerId,
                BroadcastAt = e.BroadcastAt
            }).ToList()
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Exported {Count} adaptation(s) to {Path}", entries.Count, outputPath);
    }

    /// <inheritdoc />
    public async Task<int> ImportAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            _logger?.LogWarning("Export file not found: {Path}", inputPath);
            return 0;
        }
        var json = await File.ReadAllTextAsync(inputPath, cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<SneakernetExportDto>(json);
        if (dto?.Entries == null || dto.Entries.Count == 0)
        {
            _logger?.LogInformation("No entries in export file");
            return 0;
        }
        var adopted = 0;
        foreach (var e in dto.Entries)
        {
            var entry = new SharedAdaptationEntry
            {
                Id = e.Id,
                Record = e.Record,
                Files = e.Files.ToDictionary(kv => kv.Key, kv => Convert.FromBase64String(kv.Value)),
                SourcePeerId = e.SourcePeerId,
                BroadcastAt = e.BroadcastAt
            };
            var ok = await _sync.ValidateAndAdoptAsync(entry, cancellationToken).ConfigureAwait(false);
            if (ok) adopted++;
        }
        _logger?.LogInformation("Imported {Adopted}/{Total} adaptation(s) from {Path}", adopted, dto.Entries.Count, inputPath);
        return adopted;
    }

    private sealed class SneakernetExportDto
    {
        public int Version { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public int EntryCount { get; set; }
        public List<SneakernetEntryDto> Entries { get; set; } = new();
    }

    private sealed class SneakernetEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public AdaptationRecord Record { get; set; } = null!;
        public Dictionary<string, string> Files { get; set; } = new();
        public string? SourcePeerId { get; set; }
        public DateTimeOffset BroadcastAt { get; set; }
    }
}
