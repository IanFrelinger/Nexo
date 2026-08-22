namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Port for storing and retrieving generated assets.
/// 
/// Defines the contract for asset storage adapters:
/// - Store individual asset files
/// - Store directories of assets
/// - Retrieve assets by ID
/// - Delete assets
/// 
/// Implementations (LocalAssetStorage, etc.) provide specific storage logic.
/// Used by asset generation agents to persist generated assets.
/// </summary>
public interface IAssetStorage
{
    /// <summary>
    /// Stores a single asset file.
    /// </summary>
    Task<string> StoreAsync(
        string sourceFilePath,
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a directory of assets (e.g., build artifacts).
    /// </summary>
    Task<string> StoreDirectoryAsync(
        string sourceDirectoryPath,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an asset file path.
    /// </summary>
    Task<string?> GetAsync(
        string assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an asset.
    /// </summary>
    Task<bool> DeleteAsync(
        string assetId,
        CancellationToken cancellationToken = default);
}

