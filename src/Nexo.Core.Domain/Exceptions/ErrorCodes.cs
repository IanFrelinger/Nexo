namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Error codes for better error identification and troubleshooting.
/// </summary>
public static class ErrorCodes
{
    // Analysis errors (1000-1999)
    public const string AnalysisPathNotFound = "ANALYSIS_1001";
    public const string AnalysisUnauthorizedAccess = "ANALYSIS_1002";
    public const string AnalysisInvalidAssembly = "ANALYSIS_1003";
    public const string AnalysisRuleFailure = "ANALYSIS_1004";

    // Validation errors (2000-2999)
    public const string ValidationNoTestProjects = "VALIDATION_2001";
    public const string ValidationTestExecutionFailed = "VALIDATION_2002";
    public const string ValidationTrxParseFailed = "VALIDATION_2003";
    public const string ValidationTimeout = "VALIDATION_2004";

    // Agent errors (3000-3999)
    public const string AgentNotFound = "AGENT_3001";
    public const string AgentExecutionFailed = "AGENT_3002";
    public const string AgentTimeout = "AGENT_3003";
    public const string AgentInvalidInput = "AGENT_3004";

    // Configuration errors (4000-4999)
    public const string ConfigFileNotFound = "CONFIG_4001";
    public const string ConfigInvalidFormat = "CONFIG_4002";
    public const string ConfigInvalidValue = "CONFIG_4003";

    // General errors (5000-5999)
    public const string GeneralUnexpectedError = "GENERAL_5001";
    public const string GeneralInvalidArgument = "GENERAL_5002";
}

