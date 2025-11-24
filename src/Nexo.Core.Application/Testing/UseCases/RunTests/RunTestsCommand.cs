using MediatR;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Testing.UseCases.RunTests;

/// <summary>
/// Command to run tests.
/// </summary>
public record RunTestsCommand(string? Filter, IProgress<ProgressReport>? Progress = null) : IRequest<TestExecutionResult>;

