# Nexo Plugin Host

A hardened plugin hosting system that provides secure, unloadable plugin management with explicit capability validation.

## Features

- **Collectible AssemblyLoadContext**: Plugins are loaded in isolated, unloadable contexts
- **Capability-based Security**: Only plugins implementing declared capabilities are allowed
- **Comprehensive Auditing**: All plugin lifecycle events are logged and audited
- **Dependency Isolation**: Plugin dependencies are resolved from controlled folders
- **Type Safety**: Strong typing for plugin capabilities and interfaces

## Capability Interfaces

The system defines four core capability interfaces:

- `ISense`: For plugins that can sense or observe the environment
- `IDecide`: For plugins that can make decisions based on input
- `IAct`: For plugins that can perform actions
- `IGuard`: For plugins that can guard or validate operations

## Plugin Manifest Format

Plugins must include a `plugin.json` manifest file:

```json
{
  "Name": "MyPlugin",
  "Version": "1.0.0",
  "Description": "A sample plugin",
  "Author": "Plugin Author",
  "MinimalNexoVersion": "1.0.0",
  "Capabilities": ["ISense", "IAct"]
}
```

## Usage Example

```csharp
using var pluginHost = new PluginHost(logger);

// Load a plugin
var success = await pluginHost.LoadPluginAsync("path/to/plugin.dll");

if (success)
{
    // Use plugin capabilities
    var senseInstances = pluginHost.GetCapabilityInstances<ISense>();
    foreach (var sense in senseInstances)
    {
        var result = await sense.SenseAsync("input data");
    }

    // Unload when done
    await pluginHost.UnloadPluginAsync("MyPlugin");
}
```

## Security Features

1. **Capability Validation**: Plugins must implement exactly the capabilities they declare
2. **Assembly Isolation**: Each plugin runs in its own AssemblyLoadContext
3. **Dependency Control**: Dependencies are resolved from controlled folders only
4. **Audit Logging**: All operations are logged for security auditing
5. **Safe Unloading**: Plugins can be completely unloaded and garbage collected

## Testing

The system includes comprehensive tests covering:
- Plugin loading and unloading
- Capability validation
- AssemblyLoadContext collection
- Error handling and logging
- Lifecycle auditing

Run tests with:
```bash
dotnet test tests/Nexo.PluginHost.Tests/
```
