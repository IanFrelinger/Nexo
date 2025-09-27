using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles engine command creation and execution
    /// </summary>
    public class EngineCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EngineCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateStartCommand()
        {
            var startCommand = new Command("start", "Start the adaptation engine");
            startCommand.SetHandler(async () =>
            {
                var engineHandler = new EngineHandler(_serviceProvider);
                await engineHandler.StartAdaptationEngine();
            });
            return startCommand;
        }

        public Command CreateStopCommand()
        {
            var stopCommand = new Command("stop", "Stop the adaptation engine");
            stopCommand.SetHandler(async () =>
            {
                var engineHandler = new EngineHandler(_serviceProvider);
                await engineHandler.StopAdaptationEngine();
            });
            return stopCommand;
        }
    }
}
