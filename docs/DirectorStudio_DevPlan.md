# Director Studio Development Plan
## Agent-First, Genre-Agnostic Unity Editor Tool

### Overview
Director Studio is a Unity editor tool that enables non-programmers to create game slices from natural-language briefs across ANY genre (FPS, Platformer, RPG, simulation, etc.). Built on top of Nexo's agent-first orchestration system with offline adapters and strict validation pipelines.

### Scope & Non-Goals

#### In Scope
- Unity Editor window: "Nexo ▸ Director Studio"
- Genre-agnostic orchestration using Nexo's command system
- Offline AI adapters (Ollama, ComfyUI, Piper) with health checks
- Deterministic pipelines with staging→promote workflow
- Comprehensive validation suite (playability, mechanics, pacing, performance, accessibility)
- Genre profile system with auto-detection
- Director UX with fix workflow and approval gates
- Heavy smoke testing (EditMode + PlayMode)
- CI/CD with coverage requirements (≥80%)

#### Out of Scope
- Modifications to existing Nexo core libraries
- Changes to Nexo solution files, props, or analyzers
- Real-time AI model integration (offline adapters only)
- Multi-user collaboration features
- Version control integration
- Asset store distribution

### Branch & Development Style
- **Branch**: `feature/director-studio-genre-agnostic`
- **Style**: Agent-first + TDD with small commits
- **Conventions**: `test: ...` / `feat: ...` / `refactor: ...` / `chore: ...`

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Director Studio                          │
├─────────────────────────────────────────────────────────────┤
│  Editor Window (DirectorStudioWindow)                      │
│  ├─ Notes Panel                                            │
│  ├─ Genre Picker                                           │
│  ├─ Scorecard Tiles                                        │
│  └─ Beats Timeline                                         │
├─────────────────────────────────────────────────────────────┤
│  Orchestration Layer                                       │
│  ├─ GenericCommandOrchestrator (from Nexo)                 │
│  ├─ PlanGameSliceCommand                                   │
│  ├─ BuildWorldLayoutCommand                                │
│  ├─ PlaceInteractionsCommand                               │
│  └─ CreateContentBundleCommand                             │
├─────────────────────────────────────────────────────────────┤
│  Validation Suite                                          │
│  ├─ PlayabilityValidator                                   │
│  ├─ MechanicsValidator                                      │
│  ├─ PacingValidator                                         │
│  ├─ PerformanceValidator                                    │
│  ├─ AssetQualityValidator                                   │
│  ├─ AccessibilityValidator                                  │
│  └─ SafetyValidator                                         │
├─────────────────────────────────────────────────────────────┤
│  Genre Profiles                                            │
│  ├─ IGenreProfile                                          │
│  ├─ GenreRegistry                                          │
│  ├─ FPSProfile                                             │
│  ├─ PlatformerProfile                                       │
│  └─ RPGProfile                                              │
├─────────────────────────────────────────────────────────────┤
│  Offline Adapters                                          │
│  ├─ IOllamaAdapter (LLM)                                   │
│  ├─ ITextureGenAdapter (ComfyUI)                          │
│  └─ ITtsAdapter (Piper)                                    │
├─────────────────────────────────────────────────────────────┤
│  File System & Policies                                    │
│  ├─ FileTransaction (staging→promote)                      │
│  ├─ PathAllowlist (Assets/Generated/** only)              │
│  ├─ WriteBudget (size caps)                                │
│  └─ AuditWriter (deterministic logging)                    │
└─────────────────────────────────────────────────────────────┘
```

### Dependencies
- **Nexo References**: Abstractions, Core.Application, Core.Domain, Tools.*
- **Unity**: 2022.3 LTS or later
- **Constraints**: No modifications to Nexo core assemblies

### Project Layout
```
Assets/NexoDirectorStudio/
├── Editor/                    # Editor-only code
├── Runtime/                   # Runtime code
│   ├── Orchestration/        # Command implementations
│   ├── Commands/             # Command definitions
│   ├── Validators/           # Validation logic
│   ├── Policies/             # Policy enforcement
│   ├── Adapters/             # Offline AI adapters
│   ├── DTO/                  # Data transfer objects
│   └── Profiles/             # Genre profiles
├── Tests/
│   ├── EditMode/             # Editor-time tests
│   └── PlayMode/             # Runtime tests
└── Generated/                # Write-only target for promoted artifacts
```

### Assembly Definitions
- **NexoDirectorStudio.Runtime**: Runtime code only
- **NexoDirectorStudio.Editor**: Editor code with UNITY_EDITOR define

### Risk Assessment & Mitigations

#### High Risk
1. **Nexo Integration Complexity**
   - *Risk*: Nexo's command system may not fit Unity's lifecycle
   - *Mitigation*: Create thin adapters, keep Unity API calls on main thread

2. **Deterministic Pipeline**
   - *Risk*: Non-deterministic AI outputs breaking reproducibility
   - *Mitigation*: Seed-based generation, audit logging, validation gates

3. **Performance Budgets**
   - *Risk*: Generated content exceeds Unity performance limits
   - *Mitigation*: Profile-driven budgets, validation gates, performance tests

#### Medium Risk
1. **File System Safety**
   - *Risk*: Accidental writes outside allowed paths
   - *Mitigation*: Path allowlist enforcement, staging→promote workflow

2. **Genre Profile Detection**
   - *Risk*: Incorrect genre detection leading to wrong validators
   - *Mitigation*: Manual override, profile validation, clear feedback

#### Low Risk
1. **Offline Adapter Health**
   - *Risk*: Adapter failures breaking workflow
   - *Mitigation*: Health checks, fallback stubs, clear error messages

### Test Strategy

#### Unit Tests (EditMode)
- DTO serialization/deserialization
- Validator logic with known inputs/outputs
- Profile detection and budget application
- Path allowlist and size cap enforcement
- Deterministic plan generation

#### Integration Tests (EditMode)
- Staging→promote atomicity
- Content reference resolution
- Addressables group assignment
- Auto-fix roundtrip cycles

#### Smoke Tests (PlayMode)
- Window responsiveness during background tasks
- Play mode enter/exit with generated content
- Main thread assertions
- Minimal scene execution
- Performance budget validation

#### Cross-Platform Tests
- Windows, macOS, Linux editor compatibility
- Path case-sensitivity
- Line ending normalization

### Acceptance Criteria

#### Functional Requirements
1. **Genre Agnostic**: Support FPS, Platformer, RPG, and extensible to any genre
2. **Natural Language Input**: Accept briefs in plain English
3. **Deterministic Output**: Same brief + seed = identical results
4. **Validation Gating**: No failed validation scenes can be played
5. **Staging Workflow**: All artifacts written to Assets/Generated/** only
6. **Auto-Fix Approval**: Proposals shown, manual approval required

#### Non-Functional Requirements
1. **Performance**: Generated content within Unity performance budgets
2. **Safety**: No writes outside allowed paths, size caps enforced
3. **Maintainability**: Clean separation of concerns, testable components
4. **Extensibility**: Easy to add new genres, validators, adapters

#### Quality Gates
1. **CI Green**: All tests pass on target platforms
2. **Coverage ≥ 80%**: Comprehensive test coverage
3. **Smoke Tests Pass**: All smoke tests pass in CI
4. **Performance Budgets**: Generated content meets performance requirements

### Phase Checklist

#### Phase 0: Kickoff & Docs ✅
- [x] Create development plan
- [ ] Add package README

#### Phase 1: Scaffolding & Smoke Tests
- [ ] Create assembly definitions
- [ ] Add Editor menu item
- [ ] Create empty window
- [ ] Add smoke tests (window, playmode, asmdef boundaries)

#### Phase 2: Orchestration Core
- [ ] Define DTOs (DesignBrief, GamePlan, WorldLayout, etc.)
- [ ] Implement command orchestrator
- [ ] Add determinism and path allowlist tests

#### Phase 3: Planning & World Build
- [ ] PlanGameSliceCommand (stub)
- [ ] BuildWorldLayoutCommand
- [ ] FileTransaction staging→promote
- [ ] Add integration tests

#### Phase 4: Interactions & Content
- [ ] PlaceInteractionsCommand
- [ ] CreateContentBundleCommand
- [ ] Addressables integration
- [ ] Add content reference tests

#### Phase 5: Validation Suite
- [ ] Implement all validators
- [ ] Aggregate ValidationReport
- [ ] Gate Playtest button
- [ ] Add validator unit tests

#### Phase 6: Genre Profiles
- [ ] IGenreProfile interface
- [ ] GenreRegistry
- [ ] Example profiles (FPS, Platformer, RPG)
- [ ] Profile detection tests

#### Phase 7: Director UX
- [ ] Window panels implementation
- [ ] Auto-fix proposal/apply workflow
- [ ] Diff/preview pane
- [ ] Add UX smoke tests

#### Phase 8: Offline Adapters
- [ ] Implement adapter interfaces
- [ ] Health check system
- [ ] JSON repair logic
- [ ] Add adapter integration tests

#### Phase 9: CI & Hardening
- [ ] GitHub Actions workflows
- [ ] Coverage reporting
- [ ] Package configuration
- [ ] Cross-platform testing

### Glossary

- **Director**: Non-programmer user creating game slices
- **Brief**: Natural language description of desired game slice
- **Game Slice**: Self-contained playable game segment
- **Genre Profile**: Configuration defining genre-specific rules and budgets
- **Staging**: Temporary location for generated assets before promotion
- **Promotion**: Atomic move of staged assets to final location
- **Validation Report**: Aggregated results from all validators
- **Auto-Fix**: Automated suggestions for validation failures
- **Delta Plan**: Minimal changes needed to fix validation issues

### Development Notes

#### Threading Model
- All Unity API calls must occur on main thread
- Background orchestration uses async/await patterns
- File I/O operations are thread-safe

#### Memory Management
- Generated assets use Unity's object lifecycle
- Large content bundles use Addressables
- Staging area cleaned up after promotion

#### Error Handling
- Validation failures block playtest
- Adapter failures show health status
- File system errors prevent writes outside allowed paths
- All errors logged with context and recovery suggestions
