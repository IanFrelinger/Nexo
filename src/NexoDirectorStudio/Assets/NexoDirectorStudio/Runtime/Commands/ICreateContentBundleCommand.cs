using Nexo.Core.Application.Interfaces;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to create a content bundle from an interaction graph.
    /// This generates ScriptableObjects, prefabs, and other Unity assets.
    /// </summary>
    public interface ICreateContentBundleCommand : ICommand<InteractionGraph, ContentBundle>
    {
    }
}
