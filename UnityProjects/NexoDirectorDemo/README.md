# Nexo Director Demo - Unity Project

This Unity project demonstrates the integration of **Director Studio** with **Nexo validation** in a real Unity Editor environment.

## 🎯 Project Overview

This demo project showcases:
- **Director Studio Integration**: Cross-platform desktop UI for Unity Editor control
- **Nexo Validation**: Architecture and code validation through Director Studio
- **Real-time Feedback**: Live logs, gate results, and status updates
- **Unity Editor Controls**: Play mode, scene management, and project information

## 🚀 Quick Start

### Prerequisites
- Unity 2022.3.15f1 or later
- .NET 8.0 (for Director Studio)
- Director Studio application

### Setup Steps

1. **Open Unity Project**
   ```bash
   # Open Unity and load this project
   # The project is located at: UnityProjects/NexoDirectorDemo/
   ```

2. **Start Director Studio**
   ```bash
   # From the main Nexo project directory
   dotnet run --project tools/Director.Avalonia/Director.Avalonia.csproj
   ```

3. **Connect Director Studio to Unity**
   - Open the Director Studio window in Unity: `Window > Director Studio > Director Studio Control`
   - Copy the generated token from the Unity window
   - Paste the token in the Director Studio application
   - Click "Connect"

4. **Test the Integration**
   - Use the Director Studio UI to run Nexo commands
   - Watch real-time feedback in both applications
   - Test Unity Editor controls (play mode, scene management)

## 📁 Project Structure

```
UnityProjects/NexoDirectorDemo/
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity          # Main demo scene
│   ├── Scripts/
│   │   ├── NexoValidationDemo.cs      # Demo validation patterns
│   │   └── DirectorStudioController.cs # Director Studio integration
│   └── Editor/
│       └── DirectorStudioWindow.cs    # Custom Unity Editor window
├── Packages/
│   └── com.nexo.director/             # Director Studio UPM package
└── ProjectSettings/
    └── ProjectVersion.txt             # Unity version info
```

## 🔧 Features Demonstrated

### Director Studio Integration
- **Cross-platform UI**: Modern desktop interface
- **Real-time Communication**: TCP-based IPC with Unity Editor
- **Token Authentication**: Secure connection establishment
- **Command Execution**: Run Nexo commands from desktop UI

### Nexo Validation
- **Architecture Validation**: Check architectural patterns
- **Code Analysis**: Analyze code quality and violations
- **Agent Operations**: List and run available agents
- **Performance Monitoring**: Track performance metrics

### Unity Editor Controls
- **Play Mode Control**: Toggle play mode from Director Studio
- **Scene Management**: List and open scenes
- **Project Information**: Get project details and status
- **Real-time Feedback**: Live logs and gate results

## 🎮 Demo Scripts

### NexoValidationDemo.cs
Demonstrates various architectural patterns that Nexo can validate:
- Singleton pattern usage
- Dependency injection
- Event system architecture
- Performance optimization
- Async/await patterns
- Error handling
- Resource management

### DirectorStudioController.cs
Shows how to interact with the Director Studio system:
- Connection management
- Command execution
- Real-time status updates
- Unity Editor control simulation

### DirectorStudioWindow.cs
Custom Unity Editor window that provides:
- Director Studio connection interface
- Nexo command execution
- Log message display
- Gate result visualization

## 🔌 Director Studio Commands

The demo supports these Director Studio commands:

### Nexo Commands
- `validate --filter Category=Architecture` - Run architecture validation
- `analyze --format-json` - Run code analysis
- `agent --name list` - List available agents
- `agent --name analyze` - Run agent analysis

### Unity Commands
- `TogglePlay` - Toggle Unity play mode
- `GetProjectInfo` - Get project information
- `ListScenes` - List available scenes
- `OpenScene` - Open a specific scene

## 📊 Real-time Feedback

Director Studio provides real-time feedback through:

### Log Messages
- Command execution status
- Validation results
- Error messages
- System notifications

### Gate Results
- Architecture validation results
- Code analysis findings
- Performance metrics
- Quality gate status

### Status Updates
- Connection status
- Play mode state
- Project information
- Scene information

## 🛠️ Development

### Adding New Commands
1. Add command type to `Director.Core.Protocol.CommandTypes`
2. Create payload class in `Director.Core.Protocol.Contracts`
3. Implement handler in Unity Editor scripts
4. Add UI controls in Director Studio

### Extending Validation
1. Add validation rules to `NexoValidationDemo.cs`
2. Implement validation logic
3. Add corresponding Nexo commands
4. Update Director Studio UI

### Custom UI Integration
1. Modify `DirectorStudioWindow.cs` for Unity Editor UI
2. Update `MainWindow.axaml` for Director Studio UI
3. Add new ViewModels for complex interactions

## 🐛 Troubleshooting

### Common Issues

**Director Studio won't connect to Unity**
- Ensure Unity Editor is running
- Check that the Director Studio package is imported
- Verify the token is correct
- Check firewall settings for port 5088

**Nexo commands not working**
- Ensure Nexo CLI is built and available
- Check command syntax and parameters
- Verify working directory is correct
- Check Unity console for error messages

**UI not updating**
- Check connection status
- Verify event handling is working
- Check for threading issues
- Restart both applications

### Debug Mode
Enable debug logging by setting `enableDirectorStudio = true` in the demo scripts.

## 📚 Further Reading

- [Director Studio Architecture](../docs/ARCHITECTURE_DIAGRAM.md)
- [Nexo Validation Guide](../docs/VALIDATION.md)
- [Unity Integration Guide](../Packages/com.nexo.director/README.md)
- [Director Studio Usage](../tools/Director.Avalonia/README.md)

## 🤝 Contributing

This demo project is part of the larger Nexo framework. To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with this Unity project
5. Submit a pull request

## 📄 License

This project is part of the Nexo framework and is licensed under the MIT License.
