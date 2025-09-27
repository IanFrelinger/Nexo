using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles environment command creation and execution
    /// </summary>
    public class EnvironmentCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EnvironmentCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateEnvironmentCommand()
        {
            var environmentCommand = new Command("environment", "Show environment adaptation status");
            environmentCommand.SetHandler(async () =>
            {
                var environmentHandler = new EnvironmentHandler(_serviceProvider);
                await environmentHandler.ShowEnvironmentStatus();
            });
            return environmentCommand;
        }
    }
}
