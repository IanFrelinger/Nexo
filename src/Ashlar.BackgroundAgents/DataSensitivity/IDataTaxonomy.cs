namespace Ashlar.BackgroundAgents.DataSensitivity;

/// <summary>
/// Maps observed data types to default IDataSensitivityLevel names.
/// Used by the sanitization layer to classify unstructured context.
/// </summary>
public interface IDataTaxonomy
{
    /// <summary>
    /// Gets the default sensitivity level name for a data type.
    /// </summary>
    /// <param name="dataType">The data type (e.g. file-paths, editor-events, terminal-output).</param>
    /// <returns>The level name (e.g. TopSecret, Public), or null if not found. Unknown types default to TopSecret.</returns>
    string? GetDefaultLevelForDataType(string dataType);
}
