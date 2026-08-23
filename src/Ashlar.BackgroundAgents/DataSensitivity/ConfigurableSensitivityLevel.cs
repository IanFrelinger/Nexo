namespace Ashlar.BackgroundAgents.DataSensitivity;

/// <summary>
/// Configurable sensitivity level implementation.
/// 
/// Created from CustomSensitivityLevel configuration.
/// </summary>
public sealed record ConfigurableSensitivityLevel(
    string Value,
    string Display,
    int SensitivityValue,
    bool AllowsExternalLLM,
    bool AllowsWebSearch,
    bool RequiresLocalOnly,
    bool AllowsNetworkExports,
    string Description) : IDataSensitivityLevel;
