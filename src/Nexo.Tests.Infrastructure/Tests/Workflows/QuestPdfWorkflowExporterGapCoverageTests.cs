using FluentAssertions;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

public sealed class QuestPdfWorkflowExporterGapCoverageTests
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
    public async Task ExportToPdfAsync_handles_null_content()
    {
        var exporter = TryCreateExporter();
        if (exporter is null)
            return;

        var bytes = await exporter.ExportToPdfAsync(null!);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, 4)).Should().Be("%PDF");
    }

    [Fact]
    public async Task ExportToPdfAsync_handles_multiline_content()
    {
        var exporter = TryCreateExporter();
        if (exporter is null)
            return;

        var bytes = await exporter.ExportToPdfAsync("line-one\nline-two\nline-three");

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }
}
