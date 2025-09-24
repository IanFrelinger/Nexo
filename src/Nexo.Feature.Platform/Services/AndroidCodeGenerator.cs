using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services.Android.Generators;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// Orchestrator for Android code generation that delegates to specialized generators.
    /// </summary>
    public class AndroidCodeGenerator : IAndroidCodeGenerator
    {
        private readonly ILogger<AndroidCodeGenerator> _logger;
        private readonly JetpackComposeCodeGenerator _composeGenerator;
        private readonly RoomDatabaseCodeGenerator _roomGenerator;
        private readonly ViewModelCodeGenerator _viewModelGenerator;
        private readonly RepositoryCodeGenerator _repositoryGenerator;
        private readonly ServiceCodeGenerator _serviceGenerator;

        public AndroidCodeGenerator(ILogger<AndroidCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _composeGenerator = new JetpackComposeCodeGenerator(logger);
            _roomGenerator = new RoomDatabaseCodeGenerator(logger);
            _viewModelGenerator = new ViewModelCodeGenerator(logger);
            _repositoryGenerator = new RepositoryCodeGenerator(logger);
            _serviceGenerator = new ServiceCodeGenerator(logger);
        }

        public async Task<AndroidCodeGenerationResult> GenerateJetpackComposeCodeAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Android Jetpack Compose code generation");

            var result = new AndroidCodeGenerationResult
            {
                GeneratedCode = new AndroidGeneratedCode()
            };

            try
            {
                // Generate Jetpack Compose files
                if (androidOptions.EnableJetpackCompose)
                {
                    var composeFiles = await _composeGenerator.GenerateComposeFilesAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.ComposeFiles.AddRange(composeFiles);
                }

                // Generate Room database files
                if (androidOptions.EnableRoomDatabase)
                {
                    var roomFiles = await _roomGenerator.GenerateRoomFilesAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.RoomFiles.AddRange(roomFiles);
                }

                // Generate ViewModel files
                var viewModelFiles = await _viewModelGenerator.GenerateViewModelFilesAsync(applicationLogic, androidOptions, cancellationToken);
                result.GeneratedCode.ViewModelFiles.AddRange(viewModelFiles);

                // Generate Repository files
                var repositoryFiles = await _repositoryGenerator.GenerateRepositoryFilesAsync(applicationLogic, androidOptions, cancellationToken);
                result.GeneratedCode.RepositoryFiles.AddRange(repositoryFiles);

                // Generate Service files
                var serviceFiles = await _serviceGenerator.GenerateServiceFilesAsync(applicationLogic, androidOptions, cancellationToken);
                result.GeneratedCode.ServiceFiles.AddRange(serviceFiles);

                result.IsSuccess = true;
                result.Message = "Android code generation completed successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Android code");
                result.IsSuccess = false;
                result.Message = $"Android code generation failed: {ex.Message}";
                return result;
            }
        }
    }
}
