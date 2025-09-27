using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// AI chat interface commands for Phase 3.3 developer tools.
    /// Provides interactive AI chat capabilities for development assistance.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AIChatCommands
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIChatCommands> _logger;

        public AIChatCommands(IServiceProvider serviceProvider, ILogger<AIChatCommands> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates the AI chat command with all subcommands.
        /// </summary>
        public Command CreateAIChatCommand()
        {
            var chatCommand = new Command("chat", "Interactive AI chat interface");

            // Interactive chat
            chatCommand.AddCommand(CreateInteractiveChatCommand());

            // Code review chat
            chatCommand.AddCommand(CreateCodeReviewCommand());

            // Architecture chat
            chatCommand.AddCommand(CreateArchitectureChatCommand());

            // Debugging chat
            chatCommand.AddCommand(CreateDebuggingChatCommand());

            // Documentation chat
            chatCommand.AddCommand(CreateDocumentationChatCommand());

            return chatCommand;
        }
    }
}