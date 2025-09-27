// This file has been refactored into specialized chat command classes:
// - Core/ChatCommand.cs - Main orchestrator for chat command functionality
// - Commands/InteractiveCommandHandler.cs - Interactive chat command handling
// - Commands/QuickCommandHandler.cs - Quick chat command handling
// - Commands/CodeCommandHandler.cs - Code chat command handling
// - Handlers/InteractiveChatHandler.cs - Interactive chat session logic
// - Handlers/QuickChatHandler.cs - Quick chat processing
// - Handlers/CodeChatHandler.cs - Code chat processing
// - Handlers/ChatSessionHandler.cs - Chat session management
// - Handlers/ChatCommandHandler.cs - Chat command processing
// - Utilities/ModelSelector.cs - Model selection logic
// - Utilities/CodeHighlighter.cs - Code highlighting functionality
// - Utilities/HelpHandler.cs - Help display functionality
// - Models/ChatMessage.cs - Chat message model

// Re-export all classes for backward compatibility
using Nexo.Infrastructure.Commands.Chat.Core;
using Nexo.Infrastructure.Commands.Chat.Commands;
using Nexo.Infrastructure.Commands.Chat.Handlers;
using Nexo.Infrastructure.Commands.Chat.Utilities;
using Nexo.Infrastructure.Commands.Chat.Models;

namespace Nexo.Infrastructure.Commands
{
    // All chat command classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
