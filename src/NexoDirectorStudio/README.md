# Nexo Director Studio

A Unity editor tool that enables non-programmers to create game slices from natural-language briefs across any genre (FPS, Platformer, RPG, simulation, etc.).

## Overview

Director Studio is built on top of Nexo's agent-first orchestration system, providing:

- **Genre-Agnostic Design**: Create content for any game genre
- **Natural Language Input**: Describe your game slice in plain English
- **Deterministic Output**: Consistent results with seed-based generation
- **Comprehensive Validation**: Playability, mechanics, pacing, performance, and accessibility checks
- **Offline AI Integration**: Works with local AI models (Ollama, ComfyUI, Piper)
- **Safe Asset Generation**: All content written to `Assets/Generated/**` only

## Quick Start

1. Open Unity Editor
2. Navigate to **Nexo ▸ Director Studio** in the menu
3. Enter your game brief in natural language
4. Select or auto-detect the genre
5. Click **Plan Game Slice** to generate your content
6. Review the validation report
7. Nexo agents will generate all game components as part of the pipeline

## Features

### Genre Profiles
- **FPS**: First-person shooter mechanics and validation
- **Platformer**: Jump mechanics and level design rules
- **RPG**: Quest systems and character progression
- **Extensible**: Easy to add new genres

### Validation Suite
- **Playability**: Ensures path to completion exists
- **Mechanics**: Validates genre-specific affordances
- **Pacing**: Checks interaction density and breathing room
- **Performance**: Enforces triangle/draw call budgets
- **Accessibility**: Validates contrast, text size, motion settings
- **Safety**: Prevents writes outside allowed paths

### Auto-Fix Workflow
- Proposes fixes for validation failures
- Shows diff preview before applying changes
- Requires manual approval for all changes
- Logs all modifications for audit trail

## Development

See [DirectorStudio_DevPlan.md](../../docs/DirectorStudio_DevPlan.md) for comprehensive development documentation, architecture details, and implementation phases.

## Architecture

Director Studio is built as a Unity package that references Nexo's public APIs without modifying the core Nexo libraries. The architecture follows:

- **Agent-First Orchestration**: Uses Nexo's command system
- **Offline Adapters**: Local AI model integration
- **Staging→Promote**: Safe asset generation workflow
- **Validation Gates**: Comprehensive quality checks
- **Genre Profiles**: Pluggable genre-specific rules

## Safety & Constraints

- All generated assets written to `Assets/Generated/**` only
- Size caps enforced to prevent excessive resource usage
- Path allowlist prevents accidental file system writes
- Deterministic generation with audit logging
- No modifications to existing Nexo assemblies

## Testing

The package includes comprehensive test coverage:

- **Unit Tests**: DTO serialization, validator logic, profile detection
- **Integration Tests**: Staging/promote workflow, content references
- **Smoke Tests**: Window responsiveness, play mode transitions
- **Cross-Platform**: Windows, macOS, Linux compatibility

## Contributing

This package is part of the Nexo ecosystem. See the main [CONTRIBUTING.md](../../CONTRIBUTING.md) for contribution guidelines.

## License

See the main [LICENSE](../../LICENSE) file for license information.
