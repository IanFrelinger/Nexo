namespace Nexo.Infrastructure.Enums
{
/// <summary>
/// Represents the completion of a command indicating that the operation has finished successfully.
/// This type is typically used to signify the transition of a command to a successful final state.
/// </summary>
public enum CommandOutputType
{
    /// <summary>
    /// Represents the standard output type for a command execution.
    /// </summary>
    /// <remarks>
    /// This type is used to specify that the output content is the regular output
    /// generated during the execution of a command.
    /// </remarks>
    StandardOutput,

    /// <summary>
    /// Represents the standard error output type for a command execution.
    /// </summary>
    /// <remarks>
    /// This type is used to specify that the output content is related to errors or diagnostic information
    /// produced during the execution of a command.
    /// </remarks>
    StandardError,

    /// <summary>
    /// Represents informational output from a command that provides additional details,
    /// such as logs, messages, or non-critical updates, without affecting the command's execution status.
    /// </summary>
    Info,

    /// <summary>
    /// Represents the progress output type for a command execution.
    /// </summary>
    /// <remarks>
    /// This type is used to convey intermediate progress information
    /// during the execution of a command.
    /// </remarks>
    Progress,

    /// <summary>
    /// Represents the completion output type of command execution.
    /// </summary>
    /// <remarks>
    /// This type is used to indicate that the command has successfully finished its execution.
    /// </remarks>
    Completed,

    /// <summary>
    /// Represents output from a command that has failed to execute successfully.
    /// This type indicates an error or failure state during command execution.
    /// </summary>
    Failed
}
}