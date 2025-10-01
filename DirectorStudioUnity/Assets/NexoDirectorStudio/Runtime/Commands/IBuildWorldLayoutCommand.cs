using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for building world layouts from game plans.
    /// </summary>
    public interface IBuildWorldLayoutCommand : ICommand<IBuildWorldLayoutCommand.Input, WorldLayout>
    {
        public sealed record Input(GamePlan GamePlan, WorldLayoutOptions Options = null);
    }
}
