using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Hardware requirements and cloud fallback command
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class HardwareCommand : ICommand
    {
        private readonly IHardwareRequirementsChecker _hardwareChecker;
        private readonly ILogger<HardwareCommand> _logger;

        public HardwareCommand(
            IHardwareRequirementsChecker hardwareChecker,
            ILogger<HardwareCommand> logger)
        {
            _hardwareChecker = hardwareChecker ?? throw new ArgumentNullException(nameof(hardwareChecker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args.Length == 0)
            {
                await ShowHardwareDashboardAsync(cancellationToken);
                return;
            }

            var command = args[0].ToLowerInvariant();
            switch (command)
            {
                case "check":
                    await CheckSystemRequirementsAsync(cancellationToken);
                    break;

                case "cloud":
                    await ShowCloudOptionsAsync(cancellationToken);
                    break;

                case "recommend":
                    await ShowRecommendationsAsync(cancellationToken);
                    break;

                case "cost":
                    var hours = args.Length > 1 && int.TryParse(args[1], out var h) ? h : 160; // Default 160 hours/month
                    await ShowCostEstimatesAsync(hours, cancellationToken);
                    break;

                case "tiers":
                    await ShowPerformanceTiersAsync(cancellationToken);
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }
    }
}