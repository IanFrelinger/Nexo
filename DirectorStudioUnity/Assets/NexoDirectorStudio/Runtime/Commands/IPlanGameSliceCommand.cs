using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for generating game plans from design briefs.
    /// </summary>
    public interface IPlanGameSliceCommand : ICommand<IPlanGameSliceCommand.Input, GamePlan>
    {
        public sealed record Input(DesignBrief Brief, string GenreHint = null);
    }
}
