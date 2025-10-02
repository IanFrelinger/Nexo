namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents an asset requirement for the game slice.
    /// </summary>
    public sealed record AssetRequirement(
        string AssetType,
        string Name,
        string Description,
        bool IsRequired = true,
        int Priority = 3);
}
