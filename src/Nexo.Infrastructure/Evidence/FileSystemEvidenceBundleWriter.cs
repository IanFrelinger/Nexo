using System.Text.Json;
using Nexo.Core.Application.Evidence.Ports;
using Nexo.Core.Domain.Evidence;

namespace Nexo.Infrastructure.Evidence;

/// <summary>
/// Writes a generic evidence bundle JSON/Markdown pair under the bundle root.
/// Product-specific report fields are left untouched when patching uploads only.
/// </summary>
public sealed class FileSystemEvidenceBundleWriter : IEvidenceBundleWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(
        EvidenceBundle bundle,
        IReadOnlyList<UploadedArtifact>? uploads = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(bundle.RootPath);
        var jsonPath = Path.Combine(bundle.RootPath, "evidence-bundle.json");
        var markdownPath = Path.Combine(bundle.RootPath, "evidence-bundle.md");

        var payload = new
        {
            sessionId = bundle.SessionId,
            generatedAt = DateTimeOffset.UtcNow,
            verdict = bundle.Verdict,
            rootPath = bundle.RootPath,
            checkpoints = bundle.Checkpoints.Select(checkpoint => new
            {
                label = checkpoint.Label,
                statusJson = checkpoint.StatusJson
            }),
            artifacts = bundle.Artifacts.Select(artifact => new
            {
                kind = artifact.Kind.ToString(),
                relativePath = artifact.RelativePath,
                bytes = artifact.Bytes,
                createdAt = artifact.CreatedAt
            }),
            uploads = (uploads ?? Array.Empty<UploadedArtifact>()).Select(upload => new
            {
                provider = upload.Provider,
                externalId = upload.ExternalId,
                webViewLink = upload.WebViewUri.ToString(),
                bytes = upload.Bytes,
                localPath = upload.LocalPath,
                fileName = upload.FileName
            })
        };

        await File.WriteAllTextAsync(
                jsonPath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                }),
                cancellationToken)
            .ConfigureAwait(false);

        var uploadLines = (uploads ?? Array.Empty<UploadedArtifact>())
            .Select(upload => $"- [{upload.FileName}]({upload.WebViewUri}) ({upload.Provider})")
            .ToArray();
        var markdown =
            $"""
            # Evidence bundle

            - Session: {bundle.SessionId}
            - Verdict: {bundle.Verdict ?? "n/a"}
            - Artifacts: {bundle.Artifacts.Count}
            - Checkpoints: {bundle.Checkpoints.Count}

            ## Uploads

            {(uploadLines.Length == 0 ? "_none_" : string.Join("\n", uploadLines))}
            """;
        await File.WriteAllTextAsync(markdownPath, markdown, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Patches an existing product report JSON with <c>videos</c> and <c>driveUploads</c>
    /// (or generic <c>uploads</c>) without rewriting product-specific checks.
    /// </summary>
    public static async Task PatchReportUploadsAsync(
        string reportPath,
        IReadOnlyList<string> videos,
        IReadOnlyList<UploadedArtifact> uploads,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(reportPath))
            return;

        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(reportPath, cancellationToken)
                .ConfigureAwait(false));
        using var stream = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
               {
                   Indented = true
               }))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals("videos") ||
                    property.NameEquals("driveUploads") ||
                    property.NameEquals("uploads"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WritePropertyName("videos");
            writer.WriteStartArray();
            foreach (var video in videos)
                writer.WriteStringValue(video);
            writer.WriteEndArray();

            writer.WritePropertyName("driveUploads");
            writer.WriteStartArray();
            foreach (var upload in uploads)
            {
                writer.WriteStartObject();
                writer.WriteString("localPath", upload.LocalPath);
                writer.WriteString("fileId", upload.ExternalId);
                writer.WriteString("fileName", upload.FileName);
                writer.WriteNumber("bytes", upload.Bytes);
                writer.WriteString("webViewLink", upload.WebViewUri.ToString());
                writer.WriteString("provider", upload.Provider);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await File.WriteAllBytesAsync(reportPath, stream.ToArray(), cancellationToken)
            .ConfigureAwait(false);

        var markdownPath = Path.ChangeExtension(reportPath, ".md");
        if (!File.Exists(markdownPath) || uploads.Count == 0)
            return;

        var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken)
            .ConfigureAwait(false);
        if (markdown.Contains("## Google Drive uploads", StringComparison.Ordinal) ||
            markdown.Contains("## Artifact uploads", StringComparison.Ordinal))
        {
            return;
        }

        var section = "\n\n## Artifact uploads\n\n" +
                      string.Join(
                          "\n",
                          uploads.Select(upload =>
                              $"- [{upload.FileName}]({upload.WebViewUri})"));
        await File.WriteAllTextAsync(
                markdownPath,
                markdown.TrimEnd() + section + "\n",
                cancellationToken)
            .ConfigureAwait(false);
    }
}
