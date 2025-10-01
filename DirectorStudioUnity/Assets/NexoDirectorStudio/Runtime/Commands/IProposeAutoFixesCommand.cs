using System;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for proposing auto-fixes for validation issues.
    /// </summary>
    public interface IProposeAutoFixesCommand : ICommand<IProposeAutoFixesCommand.Input, AutoFixProposal>
    {
        public sealed record Input(ContentBundle ContentBundle, ValidationReport ValidationReport);
    }
}
