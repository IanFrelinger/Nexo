using MediatR;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// Command to analyze code and assemblies for violations.
/// </summary>
public record AnalyzeCodeCommand(DirectoryInfo Path, IProgress<ProgressReport>? Progress = null) : IRequest<AnalysisResult>;

