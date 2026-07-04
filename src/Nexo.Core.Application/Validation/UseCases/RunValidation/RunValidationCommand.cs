using MediatR;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>Command to run architecture tests and contract checks.</summary>
/// <param name="Filter">Optional xUnit test filter.</param>
/// <param name="Progress">Optional progress reporter for streaming updates.</param>
public record RunValidationCommand(string? Filter, IProgress<ProgressReport>? Progress = null) : IRequest<ValidationResult>;
