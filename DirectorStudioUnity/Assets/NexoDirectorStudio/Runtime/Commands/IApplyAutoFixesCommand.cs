using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for applying auto-fixes to content bundle.
    /// </summary>
    public interface IApplyAutoFixesCommand : ICommand<IApplyAutoFixesCommand.Input, ContentBundle>
    {
        public sealed record Input(ContentBundle ContentBundle, AutoFixProposal AutoFixProposal);
    }
}
