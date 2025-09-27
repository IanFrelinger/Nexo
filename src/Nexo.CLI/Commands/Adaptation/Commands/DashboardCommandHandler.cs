using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles dashboard command creation and execution
    /// </summary>
    public partial class DashboardCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public DashboardCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateDashboardCommand()
        {
            var dashboardCommand = new Command("dashboard", "Show real-time adaptation dashboard");
            dashboardCommand.SetHandler(async () =>
            {
                var dashboardHandler = new DashboardHandler(_serviceProvider);
                await dashboardHandler.ShowDashboard();
            });
            return dashboardCommand;
        }
    }
}
