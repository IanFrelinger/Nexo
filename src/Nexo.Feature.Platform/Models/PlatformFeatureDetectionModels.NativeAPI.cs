using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Native API Integration Models
    /// </summary>
    public partial class NativeAPIInitializationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<string> AvailableAPIs { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime InitializationTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of a native API call.
    /// </summary>
    public partial class NativeAPICallResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string APIName { get; set; } = string.Empty;
        public object? Result { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of native API availability check.
    /// </summary>
    public partial class NativeAPIAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> AlternativeAPIs { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of available APIs retrieval.
    /// </summary>
    public partial class AvailableAPIsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<NativeAPIInfo> AvailableAPIs { get; set; } = new List<NativeAPIInfo>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Information about a native API.
    /// </summary>
    public partial class NativeAPIInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public APIType Type { get; set; }
        public bool RequiresPermission { get; set; }
        public List<PermissionType> RequiredPermissions { get; set; } = new List<PermissionType>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of permission request.
    /// </summary>
    public partial class PermissionRequestResult
    {
        public bool IsGranted { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PermissionType PermissionType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> RequiredActions { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of permission status check.
    /// </summary>
    public partial class PermissionStatusResult
    {
        public bool HasPermission { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PermissionStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<PermissionType> GrantedPermissions { get; set; } = new List<PermissionType>();
        public List<PermissionType> DeniedPermissions { get; set; } = new List<PermissionType>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API handler registration.
    /// </summary>
    public partial class APIHandlerRegistrationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string APIName { get; set; } = string.Empty;
        public bool IsRegistered { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API abstraction layer retrieval.
    /// </summary>
    public partial class APIAbstractionLayerResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> AbstractionLayer { get; set; } = new Dictionary<string, object>();
        public List<string> SupportedAPIs { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API compatibility validation.
    /// </summary>
    public partial class APICompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public List<string> APIs { get; set; } = new List<string>();
        public List<PlatformType> Platforms { get; set; } = new List<PlatformType>();
        public Dictionary<string, Dictionary<PlatformType, bool>> CompatibilityMatrix { get; set; } = new Dictionary<string, Dictionary<PlatformType, bool>>();
        public List<APICompatibilityIssue> Issues { get; set; } = new List<APICompatibilityIssue>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents an API compatibility issue.
    /// </summary>
    public partial class APICompatibilityIssue
    {
        public string APIName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public APICompatibilityIssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> Solutions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of native API disposal.
    /// </summary>
    public partial class NativeAPIDisposalResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DisposedAPIs { get; set; }
        public List<string> DisposedResources { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metadata for API handlers.
    /// </summary>
    public partial class APIHandlerMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<PlatformType> SupportedPlatforms { get; set; } = new List<PlatformType>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }
}
