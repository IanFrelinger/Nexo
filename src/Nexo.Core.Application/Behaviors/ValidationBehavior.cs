using FluentValidation;
using MediatR;

namespace Nexo.Core.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior for FluentValidation.
/// 
/// Responsibilities:
/// - Validates requests using FluentValidation validators
/// - Runs all registered validators in parallel
/// - Throws ValidationException if validation fails
/// - Allows request to proceed if validation passes
/// 
/// Used in MediatR pipeline to automatically validate all requests.
/// Implements IPipelineBehavior for MediatR integration.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Creates a validation pipeline behavior for the given request validators.</summary>
    /// <param name="validators">FluentValidation validators registered for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <summary>Validates the request, then invokes the next pipeline delegate.</summary>
    /// <param name="request">MediatR request to validate and handle.</param>
    /// <param name="next">Next delegate in the MediatR pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Handler response when validation passes.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}

