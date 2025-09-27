using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles monitor command creation and execution
    /// </summary>
    public class MonitorCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MonitorCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateMonitorCommand()
        {
            var monitorCommand = new Command("monitor", "Monitor adaptations in real-time");
            var durationOption = new Option<int>("--duration", () => 300, "Duration in seconds to monitor");
            monitorCommand.AddOption(durationOption);
            monitorCommand.SetHandler(async (duration) =>
            {
                var monitorHandler = new MonitorHandler(_serviceProvider);
                await monitorHandler.MonitorAdaptations(duration);
            }, durationOption);
            return monitorCommand;
        }
    }
}
