using MediatR;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// Command to run architecture tests and contract checks.
/// </summary>
public record RunValidationCommand(string? Filter, IProgress<ProgressReport>? Progress = null) : IRequest<ValidationResult>;

