using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Feature.Platform.Services.iOS.Generators;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// Orchestrator for iOS code generation that delegates to specialized generators.
    /// </summary>
    public class IOSCodeGenerator : IIOSCodeGenerator
    {
        private readonly ILogger<IOSCodeGenerator> _logger;
        private readonly SwiftUIGenerator _swiftUIGenerator;
        private readonly CoreDataGenerator _coreDataGenerator;
        private readonly MetalGenerator _metalGenerator;
        private readonly CombineGenerator _combineGenerator;
        private readonly CloudKitGenerator _cloudKitGenerator;

        public IOSCodeGenerator(ILogger<IOSCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _swiftUIGenerator = new SwiftUIGenerator(logger);
            _coreDataGenerator = new CoreDataGenerator(logger);
            _metalGenerator = new MetalGenerator(logger);
            _combineGenerator = new CombineGenerator(logger);
            _cloudKitGenerator = new CloudKitGenerator(logger);
        }

        public async Task<IOSCodeGenerationResult> GenerateSwiftUICodeAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS SwiftUI code generation");

            try
            {
                var result = new IOSCodeGenerationResult
                {
                    GeneratedCode = new IOSGeneratedCode()
                };

                if (iosOptions.EnableSwiftUI)
                {
                    var swiftUIFiles = await _swiftUIGenerator.GenerateSwiftUIFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.SwiftUIFiles.AddRange(swiftUIFiles);
                }

                if (iosOptions.EnableCoreData)
                {
                    var coreDataFiles = await _coreDataGenerator.GenerateCoreDataFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.CoreDataFiles.AddRange(coreDataFiles);
                }

                if (iosOptions.EnableMetal)
                {
                    var metalFiles = await _metalGenerator.GenerateMetalFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.MetalFiles.AddRange(metalFiles);
                }

                if (iosOptions.EnableCombine)
                {
                    var combineFiles = await _combineGenerator.GenerateCombineFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.CombineFiles.AddRange(combineFiles);
                }

                if (iosOptions.EnableCloudKit)
                {
                    var cloudKitFiles = await _cloudKitGenerator.GenerateCloudKitFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.CloudKitFiles.AddRange(cloudKitFiles);
                }

                result.IsSuccess = true;
                result.Message = "iOS code generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating iOS code");
                return new IOSCodeGenerationResult
                {
                    IsSuccess = false,
                    Message = $"iOS code generation failed: {ex.Message}",
                    Errors = { ex.Message }
                };
            }
        }
    }
}
