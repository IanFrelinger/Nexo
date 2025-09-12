using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Services.RAG
{
    /// <summary>
    /// Service for ingesting C# and .NET documentation into the RAG system
    /// </summary>
    public class DocumentationIngestionService : IDocumentationIngestionService
    {
        private readonly ILogger<DocumentationIngestionService> _logger;
        private readonly IDocumentationRAGService _ragService;
        private readonly IDocumentationParser _parser;

        public DocumentationIngestionService(
            ILogger<DocumentationIngestionService> logger,
            IDocumentationRAGService ragService,
            IDocumentationParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public async Task IngestCSharpDocumentationAsync(string documentationPath)
        {
            try
            {
                _logger.LogInformation("Starting C# documentation ingestion from: {Path}", documentationPath);

                var chunks = new List<DocumentationChunk>();

                // Ingest C# language documentation
                await IngestLanguageDocumentationAsync(documentationPath, "CSharp", chunks);

                // Ingest .NET Framework documentation
                await IngestFrameworkDocumentationAsync(documentationPath, "NetFramework", chunks);

                // Ingest .NET Core documentation
                await IngestFrameworkDocumentationAsync(documentationPath, "NetCore", chunks);

                // Ingest .NET 5+ documentation
                await IngestFrameworkDocumentationAsync(documentationPath, "Net5Plus", chunks);

                // Index all chunks
                await _ragService.BulkIndexDocumentationAsync(chunks);

                _logger.LogInformation("Successfully ingested {Count} documentation chunks", chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during C# documentation ingestion");
                throw;
            }
        }

        public async Task IngestDotNetRuntimeDocumentationAsync(string documentationPath, string runtimeVersion)
        {
            try
            {
                _logger.LogInformation("Starting .NET {Version} documentation ingestion from: {Path}", runtimeVersion, documentationPath);

                var chunks = new List<DocumentationChunk>();

                // Ingest runtime-specific documentation
                await IngestRuntimeSpecificDocumentationAsync(documentationPath, runtimeVersion, chunks);

                // Ingest API reference documentation
                await IngestAPIReferenceDocumentationAsync(documentationPath, runtimeVersion, chunks);

                // Ingest migration guides
                await IngestMigrationGuidesAsync(documentationPath, runtimeVersion, chunks);

                // Index all chunks
                await _ragService.BulkIndexDocumentationAsync(chunks);

                _logger.LogInformation("Successfully ingested {Count} documentation chunks for .NET {Version}", chunks.Count, runtimeVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during .NET {Version} documentation ingestion", runtimeVersion);
                throw;
            }
        }

        public async Task IngestCustomDocumentationAsync(string filePath, DocumentationMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            try
            {
                _logger.LogInformation("Ingesting custom documentation from: {FilePath}", filePath);

                var content = await File.ReadAllTextAsync(filePath);
                var chunks = await _parser.ParseDocumentationAsync(content, metadata);

                await _ragService.BulkIndexDocumentationAsync(chunks);

                _logger.LogInformation("Successfully ingested custom documentation with {Count} chunks", chunks.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting custom documentation from: {FilePath}", filePath);
                throw;
            }
        }

        private async Task IngestLanguageDocumentationAsync(string basePath, string language, List<DocumentationChunk> chunks)
        {
            var languagePath = Path.Combine(basePath, "Languages", language);
            if (!Directory.Exists(languagePath))
            {
                _logger.LogWarning("Language documentation path not found: {Path}", languagePath);
                return;
            }

            var files = Directory.GetFiles(languagePath, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var metadata = new DocumentationMetadata
                    {
                        Source = file,
                        DocumentationType = "Language",
                        Version = ExtractVersionFromPath(file),
                        Runtime = "All",
                        Categories = new[] { "Language", "Syntax", "Features" }
                    };

                    var fileChunks = await _parser.ParseDocumentationAsync(content, metadata);
                    chunks.AddRange(fileChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing language documentation file: {File}", file);
                }
            }
        }

        private async Task IngestFrameworkDocumentationAsync(string basePath, string framework, List<DocumentationChunk> chunks)
        {
            var frameworkPath = Path.Combine(basePath, "Frameworks", framework);
            if (!Directory.Exists(frameworkPath))
            {
                _logger.LogWarning("Framework documentation path not found: {Path}", frameworkPath);
                return;
            }

            var files = Directory.GetFiles(frameworkPath, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var metadata = new DocumentationMetadata
                    {
                        Source = file,
                        DocumentationType = "Framework",
                        Version = ExtractVersionFromPath(file),
                        Runtime = framework,
                        Categories = new[] { "Framework", "API", "Concepts" }
                    };

                    var fileChunks = await _parser.ParseDocumentationAsync(content, metadata);
                    chunks.AddRange(fileChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing framework documentation file: {File}", file);
                }
            }
        }

        private async Task IngestRuntimeSpecificDocumentationAsync(string basePath, string runtimeVersion, List<DocumentationChunk> chunks)
        {
            var runtimePath = Path.Combine(basePath, "Runtimes", runtimeVersion);
            if (!Directory.Exists(runtimePath))
            {
                _logger.LogWarning("Runtime documentation path not found: {Path}", runtimePath);
                return;
            }

            var files = Directory.GetFiles(runtimePath, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var metadata = new DocumentationMetadata
                    {
                        Source = file,
                        DocumentationType = "Runtime",
                        Version = runtimeVersion,
                        Runtime = runtimeVersion,
                        Categories = new[] { "Runtime", "Performance", "Features" }
                    };

                    var fileChunks = await _parser.ParseDocumentationAsync(content, metadata);
                    chunks.AddRange(fileChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing runtime documentation file: {File}", file);
                }
            }
        }

        private async Task IngestAPIReferenceDocumentationAsync(string basePath, string runtimeVersion, List<DocumentationChunk> chunks)
        {
            var apiPath = Path.Combine(basePath, "API", runtimeVersion);
            if (!Directory.Exists(apiPath))
            {
                _logger.LogWarning("API documentation path not found: {Path}", apiPath);
                return;
            }

            var files = Directory.GetFiles(apiPath, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var metadata = new DocumentationMetadata
                    {
                        Source = file,
                        DocumentationType = "API",
                        Version = runtimeVersion,
                        Runtime = runtimeVersion,
                        Categories = new[] { "API", "Reference", "Methods", "Properties" }
                    };

                    var fileChunks = await _parser.ParseDocumentationAsync(content, metadata);
                    chunks.AddRange(fileChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing API documentation file: {File}", file);
                }
            }
        }

        private async Task IngestMigrationGuidesAsync(string basePath, string runtimeVersion, List<DocumentationChunk> chunks)
        {
            var migrationPath = Path.Combine(basePath, "Migrations", runtimeVersion);
            if (!Directory.Exists(migrationPath))
            {
                _logger.LogWarning("Migration documentation path not found: {Path}", migrationPath);
                return;
            }

            var files = Directory.GetFiles(migrationPath, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var metadata = new DocumentationMetadata
                    {
                        Source = file,
                        DocumentationType = "Migration",
                        Version = runtimeVersion,
                        Runtime = runtimeVersion,
                        Categories = new[] { "Migration", "Breaking Changes", "Upgrade" }
                    };

                    var fileChunks = await _parser.ParseDocumentationAsync(content, metadata);
                    chunks.AddRange(fileChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing migration documentation file: {File}", file);
                }
            }
        }

        private string ExtractVersionFromPath(string filePath)
        {
            // Extract version from path like "CSharp/8.0/features.md" or ".NET/5.0/api.md"
            var parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            foreach (var part in parts)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^\d+\.\d+"))
                {
                    return part;
                }
            }

            return "Unknown";
        }
    }
}
