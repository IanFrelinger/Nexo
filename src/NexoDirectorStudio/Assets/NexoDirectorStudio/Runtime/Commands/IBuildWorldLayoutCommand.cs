using Nexo.Core.Application.Interfaces;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to build a world layout from a game plan.
    /// This creates the spatial structure of the game slice.
    /// </summary>
    public interface IBuildWorldLayoutCommand : ICommand<IBuildWorldLayoutCommand.Input, WorldLayout>
    {
        public sealed record Input(GamePlan GamePlan, WorldLayoutOptions? Options = null);
    }
}
