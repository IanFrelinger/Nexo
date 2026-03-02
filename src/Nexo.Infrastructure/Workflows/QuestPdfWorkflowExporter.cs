using Nexo.Core.Application.Common.Ports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nexo.Infrastructure.Workflows;

/// <summary>
/// Exports workflow output content to PDF using QuestPDF.
/// </summary>
public sealed class QuestPdfWorkflowExporter : IWorkflowPdfExporter
{
    static QuestPdfWorkflowExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportToPdfAsync(string content, CancellationToken ct = default)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header()
                    .Text("Workflow Output")
                    .SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(8);
                        foreach (var line in (content ?? "").Split('\n'))
                            x.Item().Text(line).FontSize(10);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
