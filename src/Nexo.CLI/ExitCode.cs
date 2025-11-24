namespace Nexo.CLI;

/// <summary>
/// Exit codes for CLI operations.
/// </summary>
public enum ExitCode
{
    Ok = 0,
    ValidationFailed = 2,
    PolicyViolation = 3,
    UnexpectedError = 10
}

