using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Models;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for an agent entity capable of handling specific tasks,
    /// managing its lifecycle, and providing information about its capabilities, focus areas,
    /// and current state.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Gets the unique identifier for the agent.
        /// </summary>
        /// <remarks>
        /// The <c>Id</c> property is represented by an <see cref="AgentId"/> object,
        /// which encapsulates the unique identifier as a value object. This identifier
        /// is immutable and used to distinguish an agent within the system.
        /// </remarks>
        AgentId Id { get; }

        /// <summary>
        /// Gets the name of the agent.
        /// </summary>
        /// <remarks>
        /// The property represents the unique name of the agent as an instance of the <see cref="AgentName"/> value object.
        /// This value is immutable and serves as an identifier or display name for the agent within the system.
        /// </remarks>
        AgentName Name { get; }

        /// <summary>
        /// Represents the role assigned to an agent within the system.
        /// The role determines the agent's duties and responsibilities, as well as participation in various tasks and activities.
        /// </summary>
        /// <remarks>
        /// The role is represented by the <see cref="AgentRole"/> value object, which provides predefined standard roles such as
        /// Manager, Architect, Developer, and Tester. Custom roles can also be defined by utilizing the <see cref="AgentRole"/> constructor.
        /// </remarks>
        AgentRole Role { get; }

        /// <summary>
        /// Gets the operational status of the agent.
        /// </summary>
        /// <remarks>
        /// The <see cref="AgentStatus"/> indicates whether the agent is active, busy, inactive,
        /// or has encountered a failure. This property reflects the current state of the agent's operations.
        /// </remarks>
        AgentStatus Status { get; }

        /// <summary>
        /// A list representing the specific capabilities or competencies of an agent.
        /// These capabilities are strings that describe the agent's expertise, skills,
        /// or knowledge in certain areas which can influence the assignment of tasks
        /// and the calculation of task suitability.
        /// </summary>
        List<string> Capabilities { get; }

        /// <summary>
        /// Represents a collection of specific areas of expertise or concentration for an agent.
        /// This property is used to evaluate the suitability of the agent for tasks
        /// that align with its defined focus areas.
        /// </summary>
        /// <remarks>
        /// Focus areas are typically compared against task descriptions to determine
        /// how well an agent is suited to handle or prioritize a particular task within the system.
        /// The matching process often involves keyword extraction and comparison.
        /// </remarks>
        List<string> FocusAreas { get; }

        /// <summary>
        /// Processes a given request asynchronously and provides a corresponding response.
        /// </summary>
        /// <param name="request">The request to be processed, including its type, content, and context.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the response to the processed request.</returns>
        Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct);

        /// <summary>
        /// Determines whether the agent can handle the specified sprint task based on its focus areas or capabilities.
        /// </summary>
        /// <param name="task">The sprint task to evaluate, containing details like description, priority, and story points.</param>
        /// <param name="ct">The cancellation token to observe for task cancellation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the agent can handle the provided task.</returns>
        Task<bool> CanHandleTaskAsync(SprintTask task, CancellationToken ct);

        /// <summary>
        /// Starts the agent asynchronously by performing necessary initialization,
        /// registering the agent with the communication service, and updating its status to active.
        /// </summary>
        /// <param name="ct">A CancellationToken instance that can be used to observe and cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StartAsync(CancellationToken ct);

        /// <summary>
        /// Stops the agent asynchronously by performing operations such as updating its status
        /// and handling necessary cleanup processes.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the operation to complete.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous stop operation.</returns>
        Task StopAsync(CancellationToken ct);
    }
}