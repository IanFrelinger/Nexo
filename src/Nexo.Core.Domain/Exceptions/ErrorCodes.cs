namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Error codes for better error identification and troubleshooting.
/// 
/// Provides structured error codes organized by domain:
/// - Analysis errors (1000-1999): Code/assembly analysis failures
/// - Validation errors (2000-2999): Test and validation failures
/// - Agent errors (3000-3999): Agent execution failures
/// - Configuration errors (4000-4999): Configuration management failures
/// - General errors (5000-5999): General application errors
/// 
/// Used throughout the domain layer for consistent error identification.
/// </summary>
public static class ErrorCodes
{
    // Analysis errors (1000-1999)
    /// <summary>Analysis target path was not found.</summary>
    public const string AnalysisPathNotFound = "ANALYSIS_1001";
    /// <summary>Analysis could not access the target path.</summary>
    public const string AnalysisUnauthorizedAccess = "ANALYSIS_1002";
    /// <summary>Assembly could not be loaded or parsed for analysis.</summary>
    public const string AnalysisInvalidAssembly = "ANALYSIS_1003";
    /// <summary>An analysis rule failed during evaluation.</summary>
    public const string AnalysisRuleFailure = "ANALYSIS_1004";

    // Validation errors (2000-2999)
    /// <summary>No test projects were discovered for validation.</summary>
    public const string ValidationNoTestProjects = "VALIDATION_2001";
    /// <summary>Test execution failed before results could be parsed.</summary>
    public const string ValidationTestExecutionFailed = "VALIDATION_2002";
    /// <summary>TRX test result parsing failed.</summary>
    public const string ValidationTrxParseFailed = "VALIDATION_2003";
    /// <summary>Validation exceeded the configured timeout.</summary>
    public const string ValidationTimeout = "VALIDATION_2004";

    // Agent errors (3000-3999)
    /// <summary>Requested agent was not found in the registry.</summary>
    public const string AgentNotFound = "AGENT_3001";
    /// <summary>Agent execution failed with a domain error.</summary>
    public const string AgentExecutionFailed = "AGENT_3002";
    /// <summary>Agent execution exceeded the configured timeout.</summary>
    public const string AgentTimeout = "AGENT_3003";
    /// <summary>Agent received invalid or incomplete input.</summary>
    public const string AgentInvalidInput = "AGENT_3004";

    // Configuration errors (4000-4999)
    /// <summary>Expected configuration file was not found.</summary>
    public const string ConfigFileNotFound = "CONFIG_4001";
    /// <summary>Configuration file could not be parsed.</summary>
    public const string ConfigInvalidFormat = "CONFIG_4002";
    /// <summary>Configuration value failed validation.</summary>
    public const string ConfigInvalidValue = "CONFIG_4003";

    // General errors (5000-5999)
    /// <summary>Unexpected domain error without a more specific code.</summary>
    public const string GeneralUnexpectedError = "GENERAL_5001";
    /// <summary>Method received an invalid argument.</summary>
    public const string GeneralInvalidArgument = "GENERAL_5002";
}

