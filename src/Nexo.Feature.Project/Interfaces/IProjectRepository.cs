using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<string>> GetAllProjectNamesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task AddProjectAsync(string projectName, CancellationToken cancellationToken = default(CancellationToken));
        Task RemoveProjectAsync(string projectName, CancellationToken cancellationToken = default(CancellationToken));
    }
}