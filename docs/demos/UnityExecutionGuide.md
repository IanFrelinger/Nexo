# Unity Natural Language Pipeline: Complete Execution Guide

## 🎯 Quick Start (5 Minutes)

### Option 1: Automated Demo Script
```bash
# Run the complete demo automatically
./scripts/unity-natural-language-demo.sh
```

### Option 2: Interactive Project Manager
```bash
# Start the interactive demo
dotnet run --project src/Nexo.Agent.Demo.ProjectManager
```

### Option 3: CLI Commands
```bash
# Create Unity components with natural language
dotnet run --project src/Nexo.CLI -- demo unity create "FPS character controller with movement and mouse look" --platform pc --complexity medium

# Validate Unity project
dotnet run --project src/Nexo.CLI -- demo unity validate
```

## 🏗️ Complete Workflow Execution

### Step 1: Natural Language Input

**Example Input:**
```
Description: "Create a first-person shooter character controller with movement, jumping, and mouse look controls"

Context: "This is for a Unity game project targeting PC platform with modern FPS mechanics"

Priority: "High - core gameplay feature"

Timeline: "2-3 days for basic implementation"
```

### Step 2: Command Decomposition

The system automatically decomposes your input into reusable commands:

```
🔧 Decomposing Requirements into Reusable Commands...

Generated Commands:
1. CreateFPSCharacterController (Movement) - Reused 5 times
   - Previous projects: "FPS Game Project", "Unity Demo", "Character Controller Test"
   - Similarity score: 0.85
   - Similar commands: CreateThirdPersonController, CreateTopDownController

2. IntegrateNexoEcosystem (Integration) - Reused 8 times
   - Previous projects: "Enterprise App", "Mobile Game", "Web Platform"
   - Similarity score: 0.90
   - Similar commands: IntegrateWithPipeline, SetupAgentSystem

3. SetupUnityInputSystem (Input) - Reused 12 times
   - Previous projects: "Action RPG", "Platformer", "Racing Game"
   - Similarity score: 0.92
   - Similar commands: SetupInputActions, ConfigureControls

4. ImplementPhysicsSystem (Physics) - Reused 6 times
   - Previous projects: "Physics Demo", "Simulation Game", "VR App"
   - Similarity score: 0.78
   - Similar commands: SetupRigidbody, ConfigureColliders

Reuse Analysis:
- Total Commands: 4
- Reused Commands: 4
- New Commands: 0
- Reuse Percentage: 100%
```

### Step 3: Documentation Cross-References

Each command is cross-referenced with relevant documentation:

```
Documentation Cross-References:
==============================

CreateFPSCharacterController:
- Unity Documentation: https://docs.unity3d.com/Manual/class-CharacterController.html
- Section: Character Controller
- Related Topics: character, controller, unity, movement
- Code Examples: GetComponent<CharacterController>(), Move(), SimpleMove()

IntegrateNexoEcosystem:
- Nexo Documentation: https://nexo.dev/docs/agent/integration
- Section: Agent Integration
- Related Topics: nexo, agent, integration, ecosystem
- Code Examples: public class MyTool : INexoTool, services.AddNexoAgent()

SetupUnityInputSystem:
- Unity Documentation: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/
- Section: Input System Package
- Related Topics: input, controls, keyboard, mouse, gamepad
- Code Examples: InputAction, InputActionMap, PlayerInput

ImplementPhysicsSystem:
- Unity Documentation: https://docs.unity3d.com/Manual/PhysicsOverview.html
- Section: Physics Overview
- Related Topics: physics, collision, rigidbody, ground detection
- Code Examples: Physics.Raycast(), OnCollisionEnter(), AddForce()
```

### Step 4: Agent Execution

The ITaskExecutionAgent executes the decomposed commands:

```
🚀 Executing Command Sequence: FPSCharacterControllerImplementation

Executing Command 1/4: CreateFPSCharacterController
- Creating FPSCharacterController.cs
- Setting up CharacterController component
- Implementing movement logic
- Adding mouse look functionality
✅ Command completed in 2.3s

Executing Command 2/4: IntegrateNexoEcosystem
- Creating NexoIntegration.cs
- Setting up agent communication
- Configuring tool registry
- Adding observability
✅ Command completed in 1.8s

Executing Command 3/4: SetupUnityInputSystem
- Creating InputSystem.cs
- Configuring input actions
- Setting up key bindings
- Adding gamepad support
✅ Command completed in 1.5s

Executing Command 4/4: ImplementPhysicsSystem
- Creating PhysicsSystem.cs
- Setting up ground detection
- Configuring collision layers
- Adding slope handling
✅ Command completed in 2.1s

Total execution time: 7.7s
```

### Step 5: Unity Implementation Generated

Files are created in your Unity project:

```
Assets/
├── Scripts/
│   ├── FPSCharacterController.cs
│   │   ├── Movement logic with WASD controls
│   │   ├── Mouse look with sensitivity settings
│   │   ├── Jump functionality with ground detection
│   │   └── Slope handling and physics integration
│   │
│   ├── NexoIntegration.cs
│   │   ├── Agent communication setup
│   │   ├── Tool registry integration
│   │   ├── Observability configuration
│   │   └── Error handling and logging
│   │
│   ├── InputSystem.cs
│   │   ├── Input action definitions
│   │   ├── Key binding configuration
│   │   ├── Gamepad support
│   │   └── Input validation
│   │
│   └── PhysicsSystem.cs
│       ├── Ground detection with raycasting
│       ├── Collision layer management
│       ├── Slope angle calculations
│       └── Physics material configuration
│
├── Prefabs/
│   └── FPSPlayer.prefab
│       ├── CharacterController component
│       ├── Camera setup
│       ├── Input system integration
│       └── Physics configuration
│
├── Scenes/
│   └── FPSDemo.unity
│       ├── FPSPlayer prefab instance
│       ├── Ground plane
│       ├── Lighting setup
│       └── Camera configuration
│
└── Input/
    └── FPSControls.inputactions
        ├── Movement actions (WASD)
        ├── Look actions (Mouse)
        ├── Jump action (Space)
        └── Gamepad mappings
```

### Step 6: Validation and Testing

The system runs comprehensive validation:

```
🔍 Running Validation Tests...

Code Quality Validation:
✅ Syntax check: PASSED
✅ Style guidelines: PASSED
✅ Documentation: PASSED
✅ Null safety: PASSED

Performance Validation:
✅ Frame rate: 60+ FPS
✅ Memory usage: < 50MB
✅ CPU usage: < 10%
✅ GPU usage: < 20%

Cross-Platform Validation:
✅ PC (Windows): PASSED
✅ PC (Mac): PASSED
✅ PC (Linux): PASSED
✅ Mobile (Android): PASSED
✅ Mobile (iOS): PASSED

Input System Validation:
✅ Keyboard: PASSED
✅ Mouse: PASSED
✅ Gamepad: PASSED
✅ Touch: PASSED

Physics Validation:
✅ Collision detection: PASSED
✅ Ground detection: PASSED
✅ Slope handling: PASSED
✅ Jump mechanics: PASSED

Integration Validation:
✅ Nexo ecosystem: PASSED
✅ Agent communication: PASSED
✅ Tool registry: PASSED
✅ Observability: PASSED

All validations passed! ✅
```

## 🎮 Unity-Specific Demo Commands

### Basic Unity Demos

```bash
# FPS Character Controller
dotnet run --project src/Nexo.CLI -- demo unity create "FPS character controller with movement and mouse look" --platform pc

# Third-Person Controller
dotnet run --project src/Nexo.CLI -- demo unity create "third-person character controller with camera following" --platform pc

# 2D Platformer
dotnet run --project src/Nexo.CLI -- demo unity create "2D platformer character with jumping and wall sliding" --platform mobile

# VR Hand Tracking
dotnet run --project src/Nexo.CLI -- demo unity create "VR hand tracking with haptic feedback" --platform vr
```

### Advanced Unity Scenarios

```bash
# Enterprise Game Development
dotnet run --project demo/scripts/DemoRunner -- --advanced gaming-studio

# Indie Game Development
dotnet run --project demo/scripts/DemoRunner -- showcase-game

# Mobile Game Development
dotnet run --project demo/scripts/DemoRunner -- --advanced startup-mobile
```

### Interactive Demos

```bash
# Project Manager Demo (Interactive)
dotnet run --project src/Nexo.Agent.Demo.ProjectManager

# Feature Lab Demo
dotnet run --project demo/scripts/DemoRunner -- interactive-demo

# Full Showcase
dotnet run --project demo/scripts/DemoRunner
```

## 🔧 Customization and Extension

### Adding New Unity Tools

1. **Create Tool Interface:**
```csharp
public interface IUnityTool : ITool<UnityToolRequest, UnityToolResult>
{
    string ToolName { get; }
    string[] SupportedComponents { get; }
}
```

2. **Implement Tool:**
```csharp
public class CustomUnityTool : IUnityTool
{
    public string ToolName => "CustomUnityTool";
    public string[] SupportedComponents => new[] { "CustomComponent" };
    
    public async Task<UnityToolResult> ExecuteAsync(UnityToolRequest request, ToolContext context)
    {
        // Implementation
    }
}
```

3. **Register Tool:**
```csharp
services.AddSingleton<IUnityTool, CustomUnityTool>();
```

### Extending Command Library

1. **Add New Command:**
```csharp
new Command
{
    Id = "cmd-005",
    Name = "CreateCustomComponent",
    Description = "Creates a custom Unity component with specific functionality",
    Category = "Custom",
    Tags = new[] { "custom", "component", "unity" },
    ReuseCount = 0,
    LastUsed = DateTime.UtcNow
}
```

2. **Update Documentation Cross-References:**
```csharp
new DocumentationCrossReference
{
    CommandId = "cmd-005",
    DocumentationSource = "Custom Documentation",
    DocumentationUrl = "https://custom-docs.com/component",
    Section = "Custom Components",
    RelatedTopics = new[] { "custom", "component", "unity" },
    CodeExamples = new[] { "GetComponent<CustomComponent>()" }
}
```

## 📊 Performance Metrics

### Command Execution Times

```
Command Performance Analysis:
============================

CreateFPSCharacterController: 2.3s (Average: 2.1s)
IntegrateNexoEcosystem: 1.8s (Average: 1.9s)
SetupUnityInputSystem: 1.5s (Average: 1.6s)
ImplementPhysicsSystem: 2.1s (Average: 2.0s)

Total Pipeline Time: 7.7s
Average Pipeline Time: 7.6s
Performance: EXCELLENT ✅
```

### Reuse Efficiency

```
Reuse Efficiency Analysis:
=========================

Commands Reused: 4/4 (100%)
New Commands Generated: 0
Library Growth: 0%
Efficiency: MAXIMUM ✅

Similarity Scores:
- CreateFPSCharacterController: 0.85 (High)
- IntegrateNexoEcosystem: 0.90 (Very High)
- SetupUnityInputSystem: 0.92 (Very High)
- ImplementPhysicsSystem: 0.78 (Good)

Average Similarity: 0.86 (Very High) ✅
```

## 🚀 Next Steps

1. **Run the Demo:**
   ```bash
   ./scripts/unity-natural-language-demo.sh
   ```

2. **Try Different Scenarios:**
   - FPS games
   - Platformers
   - VR applications
   - Mobile games

3. **Customize the System:**
   - Add new Unity tools
   - Extend command library
   - Create custom validation rules

4. **Integrate with Your Project:**
   - Import generated scripts
   - Configure Unity settings
   - Test in your environment

5. **Extend and Scale:**
   - Add more complex scenarios
   - Integrate with CI/CD
   - Deploy to production

## 📚 Additional Resources

- [Unity Natural Language Demo](UnityNaturalLanguageDemo.md) - Complete demo documentation
- [Agent Foundry Demo](AgentFoundry.md) - Core agent system
- [Unity Agent Foundry](UnityAgentFoundry.md) - Unity-specific implementation
- [Interactive Demo Guide](InteractiveDemoGuide.md) - Project Manager usage
- [Command Decomposition System](../architecture/CommandDecomposition.md) - Technical details
