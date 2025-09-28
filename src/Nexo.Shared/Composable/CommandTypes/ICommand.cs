using System;
using System.Threading.Tasks;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Stub interface for ICommand to avoid circular dependencies
    /// </summary>
    public interface ICommand<TInput, TOutput>
    {
        Task<TOutput> ExecuteAsync(TInput input);
    }
}
