namespace Nexo.CLI;

/// <summary>
/// Exit codes for CLI operations.
/// 
/// Defines standard exit codes used by the CLI:
/// - Ok: Operation completed successfully
/// - ValidationFailed: Validation errors occurred
/// - PolicyViolation: Policy violations detected
/// - UnexpectedError: Unexpected error occurred
/// 
/// Used to communicate operation results to calling processes.
/// </summary>
public enum ExitCode
{
    Ok = 0,
    ValidationFailed = 2,
    PolicyViolation = 3,
    UnexpectedError = 10
}

