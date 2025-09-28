namespace Nexo.Core.Application.Interfaces;

public interface ICommand<in TIn, TOut>
{
    ValueTask<TOut> ExecuteAsync(TIn input, CancellationToken ct);
}