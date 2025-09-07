using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS cluster credentials
/// </summary>
public record AKSClusterCredentials
{
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string KubeConfig { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
    public string CertificateAuthorityData { get; init; } = string.Empty;
    public string ClientCertificateData { get; init; } = string.Empty;
    public string ClientKeyData { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
} 