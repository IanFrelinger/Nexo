namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Specifies the types of requests that can be handled by an agent in the system.
    /// </summary>
    public enum AgentRequestType
    {
        /// <summary>
        /// Represents a request type indicating that a task is to be assigned to an agent.
        /// This type is used to delegate specific tasks to a suitable agent by evaluating
        /// their capabilities or availability. Once assigned, the agent is expected to
        /// handle the task as per their implementation of the processing logic.
        /// </summary>
        TaskAssignment,

        /// <summary>
        /// Represents a request for a code review task.
        /// This request type typically involves reviewing code for quality,
        /// correctness, adherence to coding standards, and identifying potential issues
        /// or improvements.
        /// </summary>
        CodeReview,

        /// <summary>
        /// Represents a request type for proposing or evaluating the architectural design of a system, component, or feature.
        /// Typically, involves tasks such as creating design blueprints, reviewing architectural decisions,
        /// or suggesting improvements to ensure system scalability, maintainability, and alignment with project goals.
        /// </summary>
        ArchitectureDesign,

        /// <summary>
        /// Represents an agent request type where the task involves the creation of test cases,
        /// frameworks, or strategies to ensure quality assurance and validate the functionality
        /// of a system or component.
        /// </summary>
        TestCreation,

        /// <summary>
        /// Represents a request type that specifies the resolution of a bug within the system or application.
        /// Used by agents to identify and process actions related to diagnosing and fixing errors or defects in the codebase.
        /// </summary>
        BugFix,

        /// <summary>
        /// Represents a request type indicating that an agent is tasked with implementing
        /// a specific feature. This type is used to assign the development of a new
        /// functionality or enhancement within the system. The agent is responsible for
        /// translating requirements into a working implementation that integrates seamlessly
        /// within the application's existing structure.
        /// </summary>
        FeatureImplementation,

        /// <summary>
        /// Represents the type of agent request for generating or updating documentation.
        /// This value indicates that the agent is responsible for handling tasks related to
        /// the creation, maintenance, or review of documentation within the context of the system or application.
        /// </summary>
        Documentation,

        /// <summary>
        /// Represents a request type where the agent is tasked with analyzing specific data or content.
        /// Typically, involves evaluating, interpreting, or extracting insights based on the given input.
        /// This request type can be used for activities such as data examination, problem assessment, or situational analysis.
        /// </summary>
        Analysis,

        /// <summary>
        /// Represents a request type indicating collaboration between agents or teams.
        /// This type is used to facilitate joint efforts where multiple agents contribute to
        /// achieving a shared goal. Collaboration requests may involve discussions,
        /// shared tasks, or coordinated problem-solving activities.
        /// </summary>
        Collaboration,

        /// <summary>
        /// Represents a request type indicating a status update from an agent about their progress,
        /// state, or any relevant information about ongoing tasks or operations.
        /// </summary>
        StatusUpdate
    }
}