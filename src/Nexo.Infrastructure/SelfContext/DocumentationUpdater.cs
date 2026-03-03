using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Ports;

namespace Nexo.Infrastructure.SelfContext;

/// <summary>
/// Updates docs/bricks/ when adaptations are promoted.
/// </summary>
public sealed class DocumentationUpdater : IDocumentationUpdater
{
    private readonly IAdaptationLog _adaptationLog;
    private readonly IChangelogGenerator _changelogGenerator;

    public DocumentationUpdater(IAdaptationLog adaptationLog, IChangelogGenerator changelogGenerator)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
        _changelogGenerator = changelogGenerator ?? throw new ArgumentNullException(nameof(changelogGenerator));
    }

    public async Task UpdateForAdaptationAsync(string adaptationId, CancellationToken ct = default)
    {
        var records = await _adaptationLog.QueryAsync(DateTimeOffset.UtcNow.AddDays(-1), null, null, ct).ConfigureAwait(false);
        var record = records.FirstOrDefault(r => string.Equals(r.Id, adaptationId, StringComparison.OrdinalIgnoreCase) && r.Promoted);
        if (record == null) return;

        var brickId = record.BrickId ?? "unknown";
        var docPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "docs", "bricks", $"{brickId}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(docPath)!);

        var changelog = await _changelogGenerator.GenerateAsync(DateTimeOffset.UtcNow.AddDays(-1), null, ct).ConfigureAwait(false);
        var content = $@"# {brickId}

## Behavior

Adapted from promotion {adaptationId}.

**Last updated:** {DateTimeOffset.UtcNow:yyyy-MM-dd}

## Changelog

```markdown
{changelog}
```
";
        await File.WriteAllTextAsync(docPath, content, ct).ConfigureAwait(false);
    }

    public Task GenerateStubAsync(string componentId, CancellationToken ct = default)
    {
        var docPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "docs", "bricks", $"{componentId}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(docPath)!);

        if (File.Exists(docPath))
            return Task.CompletedTask;

        var content = $@"# {componentId}

## Behavior

(Stub - no documentation yet)

**Generated:** {DateTimeOffset.UtcNow:yyyy-MM-dd}
";
        return File.WriteAllTextAsync(docPath, content, ct);
    }
}
