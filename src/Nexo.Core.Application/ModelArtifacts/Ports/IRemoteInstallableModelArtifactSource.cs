namespace Nexo.Core.Application.ModelArtifacts.Ports;

/// <summary>
/// Marker for catalog sources that query a remote registry of installable models
/// (as opposed to locally installed artifacts). Excluded from <see cref="IModelArtifactCatalogService.ListAllAsync"/>.
/// </summary>
public interface IRemoteInstallableModelArtifactSource : IModelArtifactCatalogSource
{
}
