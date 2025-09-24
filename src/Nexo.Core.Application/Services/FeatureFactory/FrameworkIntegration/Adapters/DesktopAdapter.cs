using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration.Adapters.Desktop;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration.Adapters
{
    /// <summary>
    /// Orchestrator for desktop framework code generation that delegates to specialized adapters.
    /// </summary>
    public class DesktopAdapter
    {
        private readonly ILogger<DesktopAdapter> _logger;
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ConsoleAdapter _consoleAdapter;
        private readonly WPFAdapter _wpfAdapter;
        private readonly WinFormsAdapter _winFormsAdapter;
        private readonly AvaloniaAdapter _avaloniaAdapter;
        private readonly EtoAdapter _etoAdapter;

        public DesktopAdapter(
            ILogger<DesktopAdapter> logger,
            IAIRuntimeSelector runtimeSelector,
            ConsoleAdapter consoleAdapter,
            WPFAdapter wpfAdapter,
            WinFormsAdapter winFormsAdapter,
            AvaloniaAdapter avaloniaAdapter,
            EtoAdapter etoAdapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _consoleAdapter = consoleAdapter ?? throw new ArgumentNullException(nameof(consoleAdapter));
            _wpfAdapter = wpfAdapter ?? throw new ArgumentNullException(nameof(wpfAdapter));
            _winFormsAdapter = winFormsAdapter ?? throw new ArgumentNullException(nameof(winFormsAdapter));
            _avaloniaAdapter = avaloniaAdapter ?? throw new ArgumentNullException(nameof(avaloniaAdapter));
            _etoAdapter = etoAdapter ?? throw new ArgumentNullException(nameof(etoAdapter));
        }

        public async Task<ConsoleResult> GenerateConsoleCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _consoleAdapter.GenerateConsoleCodeAsync(applicationLogic, cancellationToken);
        }

        public async Task<WPFResult> GenerateWPFCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _wpfAdapter.GenerateWPFCodeAsync(applicationLogic, cancellationToken);
        }

        public async Task<WinFormsResult> GenerateWinFormsCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _winFormsAdapter.GenerateWinFormsCodeAsync(applicationLogic, cancellationToken);
        }

        public async Task<AvaloniaResult> GenerateAvaloniaCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _avaloniaAdapter.GenerateAvaloniaCodeAsync(applicationLogic, cancellationToken);
        }

        public async Task<EtoResult> GenerateEtoCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
        {
            return await _etoAdapter.GenerateEtoCodeAsync(applicationLogic, cancellationToken);
        }

        public async Task<DesktopFrameworkResult> GenerateFrameworkCodeAsync(
            ApplicationLogicResult applicationLogic,
            DesktopFrameworkType frameworkType,
            CancellationToken cancellationToken = default)
        {
            return frameworkType switch
            {
                DesktopFrameworkType.Console => await GenerateConsoleCodeAsync(applicationLogic, cancellationToken),
                DesktopFrameworkType.WPF => await GenerateWPFCodeAsync(applicationLogic, cancellationToken),
                DesktopFrameworkType.WinForms => await GenerateWinFormsCodeAsync(applicationLogic, cancellationToken),
                DesktopFrameworkType.Avalonia => await GenerateAvaloniaCodeAsync(applicationLogic, cancellationToken),
                DesktopFrameworkType.Eto => await GenerateEtoCodeAsync(applicationLogic, cancellationToken),
                _ => throw new ArgumentException($"Unsupported desktop framework type: {frameworkType}")
            };
        }
    }
}
