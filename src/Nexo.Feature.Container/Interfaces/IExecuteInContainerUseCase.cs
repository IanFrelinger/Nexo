using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Nexo.Shared.Models;
using Nexo.Feature.Container.Models;

namespace Nexo.Feature.Container.Interfaces
{
    /// <summary>
    /// Use case for executing commands in a container.
    /// </summary>
    public interface IExecuteInContainerUseCase
    {
        /// <summary>
        /// Executes a command in a container.
        /// </summary>
        /// <param name="request">The execution request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command result.</returns>
        Task<CommandResult> ExecuteAsync(ExecuteInContainerRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}