using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services.Desktop.Generators;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// Orchestrator for desktop code generation that delegates to specialized generators.
    /// </summary>
    public class DesktopCodeGenerator : IDesktopCodeGenerator
    {
        private readonly ILogger<DesktopCodeGenerator> _logger;
        private readonly WPFCodeGenerator _wpfGenerator;
        private readonly WinFormsCodeGenerator _winFormsGenerator;
        private readonly ConsoleCodeGenerator _consoleGenerator;
        private readonly AvaloniaCodeGenerator _avaloniaGenerator;
        private readonly EtoCodeGenerator _etoGenerator;

        public DesktopCodeGenerator(ILogger<DesktopCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wpfGenerator = new WPFCodeGenerator(logger);
            _winFormsGenerator = new WinFormsCodeGenerator(logger);
            _consoleGenerator = new ConsoleCodeGenerator(logger);
            _avaloniaGenerator = new AvaloniaCodeGenerator(logger);
            _etoGenerator = new EtoCodeGenerator(logger);
        }

        public async Task<DesktopCodeGenerationResult> GenerateCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new DesktopCodeGenerationResult
                    {
                        Success = false,
                        Errors = { "Invalid request parameters" },
                        Platform = string.Empty,
                        ApplicationType = string.Empty,
                        UIFramework = string.Empty
                    };
                }

                _logger.LogInformation("Starting desktop code generation for {Platform} {ApplicationType}", 
                    request.Platform, request.ApplicationType);

                // Validate request
                if (!ValidateRequest(request))
                {
                    return new DesktopCodeGenerationResult
                    {
                        Success = false,
                        Errors = { "Invalid request parameters" },
                        Platform = request.Platform,
                        ApplicationType = request.ApplicationType,
                        UIFramework = request.UIFramework
                    };
                }

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = request.UIFramework,
                    Success = true
                };

                // Generate code based on UI framework
                switch (request.UIFramework.ToLower())
                {
                    case "wpf":
                        result = await _wpfGenerator.GenerateWPFCodeAsync(request);
                        break;
                    case "winforms":
                        result = await _winFormsGenerator.GenerateWinFormsCodeAsync(request);
                        break;
                    case "console":
                        result = await _consoleGenerator.GenerateConsoleCodeAsync(request);
                        break;
                    case "avalonia":
                        result = await _avaloniaGenerator.GenerateAvaloniaCodeAsync(request);
                        break;
                    case "eto":
                        result = await _etoGenerator.GenerateEtoCodeAsync(request);
                        break;
                    default:
                        result.Success = false;
                        result.Errors.Add($"Unsupported UI framework: {request.UIFramework}");
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating desktop code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"Desktop code generation failed: {ex.Message}" },
                    Platform = request?.Platform ?? string.Empty,
                    ApplicationType = request?.ApplicationType ?? string.Empty,
                    UIFramework = request?.UIFramework ?? string.Empty
                };
            }
        }

        private bool ValidateRequest(DesktopCodeGenerationRequest request)
        {
            return !string.IsNullOrEmpty(request.Platform) &&
                   !string.IsNullOrEmpty(request.ApplicationType) &&
                   !string.IsNullOrEmpty(request.UIFramework);
        }
    }
}
