using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// Data models and DTOs for AI chat functionality
    /// </summary>
    public partial class AIChatCommands
    {
        // Data models are defined here for AI chat functionality
    }

    /// <summary>
    /// Chat message model.
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
