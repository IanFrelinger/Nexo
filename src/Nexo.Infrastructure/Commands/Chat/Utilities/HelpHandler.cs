using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Utilities
{
    /// <summary>
    /// Handles help display functionality
    /// </summary>
    public partial class HelpHandler
    {
        /// <summary>
        /// Shows chat help information
        /// </summary>
        public void ShowChatHelp()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]AI Chat Commands:[/]");
            AnsiConsole.MarkupLine("  [blue]exit[/]     - Exit the chat session");
            AnsiConsole.MarkupLine("  [blue]help[/]     - Show this help message");
            AnsiConsole.MarkupLine("  [blue]clear[/]    - Clear chat history");
            AnsiConsole.MarkupLine("  [blue]/model <name>[/] - Switch to specific model");
            AnsiConsole.MarkupLine("  [blue]/context <text>[/] - Set context for the session");
            AnsiConsole.MarkupLine("  [blue]/history[/] - Show chat history");
            AnsiConsole.MarkupLine("  [blue]/stats[/]   - Show model statistics");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Idea Tips:[/]");
            AnsiConsole.MarkupLine("  - Be specific in your questions");
            AnsiConsole.MarkupLine("  - Use code blocks for code examples");
            AnsiConsole.MarkupLine("  - Ask follow-up questions for clarification");
            AnsiConsole.WriteLine();
        }
    }
}
