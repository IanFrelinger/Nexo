using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles trigger command creation and execution
    /// </summary>
    public partial class TriggerCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public TriggerCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateTriggerCommand()
        {
            var triggerCommand = new Command("trigger", "Trigger a specific adaptation");
            var typeOption = new Option<string>("--type", "Type of adaptation to trigger");
            var contextOption = new Option<string>("--context", "Context for the adaptation");
            triggerCommand.AddOption(typeOption);
            triggerCommand.AddOption(contextOption);
            triggerCommand.SetHandler(async (type, context) =>
            {
                var triggerHandler = new TriggerHandler(_serviceProvider);
                await triggerHandler.TriggerAdaptation(type, context);
            }, typeOption, contextOption);
            return triggerCommand;
        }
    }
}
