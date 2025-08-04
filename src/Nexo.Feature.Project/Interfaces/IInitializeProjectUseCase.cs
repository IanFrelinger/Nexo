using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    public interface IInitializeProjectUseCase
    {
        Task ExecuteAsync(string projectName, string projectPath, CancellationToken cancellationToken = default(CancellationToken));
    }
}