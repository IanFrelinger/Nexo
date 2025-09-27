using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Infrastructure.Services.Platform.Android.Generators;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Orchestrator for Android code generation that delegates to specialized generators.
    /// </summary>
    public partial class AndroidCodeGenerator : IAndroidCodeGenerator
    {
        private readonly ILogger<AndroidCodeGenerator> _logger;
        private readonly JetpackComposeGenerator _composeGenerator;
        private readonly RoomDatabaseGenerator _roomGenerator;
        private readonly ViewModelGenerator _viewModelGenerator;
        private readonly RepositoryGenerator _repositoryGenerator;
        private readonly ServiceGenerator _serviceGenerator;

        public AndroidCodeGenerator(ILogger<AndroidCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _composeGenerator = new JetpackComposeGenerator(logger);
            _roomGenerator = new RoomDatabaseGenerator(logger);
            _viewModelGenerator = new ViewModelGenerator(logger);
            _repositoryGenerator = new RepositoryGenerator(logger);
            _serviceGenerator = new ServiceGenerator(logger);
        }

        public async Task<AndroidGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Android code generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new AndroidGenerationResult
            {
                Success = false,
                Message = "Starting Android code generation"
            };

            try
            {
                // Generate Jetpack Compose UI
                if (options.UseCompose)
                {
                    var composeUI = await _composeGenerator.GenerateComposeUIAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(composeUI.Select(s => s.Name));
                }

                // Generate Room Database
                if (options.UseRoom)
                {
                    var roomDatabase = await _roomGenerator.GenerateRoomDatabaseAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(roomDatabase.Select(s => s.Name));
                }

                // Generate ViewModels
                var viewModels = await _viewModelGenerator.GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
                result.GeneratedFiles.AddRange(viewModels.Select(s => s.Name));

                // Generate Repositories
                var repositories = await _repositoryGenerator.GenerateRepositoriesAsync(applicationLogic, options, cancellationToken);
                result.GeneratedFiles.AddRange(repositories.Select(s => s.Name));

                // Generate Services
                var services = await _serviceGenerator.GenerateServicesAsync(applicationLogic, options, cancellationToken);
                result.GeneratedFiles.AddRange(services.Select(s => s.Name));

                result.Success = true;
                result.Message = "Android code generation completed successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Android code");
                result.Message = $"Android code generation failed: {ex.Message}";
                return result;
            }
        }
    }
}
