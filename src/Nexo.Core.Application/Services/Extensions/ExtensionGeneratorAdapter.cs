using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Contracts;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// Adapter that implements the new IExtensionGenerator interface using the existing AIExtensionGenerator.
    /// </summary>
    public class ExtensionGeneratorAdapter : IExtensionGenerator<ExtensionRequest, GeneratedExtension>
    {
        private readonly ILogger<ExtensionGeneratorAdapter> _logger;
        private readonly IExtensionGenerator _innerGenerator;

        public ExtensionGeneratorAdapter(
            ILogger<ExtensionGeneratorAdapter> logger,
            IExtensionGenerator innerGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _innerGenerator = innerGenerator ?? throw new ArgumentNullException(nameof(innerGenerator));
        }

        public async Task<GenerationResult<GeneratedExtension>> GenerateAsync(ExtensionRequest request, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Generating extension: {ExtensionName}", request.Name);

                // Use the existing generator to create the extension
                var result = await _innerGenerator.GenerateExtensionAsync(request);

                // Convert to the new contract types
                var artifact = new GeneratedExtension(
                    Id: result.RequestId,
                    Name: request.Name,
                    SourceCode: result.GeneratedCode,
                    CompiledAssembly: result.CompiledAssembly,
                    GeneratedAt: DateTime.UtcNow
                );

                var notes = new List<string>();
                
                if (result.IsSuccess)
                {
                    notes.Add($"Extension generated successfully in {result.TotalTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    notes.Add($"Extension generation failed with {result.CompilationErrors.Count} errors");
                }

                if (result.HasSyntaxWarnings)
                {
                    notes.Add($"Generated with {result.SyntaxWarnings.Count} warnings");
                }

                return new GenerationResult<GeneratedExtension>(artifact, result.GeneratedCode, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating extension: {ExtensionName}", request.Name);
                
                var artifact = new GeneratedExtension(
                    Id: request.Id,
                    Name: request.Name,
                    SourceCode: string.Empty,
                    CompiledAssembly: null,
                    GeneratedAt: DateTime.UtcNow
                );

                var notes = new List<string> { $"Generation failed: {ex.Message}" };
                
                return new GenerationResult<GeneratedExtension>(artifact, string.Empty, notes);
            }
        }
    }
}
