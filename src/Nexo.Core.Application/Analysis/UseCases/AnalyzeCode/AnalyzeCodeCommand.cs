using MediatR;
using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// Command to analyze code and assemblies for violations.
/// </summary>
public record AnalyzeCodeCommand(DirectoryInfo Path) : IRequest<AnalysisResult>;

