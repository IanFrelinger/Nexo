namespace Nexo.Contracts;

public sealed record AgentRequest(string AgentName, string? InputFilePath = null);
public sealed record AgentResponse(bool Success, string? Message, object? Output);

public sealed record ValidationRequest(string? Filter = null);
public sealed record ValidationResponse(bool Passed, string? Message, int TotalTests, int PassedTests, int FailedTests);

public sealed record OrchestrationRequest(
    string Request,
    string? RuntimeSpecJson = null,
    string? PreferModel = null,
    string? Provider = null,
    string? BarrierLevel = null,
    string? PreferredRegion = null);
public sealed record OrchestrationResponse(
    bool Success,
    string? Summary,
    object? Output,
    int? Conflicts = null,
    int? Escalations = null,
    string? ErrorCode = null);

public sealed record StatusResponse(
    string Mode,
    string Message,
    int TotalAgents,
    int ActiveAgents,
    string? ActivePackId = null,
    string? ActivePackVersion = null);

public sealed record ExecutionBuildRequest(string DockerfilePath, string ImageTag, string ContextPath, Dictionary<string, string>? BuildArgs = null);
public sealed record ExecutionBuildResponse(bool Success, string? ErrorMessage, double DurationMs);

public sealed record ExecutionRunRequest(string ImageTag, string[] Command, Dictionary<string, string>? EnvironmentVariables = null, Dictionary<string, string>? VolumeMounts = null, string? WorkingDirectory = null);
public sealed record ExecutionRunResponse(bool Success, int ExitCode, string StandardOutput, string StandardError, string? ContainerId, double DurationMs);

public sealed record TrustStatusResponse(bool IsPaused, string Status, string? ActivePolicyPackId, string? ActivePolicyPackVersion);
