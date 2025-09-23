# Unity Development Demo: Natural Language Pipeline

## 🎯 Overview

This demo showcases how to use Nexo's layered natural language pipeline for Unity game development. The system takes natural language requirements and converts them into executable Unity implementations through a series of decomposed, reusable commands.

## 🏗️ Architecture Flow

```
1. Natural Language Input
   ↓
2. Command Decomposition (ITaskExecutionAgent)
   ↓
3. Reusable Command Library
   ↓
4. Tool Execution (Unity Tools)
   ↓
5. Unity Implementation Generated
   ↓
6. Validation & Testing
```

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Unity 2022.3+ (for Unity-specific demos)
- Nexo solution built successfully

### Step 1: Start the Project Manager Demo

```bash
# Run the interactive Project Manager demo
dotnet run --project src/Nexo.Agent.Demo.ProjectManager

# Or run in demo mode (non-interactive)
dotnet run --project src/Nexo.Agent.Demo.ProjectManager -- --demo
```

### Step 2: Natural Language Input

When prompted, provide natural language requirements like:

```
Description: "Create a first-person shooter character controller with movement, jumping, and mouse look controls"

Context: "This is for a Unity game project targeting PC platform with modern FPS mechanics"

Priority: "High - core gameplay feature"

Timeline: "2-3 days for basic implementation"
```

### Step 3: Command Decomposition

The system will automatically decompose your requirements into reusable commands:

```
🔧 Decomposing Requirements into Reusable Commands...

Generated Commands:
1. CreateFPSCharacterController (Movement) - Reused 5 times
2. IntegrateNexoEcosystem (Integration) - Reused 8 times  
3. SetupUnityInputSystem (Input) - Reused 12 times
4. ImplementPhysicsSystem (Physics) - Reused 6 times

Reuse Analysis: 100% commands reused from library
Documentation Cross-References: Unity Documentation, Nexo Documentation
```

### Step 4: Agent Execution

The ITaskExecutionAgent will execute the decomposed commands:

```bash
# The agent will:
1. Plan the execution sequence
2. Identify required tools
3. Execute each command
4. Generate Unity scripts
5. Validate implementation
```

### Step 5: Unity Integration

Generated files will be created in your Unity project:

```
Assets/
├── Scripts/
│   ├── FPSCharacterController.cs
│   ├── NexoIntegration.cs
│   ├── InputSystem.cs
│   └── PhysicsSystem.cs
├── Prefabs/
│   └── FPSPlayer.prefab
└── Scenes/
    └── FPSDemo.unity
```

## 🎮 Unity-Specific Demo Commands

### Using the Unified CLI System

```bash
# Start Unity demo with natural language
dotnet run --project src/Nexo.CLI -- demo feature-lab start --platform unity

# Run Unity validation
dotnet run --project src/Nexo.CLI -- demo validation run

# Showcase Unity features
dotnet run --project src/Nexo.CLI -- demo showcase factory --type game
```

### Using the Consolidated Demo Runner

```bash
# Basic Unity demo
dotnet run --project demo/scripts/DemoRunner -- showcase-game

# Advanced Unity scenario
dotnet run --project demo/scripts/DemoRunner -- --advanced gaming-studio

# Interactive Unity demo
dotnet run --project demo/scripts/DemoRunner -- interactive-demo
```

## 🔧 Advanced Unity Scenarios

### 1. Enterprise Game Development

```bash
dotnet run --project demo/scripts/DemoRunner -- --advanced gaming-studio
```

**Natural Language Input:**
```
"Create a multiplayer game with real-time networking, advanced graphics pipeline, 
game asset management, and performance optimization for mobile and PC platforms"
```

**Generated Commands:**
- CreateMultiplayerSystem
- ImplementGraphicsPipeline  
- SetupAssetManagement
- OptimizePerformance
- IntegrateNexoEcosystem

### 2. Indie Game Development

```bash
dotnet run --project demo/scripts/DemoRunner -- showcase-game
```

**Natural Language Input:**
```
"Build a 2D platformer with character movement, enemy AI, collectibles, 
level progression, and save system"
```

**Generated Commands:**
- Create2DCharacterController
- ImplementEnemyAI
- SetupCollectibleSystem
- CreateLevelProgression
- ImplementSaveSystem

### 3. VR/AR Development

**Natural Language Input:**
```
"Create a VR application with hand tracking, spatial audio, haptic feedback, 
and cross-platform compatibility"
```

**Generated Commands:**
- SetupVRHandTracking
- ImplementSpatialAudio
- CreateHapticFeedback
- EnsureCrossPlatformCompatibility

## 🛠️ Custom Unity Tools

The system includes specialized Unity tools:

### Built-in Unity Tools

1. **PlayerController** - Character movement and controls
2. **Shotgun** - Weapon system with recoil and spread
3. **EnemyImp** - AI enemy with NavMesh navigation
4. **DoorKeySystem** - Interactive door and key mechanics
5. **GameHUD** - UI system using UIToolkit
6. **BlockoutBuilder** - Procedural level generation
7. **NavMeshBake** - Navigation mesh management

### Validation Tools

1. **Playbot** - Input System test harness
2. **UIValidator** - UIToolkit validation and contrast checking
3. **PerfGuard** - Performance monitoring and optimization
4. **NavGuard** - Navigation mesh validation
5. **CodeGate** - Code quality and policy enforcement

## 📊 Command Reuse Analysis

The system tracks command reuse across projects:

```
Command Library Analysis:
- CreateFPSCharacterController: Used in 5 projects
- SetupUnityInputSystem: Used in 12 projects  
- ImplementPhysicsSystem: Used in 6 projects
- IntegrateNexoEcosystem: Used in 8 projects

Reuse Percentage: 85% (High efficiency)
New Commands Generated: 2 (for specific requirements)
```

## 🔍 Documentation Cross-References

Each command is cross-referenced with relevant documentation:

```
CreateFPSCharacterController:
- Unity Documentation: CharacterController class
- Nexo Documentation: Agent integration patterns
- Code Examples: GetComponent<CharacterController>()
- Related Topics: movement, physics, input

SetupUnityInputSystem:
- Unity Documentation: Input System package
- Nexo Documentation: Tool integration
- Code Examples: InputAction, InputActionMap
- Related Topics: input, controls, accessibility
```

## 🎯 Best Practices

### 1. Natural Language Input

**Good:**
```
"Create a third-person character controller with smooth camera following, 
ground detection, and animation state management"
```

**Better:**
```
"Create a third-person character controller for a Unity action RPG with:
- Smooth camera following with collision detection
- Ground detection using raycasting
- Animation state management with blend trees
- Support for different movement speeds (walk, run, sprint)
- Integration with Unity's Input System"
```

### 2. Context Specification

Always provide:
- **Platform**: PC, Mobile, Console, VR
- **Genre**: FPS, RPG, Platformer, Puzzle
- **Complexity**: Simple, Medium, Complex
- **Timeline**: Hours, Days, Weeks

### 3. Validation

After generation, always:
1. Run validation tests
2. Check performance metrics
3. Verify cross-platform compatibility
4. Test with different input devices

## 🚀 Next Steps

1. **Start with the Project Manager Demo** to understand the natural language pipeline
2. **Try different Unity scenarios** using the consolidated demo runner
3. **Customize the command library** for your specific needs
4. **Integrate with your Unity project** using the generated scripts
5. **Extend the system** by adding new tools and commands

## 📚 Related Documentation

- [Agent Foundry Demo](AgentFoundry.md) - Core agent system
- [Unity Agent Foundry](UnityAgentFoundry.md) - Unity-specific implementation
- [Interactive Demo Guide](InteractiveDemoGuide.md) - Project Manager usage
- [Command Decomposition System](../architecture/CommandDecomposition.md) - Technical details
