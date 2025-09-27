using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Commands
{
    /// <summary>
    /// Handles learning command creation and execution
    /// </summary>
    public partial class LearningCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public LearningCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Command CreateLearningCommand()
        {
            var learningCommand = new Command("learning", "Show learning insights");
            learningCommand.SetHandler(async () =>
            {
                var learningHandler = new LearningHandler(_serviceProvider);
                await learningHandler.ShowLearningInsights();
            });
            return learningCommand;
        }
    }
}
