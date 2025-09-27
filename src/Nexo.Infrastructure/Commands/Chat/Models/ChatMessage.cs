namespace Nexo.Infrastructure.Commands.Chat.Models
{
    /// <summary>
    /// Chat message model
    /// </summary>
    public partial class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
