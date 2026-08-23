namespace Ashlar.BackgroundAgents.DataSensitivity;

/// <summary>
/// Factory for creating and registering custom sensitivity levels from configuration.
/// 
/// Converts CustomSensitivityLevel configuration objects into IDataSensitivityLevel instances
/// and registers them with the DataSensitivityRegistry.
/// </summary>
public sealed class CustomSensitivityLevelFactory
{
    private readonly IDataSensitivityRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomSensitivityLevelFactory"/> class.
    /// </summary>
    /// <param name="registry">The sensitivity level registry.</param>
    public CustomSensitivityLevelFactory(IDataSensitivityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Register custom sensitivity levels from configuration dictionary.
    /// </summary>
    /// <param name="config">Dictionary mapping level names to configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public void RegisterFromConfig(Dictionary<string, CustomSensitivityLevel>? config)
    {
        if (config == null || config.Count == 0)
            return;

        foreach (var (name, configLevel) in config)
        {
            // Validate configuration
            if (string.IsNullOrWhiteSpace(configLevel.Name))
            {
                throw new ArgumentException($"Custom sensitivity level '{name}' has no name", nameof(config));
            }

            if (configLevel.Name != name)
            {
                throw new ArgumentException($"Custom sensitivity level name '{configLevel.Name}' does not match key '{name}'", nameof(config));
            }

            // Create level instance
            var level = new ConfigurableSensitivityLevel(
                configLevel.Name,
                configLevel.Name, // Display same as value
                configLevel.SensitivityValue,
                configLevel.AllowsExternalLLM,
                configLevel.AllowsWebSearch,
                configLevel.RequiresLocalOnly,
                configLevel.AllowsNetworkExports,
                configLevel.Description);

            // Register with registry
            _registry.Register(level);
        }
    }
}
