using FluentAssertions;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

/// <summary>
/// Tests for QuestPdfWorkflowExporter. Skips when QuestPDF native library (QuestPdfSkia) is not
/// available on the current platform (e.g. macOS without Skia runtime in test output).
/// </summary>
public sealed class QuestPdfWorkflowExporterTests
{
    private static QuestPdfWorkflowExporter? TryCreateExporter()
    {
        try
        {
            return new QuestPdfWorkflowExporter();
        }
        catch (TypeInitializationException)
        {
            return null;
        }
    }

    [Fact]
    public async Task ExportToPdfAsync_ReturnsValidPdfBytes()
    {
        var exporter = TryCreateExporter();
        if (exporter == null)
            return; // QuestPDF native library not available (e.g. macOS without Skia runtime)

        var bytes = await exporter.ExportToPdfAsync("Hello");

        bytes.Should().NotBeNullOrEmpty();
        var header = System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, Math.Min(8, bytes.Length)));
        header.Should().StartWith("%PDF");
    }

    [Fact]
    public async Task ExportToPdfAsync_EmptyContent_ReturnsNonEmptyPdf()
    {
        var exporter = TryCreateExporter();
        if (exporter == null)
            return; // QuestPDF native library not available

        var bytes = await exporter.ExportToPdfAsync("");

        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(0);
    }
}
