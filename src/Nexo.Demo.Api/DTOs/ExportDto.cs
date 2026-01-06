using Nexo.Core.Domain.Export;

namespace Nexo.Demo.Api.DTOs;

/// <summary>
/// Data transfer object for export result.
/// </summary>
public class ExportResultDto
{
    public bool Success { get; init; }
    public string Mode { get; init; } = default!;
    public string Target { get; init; } = default!;
    public IReadOnlyList<ExportedFileDto> Files { get; init; } = [];
    public GenerationSummaryDto? GenerationSummary { get; init; }
    public IReadOnlyList<string> RuntimeRequirements { get; init; } = [];
    public IReadOnlyList<string> Messages { get; init; } = [];
    
    public static ExportResultDto FromDomain(ExportResult result)
    {
        return new ExportResultDto
        {
            Success = result.Success,
            Mode = result.Mode.ToString(),
            Target = result.Target.ToString(),
            Files = result.Files.Select(f => new ExportedFileDto
            {
                Path = f.Path,
                Content = f.Content,
                Language = f.Language
            }).ToList(),
            GenerationSummary = result.GenerationSummary != null
                ? new GenerationSummaryDto
                {
                    ItemsGenerated = result.GenerationSummary.ItemsGenerated,
                    VariationsCreated = result.GenerationSummary.VariationsCreated
                }
                : null,
            RuntimeRequirements = result.RuntimeRequirements.ToList(),
            Messages = result.Messages.ToList()
        };
    }
}

public class ExportedFileDto
{
    public string Path { get; init; } = default!;
    public string Content { get; init; } = default!;
    public string Language { get; init; } = default!;
}

public class GenerationSummaryDto
{
    public int ItemsGenerated { get; init; }
    public int VariationsCreated { get; init; }
}

