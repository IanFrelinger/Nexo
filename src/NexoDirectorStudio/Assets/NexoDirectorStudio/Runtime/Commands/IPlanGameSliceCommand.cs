using Nexo.Core.Application.Interfaces;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to plan a game slice from a design brief.
    /// This is the first step in the Director Studio pipeline.
    /// </summary>
    public interface IPlanGameSliceCommand : ICommand<DesignBrief, GamePlan>
    {
    }
}
