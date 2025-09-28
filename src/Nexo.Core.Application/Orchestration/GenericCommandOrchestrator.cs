using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Orchestration;

public interface IPreValidator
{
    ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct);
}

public interface IPostValidator<TOut>
{
    ValueTask<(bool ok, string? reason)> ValidateAsync(TOut output, CancellationToken ct);
}

public sealed class GenericCommandOrchestrator : IOrchestrator
{
    private readonly IEnumerable<IPreValidator> _pre;
    private readonly IEnumerable<object> _post; // covariant storage for IPostValidator<T>

    public GenericCommandOrchestrator(IEnumerable<IPreValidator> preValidators,
                                      IEnumerable<object> postValidators)
    {
        _pre = preValidators;
        _post = postValidators;
    }

    public async ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(
        ICommand<TIn, TOut> command, TIn input, CancellationToken ct)
    {
        foreach (var v in _pre)
        {
            var (ok, reason) = await v.ValidateAsync(input!, ct);
            if (!ok) return new(false, $"pre-validation failed: {reason}");
        }

        var result = await command.ExecuteAsync(input, ct);

        foreach (var v in _post.OfType<IPostValidator<TOut>>())
        {
            var (ok, reason) = await v.ValidateAsync(result, ct);
            if (!ok) return new(false, $"post-validation failed: {reason}");
        }

        return new(true);
    }
}
