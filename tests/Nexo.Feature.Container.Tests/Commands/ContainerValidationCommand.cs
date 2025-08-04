using Microsoft.Extensions.Logging;
using Nexo.Feature.Container.Interfaces;
using Nexo.Feature.Container.Models;
using Nexo.Feature.Container.UseCases;
using Nexo.Core.Application.Enums;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Container.Tests.Commands;

/// <summary>
/// Command for validating Container functionality with proper logging and timeouts.
/// </summary>
public class ContainerValidationCommand
{
    private readonly ILogger<ContainerValidationCommand> _logger;

    public ContainerValidationCommand(ILogger<ContainerValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Container interface definitions.
    /// </summary>
    public bool ValidateContainerInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Container interface validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var iContainerOrchestratorType = typeof(IContainerOrchestrator);
            var iContainerDevelopmentEnvironmentType = typeof(IContainerDevelopmentEnvironment);
            var iExecuteInContainerUseCaseType = typeof(IExecuteInContainerUseCase);
            var iContainerOrchestratorMethods = iContainerOrchestratorType.GetMethods();
            var iContainerDevelopmentEnvironmentMethods = iContainerDevelopmentEnvironmentType.GetMethods();
            var iExecuteInContainerUseCaseMethods = iExecuteInContainerUseCaseType.GetMethods();
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Container interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = iContainerOrchestratorType.IsInterface &&
                        iContainerDevelopmentEnvironmentType.IsInterface &&
                        iExecuteInContainerUseCaseType.IsInterface &&
                        iContainerOrchestratorMethods.Length > 0 &&
                        iContainerDevelopmentEnvironmentMethods.Length > 0 &&
                        iExecuteInContainerUseCaseMethods.Length > 0;
            _logger.LogInformation("Container interface validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Container interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Container model properties.
    /// </summary>
    public bool ValidateContainerModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Container model validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var devSession = new DevelopmentSession
            {
                Id = "session-1",
                Name = "Test Session",
                ProjectPath = "/project/path",
                ContainerName = "container-1",
                Configuration = new SessionConfiguration
                {
                    ProjectPath = "/project/path",
                    ContainerImage = "dotnet:8.0",
                    WorkingDirectory = "/src",
                    EnvironmentVariables = new Dictionary<string, string> { ["ENV1"] = "value1" },
                    VolumeMounts = new List<VolumeMount>(),
                    PortMappings = new List<PortMapping>(),
                    AdditionalPackages = new List<string>(),
                    Options = new SessionOptions()
                },
                CreatedAt = now,
                LastAccessedAt = now,
                Status = SessionStatus.Running,
                Metadata = new Dictionary<string, object> { ["meta"] = 123 },
                MountedVolumes = new List<string> { "/host/path:/container/path" },
                ExposedPorts = new List<int> { 8080 }
            };
            var execRequest = new ExecuteInContainerRequest(
                containerName: "container-1",
                command: new[] { "dotnet", "build" },
                workingDirectory: "/src",
                environmentVariables: new Dictionary<string, string> { ["ENV1"] = "value1" },
                timeoutMs: 10000
            );
            var portMapping = new PortMapping
            {
                HostPort = 8080,
                ContainerPort = 80,
                Protocol = "tcp"
            };
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Container model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = devSession.Id == "session-1" &&
                        devSession.Name == "Test Session" &&
                        devSession.Status == SessionStatus.Running &&
                        devSession.Metadata.ContainsKey("meta") &&
                        execRequest.ContainerName == "container-1" &&
                        execRequest.Command[0] == "dotnet" &&
                        execRequest.Command[1] == "build" &&
                        execRequest.WorkingDirectory == "/src" &&
                        execRequest.EnvironmentVariables.ContainsKey("ENV1") &&
                        execRequest.TimeoutMs == 10000 &&
                        portMapping.HostPort == 8080 &&
                        portMapping.ContainerPort == 80 &&
                        portMapping.Protocol == "tcp";
            _logger.LogInformation("Container model validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Container model validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Container use case functionality.
    /// </summary>
    public bool ValidateContainerUseCases(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Container use case validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var executeInContainerUseCaseType = typeof(ExecuteInContainerUseCase);
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Container use case validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = executeInContainerUseCaseType.IsClass &&
                        !executeInContainerUseCaseType.IsAbstract;
            _logger.LogInformation("Container use case validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Container use case validation");
            return false;
        }
    }
} 