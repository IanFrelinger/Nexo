using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models.Export;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Interface for export operations
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Export CLI components
        /// </summary>
        Task<ExportResult> ExportCliAsync(string outputPath, CliExportOptions options, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Export Docker components
        /// </summary>
        Task<ExportResult> ExportDockerAsync(string outputPath, DockerExportOptions options, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Export package
        /// </summary>
        Task<ExportResult> ExportPackageAsync(string outputPath, PackageExportOptions options, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Export documentation
        /// </summary>
        Task<ExportResult> ExportDocumentationAsync(string outputPath, DocumentationExportOptions options, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generic export method
        /// </summary>
        Task<ExportResult> ExportAsync(string outputPath, ExportOptions options, CancellationToken cancellationToken = default);
    }
}
