namespace Ashlar.BackgroundAgents.DataSensitivity;

/// <summary>
/// Primitive sensitivity level implementation.
/// 
/// Immutable record type for primitive sensitivity levels provided by the framework.
/// </summary>
internal sealed record PrimitiveSensitivityLevel(
    string Value,
    string Display,
    int SensitivityValue,
    bool AllowsExternalLLM,
    bool AllowsWebSearch,
    bool RequiresLocalOnly,
    bool AllowsNetworkExports,
    string Description) : IDataSensitivityLevel;
