# Director Studio (Avalonia) - Nexo Companion

A cross-platform desktop application that provides a rich UI for controlling and augmenting Unity Editor through the Nexo framework via a lightweight IPC bridge.

## Features

- **Real-time Unity Control**: Control Unity Editor from a modern desktop interface
- **Nexo Integration**: Run Nexo CLI commands (analyze, validate, agent) with live output
- **Dynamic UI Injection**: Modify Unity Editor UI through schema-based modifications
- **Live Logging**: Stream Unity and Nexo logs in real-time
- **Gate Validation**: Monitor and display validation results
- **Cross-platform**: Runs on Windows, macOS, and Linux

## Architecture

```
Director.Avalonia (Desktop UI)
    ↓ TCP/IP (localhost:5088)
Director.Core (Shared Contracts)
    ↑
com.nexo.director (Unity UPM Package)
    ↓
Unity Editor (TCP Server + UI Renderer)
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Unity Editor 2022.3+ (with Director IPC Bridge package)
- Nexo CLI installed

### Installation

1. **Build the solution:**
   ```bash
   cd tools/Director.Avalonia
   dotnet build
   ```

2. **Start Unity Editor** (this automatically starts the IPC server)

3. **Run Director Studio:**
   ```bash
   dotnet run --project tools/Director.Avalonia
   ```

4. **Connect to Unity:**
   - The app will auto-discover the Unity token
   - Click "Connect" to establish the connection
   - Start using Nexo commands!

## Usage

### Connection

- **Auto-Discover Token**: Automatically finds the Unity token
- **Manual Token**: Copy token from Unity Editor window
- **Connection Status**: Real-time connection status indicator

### Nexo Commands

- **Run Validation**: Execute `nexo validate` with live output
- **Run Analysis**: Execute `nexo analyze` with live output  
- **List Agents**: Execute `nexo agent --list` to see available agents

### Unity Control

- **Toggle Play Mode**: Start/stop Unity play mode
- **Get Project Info**: Retrieve Unity project details
- **List Scenes**: Get all scenes in the project
- **Open Scenes**: Open specific Unity scenes

### UI Modifications

- **Target Slot**: Specify which Unity UI slot to modify
- **Apply UI Mod**: Inject custom UI elements into Unity Editor
- **Schema-based**: Use JSON schema to define UI modifications

## Configuration

### Unity Setup

1. Install the `com.nexo.director` UPM package
2. Open **Window → Director Studio → IPC Bridge**
3. Copy the authentication token
4. The TCP server starts automatically on port 5088

### Avalonia Configuration

The app automatically discovers the Unity token from common locations:
- `%LOCALAPPDATA%/Unity/cache/Director.token` (Windows)
- `~/Library/Application Support/Unity/cache/Director.token` (macOS)
- `~/.config/unity/cache/Director.token` (Linux)

## Development

### Project Structure

```
tools/Director.Avalonia/
├── ViewModels/          # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── ConnectionViewModel.cs
│   ├── LogsViewModel.cs
│   ├── GatesViewModel.cs
│   └── ValidationViewModel.cs
├── Services/            # Business logic services
│   ├── DirectorClient.cs
│   ├── TokenService.cs
│   ├── DiffService.cs
│   └── NexoCommandService.cs
├── Assets/              # UI assets and styles
└── Tests/               # Unit tests
```

### Adding New Commands

1. **Add to Director.Core**: Define new command types and payloads
2. **Update Unity Server**: Handle the new command in `DirectorServer.cs`
3. **Add to Avalonia**: Create UI and ViewModel for the new command

### Custom UI Modifications

Use the UI schema system to inject custom controls:

```json
{
  "targetSlot": "quality.filters",
  "uiSchema": {
    "elements": [
      {
        "type": "toolbarMenu",
        "text": "Status: All",
        "items": ["All", "Errors", "Warnings"]
      },
      {
        "type": "toolbarToggle", 
        "text": "Show Performance",
        "value": true
      }
    ]
  }
}
```

## Troubleshooting

### Connection Issues

- **"Connection failed"**: Ensure Unity Editor is running and the IPC server started
- **"No token found"**: Check Unity Editor window for the token
- **"Authentication failed"**: Verify the token is correct and not expired

### Command Issues

- **"Command not found"**: Ensure Nexo CLI is installed and in PATH
- **"Permission denied"**: Check Unity project permissions
- **"Timeout"**: Commands may take time - check the logs panel

### UI Issues

- **UI not updating**: Check Unity Editor console for errors
- **Schema errors**: Validate JSON schema format
- **Slot not found**: Ensure target slot exists in Unity

## Security

- **Localhost Only**: Communication is restricted to localhost (127.0.0.1)
- **Token Authentication**: Ephemeral tokens prevent unauthorized access
- **No External Network**: All communication is local machine only

## License

MIT License - see LICENSE file for details.
