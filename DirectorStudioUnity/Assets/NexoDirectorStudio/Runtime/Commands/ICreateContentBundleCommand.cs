using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for creating content bundles.
    /// </summary>
    public interface ICreateContentBundleCommand : ICommand<ICreateContentBundleCommand.Input, ContentBundle>
    {
        public sealed record Input(InteractionGraph InteractionGraph, GamePlan GamePlan);
    }
}
