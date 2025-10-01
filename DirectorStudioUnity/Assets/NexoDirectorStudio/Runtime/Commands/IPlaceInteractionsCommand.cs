using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for placing interactions in the world.
    /// </summary>
    public interface IPlaceInteractionsCommand : ICommand<IPlaceInteractionsCommand.Input, InteractionGraph>
    {
        public sealed record Input(WorldLayout WorldLayout, GamePlan GamePlan);
    }
}
