using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration.Adapters;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration
{
    /// <summary>
    /// Service for adapting application logic to different frameworks using AI - orchestrates specialized adapters
    /// </summary>
    public class FrameworkAdapter : IFrameworkAdapter
    {
        private readonly ILogger<FrameworkAdapter> _logger;
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly WebApiAdapter _webApiAdapter;
        private readonly BlazorAdapter _blazorAdapter;
        private readonly MauiAdapter _mauiAdapter;
        private readonly DesktopAdapter _desktopAdapter;
        private readonly XamarinAdapter _xamarinAdapter;

        public FrameworkAdapter(ILogger<FrameworkAdapter> logger, IAIRuntimeSelector runtimeSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            
            // Initialize specialized adapters
            _webApiAdapter = new WebApiAdapter(logger, runtimeSelector);
            _blazorAdapter = new BlazorAdapter(logger, runtimeSelector);
            _mauiAdapter = new MauiAdapter(logger, runtimeSelector);
            _desktopAdapter = new DesktopAdapter(logger, runtimeSelector);
            _xamarinAdapter = new XamarinAdapter(logger, runtimeSelector);
        }

        /// <summary>
        /// Generates framework-specific code from application logic by delegating to specialized adapters
        /// </summary>
        public async Task<FrameworkResult> GenerateFrameworkCodeAsync(ApplicationLogicResult applicationLogic, FrameworkType framework, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating {Framework} code for application logic", framework);

                var result = new FrameworkResult
                {
                    Success = true,
                    Framework = framework,
                    GeneratedAt = DateTime.UtcNow
                };

                // Delegate to appropriate specialized adapter based on framework type
                switch (framework)
                {
                    case FrameworkType.WebApi:
                        var webApiResult = await _webApiAdapter.GenerateWebApiCodeAsync(applicationLogic, cancellationToken);
                        result.WebApiResult = webApiResult;
                        result.Success = webApiResult.Success;
                        result.Message = webApiResult.Message;
                        break;

                    case FrameworkType.Blazor:
                        var blazorResult = await _blazorAdapter.GenerateBlazorServerCodeAsync(applicationLogic, cancellationToken);
                        result.BlazorServerResult = blazorResult;
                        result.Success = blazorResult.Success;
                        result.Message = blazorResult.Message;
                        break;

                    case FrameworkType.BlazorWebAssembly:
                        var blazorWasmResult = await _blazorAdapter.GenerateBlazorWebAssemblyCodeAsync(applicationLogic, cancellationToken);
                        result.BlazorWebAssemblyResult = blazorWasmResult;
                        result.Success = blazorWasmResult.Success;
                        result.Message = blazorWasmResult.Message;
                        break;

                    case FrameworkType.Maui:
                        var mauiResult = await _mauiAdapter.GenerateMauiCodeAsync(applicationLogic, cancellationToken);
                        result.MauiResult = mauiResult;
                        result.Success = mauiResult.Success;
                        result.Message = mauiResult.Message;
                        break;

                    case FrameworkType.Console:
                        var consoleResult = await _desktopAdapter.GenerateConsoleCodeAsync(applicationLogic, cancellationToken);
                        result.ConsoleResult = consoleResult;
                        result.Success = consoleResult.Success;
                        result.Message = consoleResult.Message;
                        break;

                    case FrameworkType.Wpf:
                        var wpfResult = await _desktopAdapter.GenerateWpfCodeAsync(applicationLogic, cancellationToken);
                        result.WpfResult = wpfResult;
                        result.Success = wpfResult.Success;
                        result.Message = wpfResult.Message;
                        break;

                    case FrameworkType.WinForms:
                        var winFormsResult = await _desktopAdapter.GenerateWinFormsCodeAsync(applicationLogic, cancellationToken);
                        result.WinFormsResult = winFormsResult;
                        result.Success = winFormsResult.Success;
                        result.Message = winFormsResult.Message;
                        break;

                    case FrameworkType.Xamarin:
                        var xamarinResult = await _xamarinAdapter.GenerateXamarinCodeAsync(applicationLogic, cancellationToken);
                        result.XamarinResult = xamarinResult;
                        result.Success = xamarinResult.Success;
                        result.Message = xamarinResult.Message;
                        break;

                    default:
                        result.Success = false;
                        result.Message = $"Unsupported framework: {framework}";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Framework} code", framework);
                return new FrameworkResult
                {
                    Success = false,
                    Framework = framework,
                    Message = $"Framework code generation failed: {ex.Message}",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generates Web API code by delegating to Web API adapter
        /// </summary>
        public async Task<WebApiResult> GenerateWebApiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _webApiAdapter.GenerateWebApiCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates Blazor Server code by delegating to Blazor adapter
        /// </summary>
        public async Task<BlazorServerResult> GenerateBlazorServerCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _blazorAdapter.GenerateBlazorServerCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates Blazor WebAssembly code by delegating to Blazor adapter
        /// </summary>
        public async Task<BlazorWebAssemblyResult> GenerateBlazorWebAssemblyCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _blazorAdapter.GenerateBlazorWebAssemblyCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates MAUI code by delegating to MAUI adapter
        /// </summary>
        public async Task<MauiResult> GenerateMauiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _mauiAdapter.GenerateMauiCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates Console code by delegating to Desktop adapter
        /// </summary>
        public async Task<ConsoleResult> GenerateConsoleCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _desktopAdapter.GenerateConsoleCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates WPF code by delegating to Desktop adapter
        /// </summary>
        public async Task<WpfResult> GenerateWpfCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _desktopAdapter.GenerateWpfCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates WinForms code by delegating to Desktop adapter
        /// </summary>
        public async Task<WinFormsResult> GenerateWinFormsCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _desktopAdapter.GenerateWinFormsCodeAsync(applicationLogic, cancellationToken);
        }

        /// <summary>
        /// Generates Xamarin code by delegating to Xamarin adapter
        /// </summary>
        public async Task<XamarinResult> GenerateXamarinCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _xamarinAdapter.GenerateXamarinCodeAsync(applicationLogic, cancellationToken);
        }
    }
}
