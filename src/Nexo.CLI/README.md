# Nexo Enhanced CLI Framework

The Nexo Enhanced CLI Framework provides a powerful, intelligent, and user-friendly command-line interface with advanced features including interactive modes, real-time dashboards, intelligent suggestions, and comprehensive help systems.

## 🚀 Key Features

### Interactive CLI Mode
- **Smart Prompts**: Context-aware prompts showing current project, platform, and status
- **Tab Completion**: Intelligent auto-completion for commands and parameters
- **Command History**: Persistent command history with up/down arrow navigation
- **AI-Powered Suggestions**: Contextual command recommendations based on current state

### Real-Time Dashboard
- **Performance Monitoring**: Live CPU, memory, and performance metrics
- **Adaptation Status**: Real-time view of active adaptations and improvements
- **Project Status**: Current project information and health status
- **System Health**: Overall system status with issues and warnings

### Advanced Progress Tracking
- **Nested Operations**: Support for complex multi-step operations
- **Time Estimates**: Intelligent time estimation based on progress
- **Real-Time Updates**: Live progress bars with spinner animations
- **Multi-Step Display**: Comprehensive overview of complex workflows

### Comprehensive Help System
- **Interactive Help**: Searchable documentation with examples
- **Command Browser**: Browse all available commands by category
- **AI-Generated Documentation**: Dynamic documentation generation
- **Practical Examples**: Real-world examples and tutorials

### Persistent State Management
- **User Preferences**: Persistent settings and configurations
- **Command History**: Track and learn from user behavior
- **Project Context**: Remember current project and platform
- **Session Continuity**: Maintain state across CLI sessions

## 📋 Quick Start

### Start Interactive Mode
```bash
nexo interactive start
```

### Open Real-Time Dashboard
```bash
nexo dashboard show
```

### Get Help
```bash
nexo help
```

### Show System Status
```bash
nexo status system
```

## 🎯 Usage Examples

### Interactive Mode Features
```bash
# Start interactive mode with intelligent suggestions
nexo interactive start

# Get contextual help based on current state
nexo interactive context

# Show system status and context
nexo interactive status
```

### Dashboard Features
```bash
# Open real-time monitoring dashboard
nexo dashboard show

# Check dashboard status
nexo dashboard status
```

### Help System Features
```bash
# Show interactive help menu
nexo help

# Search documentation
nexo help search "performance optimization"

# Show examples for a specific category
nexo help examples "project management"

# Browse all commands
nexo help commands
```

### Status and Context
```bash
# Show comprehensive system status
nexo status system

# Show current CLI context
nexo status context

# Show performance metrics
nexo status performance
```

## 🏗️ Architecture

### Core Components

#### Interactive CLI (`IInteractiveCLI`)
- Manages interactive command-line sessions
- Provides smart prompts and command history
- Handles tab completion and user input
- Integrates with suggestion engine and state management

#### Command Suggestion Engine (`ICommandSuggestionEngine`)
- AI-powered contextual suggestions
- Tab completion for commands and parameters
- Learning from user behavior and patterns
- Integration with project context and recent activity

#### Real-Time Dashboard (`IRealTimeDashboard`)
- Live performance monitoring
- Adaptation status visualization
- Project and system health display
- Interactive widget system

#### Progress Tracking (`IProgressTracker`)
- Nested operation support
- Real-time progress updates
- Time estimation and completion tracking
- Multi-step workflow visualization

#### Help System (`IInteractiveHelpSystem`)
- Searchable documentation
- AI-generated command documentation
- Example repository and tutorials
- Interactive command browser

#### State Management (`ICLIStateManager`)
- Persistent user preferences
- Command history tracking
- Project context management
- Session state persistence

### Widget System

The dashboard uses a modular widget system:

- **PerformanceWidget**: CPU, memory, and performance metrics
- **AdaptationWidget**: Real-time adaptation status and improvements
- **ProjectStatusWidget**: Current project information and health
- **SystemHealthWidget**: Overall system status and warnings

## 🔧 Configuration

### State Storage
CLI state is stored in `~/.nexo/cli-state.json` and includes:
- Current project and platform context
- Command history (last 1000 commands)
- User preferences and settings
- Session information

### User Preferences
Common preferences that can be configured:
- Default AI model and provider
- Dashboard refresh rate
- Command history size
- Interactive mode settings
- Theme and display preferences

## 🎨 Interactive Features

### Smart Prompts
The interactive mode provides context-aware prompts:
```
nexo [MyProject] (Cross-Platform) 📊 🔄 ⚠️> 
```

Indicators:
- `[ProjectName]`: Current project
- `(Platform)`: Current platform
- `📊`: Active monitoring
- `🔄`: Pending adaptations
- `⚠️`: Performance issues

### Tab Completion
Intelligent tab completion for:
- Command names and subcommands
- Parameter names and values
- File paths and project names
- Platform and configuration options

### Command History
- Persistent history across sessions
- Up/Down arrow navigation
- Search and filter capabilities
- Success/failure tracking

## 📊 Dashboard Controls

### Navigation
- `Q` or `Esc`: Quit dashboard
- `R`: Refresh dashboard
- `H`: Show help
- `1-4`: Focus on specific widgets

### Widget Interaction
- Click on widgets for detailed information
- Real-time updates every second
- Responsive layout for different terminal sizes

## 🧪 Testing

The enhanced CLI includes comprehensive tests:

```bash
# Run CLI tests
dotnet test src/Nexo.CLI.Tests/

# Test specific components
dotnet test --filter "EnhancedCLITests"
```

## 🔮 Future Enhancements

### Planned Features
- **Voice Commands**: Voice-activated CLI commands
- **Gesture Support**: Touch and gesture support for terminals
- **Plugin System**: Extensible widget and command system
- **Collaborative Mode**: Multi-user interactive sessions
- **Advanced Analytics**: Detailed usage analytics and insights

### Integration Opportunities
- **IDE Integration**: Direct integration with VS Code, Visual Studio, Rider
- **Web Dashboard**: Browser-based dashboard interface
- **Mobile App**: Mobile companion app for monitoring
- **API Gateway**: REST API for CLI functionality

## 📚 Documentation

### Command Reference
- `nexo interactive` - Interactive CLI mode
- `nexo dashboard` - Real-time monitoring dashboard
- `nexo help` - Comprehensive help system
- `nexo status` - System status and context

### Examples and Tutorials
- Getting started guide
- Project management workflows
- Performance optimization techniques
- Unity game development integration
- Real-time adaptation strategies

## 🤝 Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8.0 SDK
3. Run `dotnet restore`
4. Build with `dotnet build`
5. Test with `dotnet test`

### Adding New Features
1. Create interfaces in appropriate namespace
2. Implement services with dependency injection
3. Add comprehensive tests
4. Update documentation
5. Submit pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Built on System.CommandLine framework
- AI integration with multiple providers
- Real-time monitoring and adaptation systems
- Comprehensive testing and documentation

---

**The Nexo Enhanced CLI Framework transforms the command-line experience into an intelligent, interactive, and powerful development tool that adapts to your workflow and provides real-time insights into your development environment.**
