using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    public interface IProjectInitializer
    {
        Task InitializeAsync(string projectName, string projectPath, CancellationToken cancellationToken = default(CancellationToken));
    }
}