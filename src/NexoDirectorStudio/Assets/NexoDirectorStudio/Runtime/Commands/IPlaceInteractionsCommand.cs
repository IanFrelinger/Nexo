using Nexo.Core.Application.Interfaces;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to place interactions in a world layout.
    /// This creates the interaction graph with triggers, events, and quests.
    /// </summary>
    public interface IPlaceInteractionsCommand : ICommand<WorldLayout, InteractionGraph>
    {
    }
}
