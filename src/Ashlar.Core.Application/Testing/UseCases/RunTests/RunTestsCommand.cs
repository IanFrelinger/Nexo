using MediatR;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.Core.Application.Testing.UseCases.RunTests;

/// <summary>Command to run tests.</summary>
/// <param name="Filter">Optional xUnit test filter.</param>
/// <param name="Progress">Optional progress reporter for streaming updates.</param>
public record RunTestsCommand(string? Filter, IProgress<ProgressReport>? Progress = null) : IRequest<TestExecutionResult>;
