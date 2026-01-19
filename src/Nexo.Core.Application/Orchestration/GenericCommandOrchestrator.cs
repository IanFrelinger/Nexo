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
    private readonly Nexo.Core.Application.Common.Ports.ILoopKernel _loops;

    public GenericCommandOrchestrator(IEnumerable<IPreValidator> preValidators,
                                      IEnumerable<object> postValidators,
                                      Nexo.Core.Application.Common.Ports.ILoopKernel loops)
    {
        _pre = preValidators;
        _post = postValidators;
        _loops = loops;
    }

    public async ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(
        ICommand<TIn, TOut> command, TIn input, CancellationToken ct)
    {
        OrchestrationResult? preFail = null;
        await _loops.ForEachAsync(_pre, async (v, _, token) =>
        {
            var (ok, reason) = await v.ValidateAsync(input!, token);
            if (!ok)
            {
                preFail = new OrchestrationResult(false, $"pre-validation failed: {reason}");
                return Nexo.Core.Application.Common.Ports.LoopAction.Break;
            }
            return Nexo.Core.Application.Common.Ports.LoopAction.Continue;
        }, new Nexo.Core.Application.Common.Ports.LoopOptions { Name = "pre-validators" }, ct);

        if (preFail != null) return preFail;

        var result = await command.ExecuteAsync(input, ct);

        var postValidators = _post.OfType<IPostValidator<TOut>>().ToList();
        OrchestrationResult? postFail = null;
        await _loops.ForEachAsync(postValidators, async (v, _, token) =>
        {
            var (ok, reason) = await v.ValidateAsync(result, token);
            if (!ok)
            {
                postFail = new OrchestrationResult(false, $"post-validation failed: {reason}");
                return Nexo.Core.Application.Common.Ports.LoopAction.Break;
            }
            return Nexo.Core.Application.Common.Ports.LoopAction.Continue;
        }, new Nexo.Core.Application.Common.Ports.LoopOptions { Name = "post-validators" }, ct);

        if (postFail != null) return postFail;

        return new(true);
    }
}
