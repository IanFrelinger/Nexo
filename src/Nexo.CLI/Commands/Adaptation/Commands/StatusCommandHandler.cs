using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles status command creation and execution
    /// </summary>
    public class StatusCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public StatusCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateStatusCommand()
        {
            var statusCommand = new Command("status", "Show current adaptation status");
            statusCommand.SetHandler(async () =>
            {
                var statusHandler = new StatusHandler(_serviceProvider);
                await statusHandler.ShowAdaptationStatus();
            });
            return statusCommand;
        }
    }
}
