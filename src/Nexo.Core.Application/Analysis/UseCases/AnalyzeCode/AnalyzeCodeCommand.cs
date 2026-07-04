using MediatR;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>Command to analyze code and assemblies for violations.</summary>
/// <param name="Path">Directory to analyze.</param>
/// <param name="Progress">Optional progress reporter for streaming updates.</param>
public record AnalyzeCodeCommand(DirectoryInfo Path, IProgress<ProgressReport>? Progress = null) : IRequest<AnalysisResult>;
