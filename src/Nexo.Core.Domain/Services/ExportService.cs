using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Services.Export;
using Nexo.Core.Domain.Models.Export;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Orchestrator for export operations that delegates to specialized export services.
    /// </summary>
    public partial class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly CliExporter _cliExporter;
        private readonly DockerExporter _dockerExporter;
        private readonly PackageExporter _packageExporter;
        private readonly DocumentationExporter _documentationExporter;

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cliExporter = new CliExporter(logger);
            _dockerExporter = new DockerExporter(logger);
            _packageExporter = new PackageExporter(logger);
            _documentationExporter = new DocumentationExporter(logger);
        }

        public async Task<ExportResult> ExportCliAsync(
            string outputPath,
            CliExportOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _cliExporter.ExportCliAsync(outputPath, options, cancellationToken);
        }

        public async Task<ExportResult> ExportDockerAsync(
            string outputPath,
            DockerExportOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _dockerExporter.ExportDockerAsync(outputPath, options, cancellationToken);
        }

        public async Task<ExportResult> ExportPackageAsync(
            string outputPath,
            PackageExportOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _packageExporter.ExportPackageAsync(outputPath, options, cancellationToken);
        }

        public async Task<ExportResult> ExportDocumentationAsync(
            string outputPath,
            DocumentationExportOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _documentationExporter.ExportDocumentationAsync(outputPath, options, cancellationToken);
        }

        public async Task<ExportResult> ExportAllAsync(
            string outputPath,
            ExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting comprehensive export to {OutputPath}", outputPath);

                var results = new List<ExportResult>();

                // Export CLI
                if (options.IncludeCli)
                {
                    var cliResult = await ExportCliAsync(outputPath, options.CliOptions, cancellationToken);
                    results.Add(cliResult);
                }

                // Export Docker
                if (options.IncludeDocker)
                {
                    var dockerResult = await ExportDockerAsync(outputPath, options.DockerOptions, cancellationToken);
                    results.Add(dockerResult);
                }

                // Export Package
                if (options.IncludePackage)
                {
                    var packageResult = await ExportPackageAsync(outputPath, options.PackageOptions, cancellationToken);
                    results.Add(packageResult);
                }

                // Export Documentation
                if (options.IncludeDocumentation)
                {
                    var docResult = await ExportDocumentationAsync(outputPath, options.DocumentationOptions, cancellationToken);
                    results.Add(docResult);
                }

                var allFiles = results.SelectMany(r => r.Files).ToList();
                var success = results.All(r => r.Success);

                return new ExportResult
                {
                    Success = success,
                    ExportPath = outputPath,
                    ArtifactType = "All",
                    Files = allFiles,
                    Message = success ? "All exports completed successfully" : "Some exports failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive export");
                return new ExportResult
                {
                    Success = false,
                    Message = $"Export failed: {ex.Message}",
                    Files = new List<string>()
                };
            }
        }

        public async Task<ExportResult> ExportAsync(string outputPath, ExportOptions options, CancellationToken cancellationToken = default)
        {
            return options switch
            {
                CliExportOptions cliOptions => await ExportCliAsync(outputPath, cliOptions, cancellationToken),
                DockerExportOptions dockerOptions => await ExportDockerAsync(outputPath, dockerOptions, cancellationToken),
                PackageExportOptions packageOptions => await ExportPackageAsync(outputPath, packageOptions, cancellationToken),
                DocumentationExportOptions docOptions => await ExportDocumentationAsync(outputPath, docOptions, cancellationToken),
                _ => new ExportResult { Success = false, Error = "Unsupported export options type" }
            };
        }
    }
}
