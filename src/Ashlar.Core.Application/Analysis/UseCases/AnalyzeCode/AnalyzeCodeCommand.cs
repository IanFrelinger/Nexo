using MediatR;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>Command to analyze code and assemblies for violations.</summary>
/// <param name="Path">Directory to analyze.</param>
/// <param name="Progress">Optional progress reporter for streaming updates.</param>
public record AnalyzeCodeCommand(DirectoryInfo Path, IProgress<ProgressReport>? Progress = null) : IRequest<AnalysisResult>;
