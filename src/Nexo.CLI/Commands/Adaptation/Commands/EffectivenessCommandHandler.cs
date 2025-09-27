using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles effectiveness command creation and execution
    /// </summary>
    public partial class EffectivenessCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EffectivenessCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateEffectivenessCommand()
        {
            var effectivenessCommand = new Command("effectiveness", "Show adaptation effectiveness");
            effectivenessCommand.SetHandler(async () =>
            {
                var effectivenessHandler = new EffectivenessHandler(_serviceProvider);
                await effectivenessHandler.ShowAdaptationEffectiveness();
            });
            return effectivenessCommand;
        }
    }
}
