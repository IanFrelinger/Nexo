namespace Nexo.Client;

// Request DTOs
public sealed record AgentRequest(string AgentName, string? InputFilePath = null);
public sealed record ValidationRequest(string? Filter = null);
public sealed record OrchestrationRequest(string Request);
public sealed record ExecutionBuildRequest(string DockerfilePath, string ImageTag, string ContextPath, Dictionary<string, string>? BuildArgs = null);
public sealed record ExecutionRunRequest(string ImageTag, string[] Command, Dictionary<string, string>? EnvironmentVariables = null, Dictionary<string, string>? VolumeMounts = null, string? WorkingDirectory = null);

// Response DTOs
public sealed record AgentResponse(bool Success, string? Message, object? Output);
public sealed record ValidationResponse(bool Passed, string? Message, int TotalTests, int PassedTests, int FailedTests);
public sealed record OrchestrationResponse(bool Success, string? Summary, object? Output);
public sealed record StatusResponse(string Mode, string Message);
public sealed record ExecutionBuildResponse(bool Success, string? ErrorMessage, double DurationMs);
public sealed record ExecutionRunResponse(bool Success, int ExitCode, string StandardOutput, string StandardError, string? ContainerId, double DurationMs);
