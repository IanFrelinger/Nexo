using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    public interface ISolutionScaffolder
    {
        Task ScaffoldSolutionAsync(string solutionName, string outputPath, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task AddProjectToSolutionAsync(string solutionPath, string projectName, CancellationToken cancellationToken = default(CancellationToken));
        Task RemoveProjectFromSolutionAsync(string solutionPath, string projectName, CancellationToken cancellationToken = default(CancellationToken));
    }
} 