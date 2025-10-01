# DirectorStudio Robust System

This document describes the battle-tested robust system implemented to make DirectorStudio runs reliable and fully automated in both Editor and headless CI environments.

## 🎯 Problem Solved

The classic "boots fine / nothing fires" problem where:
- Scenes load successfully
- Objects are placed correctly
- But interactions never trigger
- Autoplayer doesn't work in headless mode
- CI tests fail silently

## 🏗️ Architecture Overview

### 1. AutoBootstrap (`Runtime/Boot/AutoBootstrap.cs`)
**Deterministic bootstrap that guarantees runtime prerequisites**

- ✅ Creates EventSystem + UI input module if missing
- ✅ Forces sane headless defaults: `Application.runInBackground = true`, `targetFrameRate = 60`, `QualitySettings.vSyncCount = 0`
- ✅ Ensures MainCamera exists and is properly tagged
- ✅ Spawns AIAutoplayer if missing
- ✅ Rebuilds NavMesh at runtime (procedural scenes won't have baked navmesh)
- ✅ Runs with `[DefaultExecutionOrder(-10000)]` to ensure it runs first

### 2. Interaction System (`Runtime/Interactions/`)

#### IInteraction Interface
```csharp
public interface IInteraction
{
    event Action<IInteraction> Triggered;
    bool IsArmed { get; }
    bool HasTriggered { get; }
    void Initialize();
    void Arm();
    void Trigger();
    InteractionMetadata GetMetadata();
}
```

#### InteractionBus
- Centralized bus for managing all interactions
- Registers, arms, and tracks all interactive objects
- Provides metrics and reporting
- Singleton pattern with DontDestroyOnLoad

#### Clicking Utility
- Programmatic interaction simulation for headless environments
- Simulates mouse clicks, hovers, and drags
- Works with both domain-level calls and UI events

### 3. Headless Input System (`Runtime/Input/HeadlessInputDevices.cs`)
**Adds test devices for New Input System in headless mode**

- ✅ Adds Keyboard, Mouse, and Gamepad devices when missing
- ✅ Sets appropriate update mode for headless compatibility
- ✅ Only activates in batch mode or headless environments

### 4. PlayMode E2E Test (`Tests/PlayMode/Director_E2E_Headless.cs`)
**Reliable automation harness**

- ✅ Builds slice from prompt
- ✅ Loads generated scene
- ✅ Runs autoplayer for N seconds
- ✅ Asserts interactions were triggered
- ✅ Generates JSON test reports
- ✅ Works with `-runTests` in batch mode

### 5. Metrics & Timeouts (`Runtime/Metrics/`)

#### PhaseResult
```csharp
public struct PhaseResult
{
    public bool Ok;
    public string[] Warnings;
    public string[] Errors;
    public Dictionary<string, object> Metrics;
    public float Duration;
    public string PhaseName;
    public DateTime Timestamp;
}
```

#### MetricsCollector
- Collects comprehensive metrics for each run
- Provides timeout protection for each phase
- Generates JSON artifacts for CI
- Includes performance metrics (FPS, memory, etc.)
- Heartbeat system for long-running operations

### 6. Simulation Profiles (`Runtime/Profiles/SimulationProfile.cs`)
**Data-driven configuration for runs**

- ✅ Prompts, durations, and budgets as ScriptableObject data
- ✅ Built-in profiles: Default, QuickTest, StressTest
- ✅ Validation system for profile settings
- ✅ Timeout configuration per phase

### 7. Enhanced Game Objects
**All interactive objects implement IInteraction**

- ✅ `DoomPowerUp` - implements IInteraction + IResettableInteraction
- ✅ `DoomGoal` - implements IInteraction + IResettableInteraction
- ✅ Automatic registration with InteractionBus during placement
- ✅ Programmatic triggering support for autoplayer

### 8. Enhanced Autoplayer (`Runtime/Agents/AIAutoplayer.cs`)
**Actively exercises interactions**

- ✅ Finds nearby interactive objects
- ✅ Calls `Trigger()` directly on IInteraction components
- ✅ Uses Clicking utility for UI-based interactions
- ✅ Configurable interaction intervals

## 🚀 Usage

### Running in Editor
1. Add `AutoBootstrap` component to any GameObject
2. Use `AgentDirector` with `attachAutoplayer = true`
3. Interactions will be automatically registered and triggered

### Running in Headless CI
```bash
# Run PlayMode tests
"/Applications/Unity/Hub/Editor/2022.3.XXf1/Unity" \
  -batchmode -nographics -projectPath ./DirectorStudioUnity \
  -runTests -testPlatform PlayMode \
  -logFile ./unity-playmode.log \
  -testResults ./playmode-results.xml \
  -quit
```

### Using Simulation Profiles
```csharp
// Create a quick test profile
var profile = SimulationProfile.CreateQuickTest();
var runner = DirectorCLIRunner.CreateWithProfile(profile);
StartCoroutine(runner.RunDirectorCLI());
```

## 📊 Metrics & Reporting

### JSON Artifacts Generated
- `Generated/run_artifacts/{runId}/summary.json` - High-level run summary
- `Generated/run_artifacts/{runId}/detailed_metrics.json` - Detailed metrics
- `Generated/test_reports/director_e2e_{timestamp}.json` - Test results

### Key Metrics Tracked
- Total interactions registered
- Interactions triggered
- Phase durations and success rates
- Performance metrics (FPS, memory usage)
- Error and warning counts

## 🔧 Configuration

### AutoBootstrap Settings
- Execution order: -10000 (runs first)
- Headless defaults applied automatically
- NavMesh rebuilding on scene load

### InteractionBus Settings
- Singleton with DontDestroyOnLoad
- Automatic registration of IInteraction components
- Metrics collection and reporting

### MetricsCollector Settings
- Heartbeat interval: 1 second
- Default phase timeout: 10 seconds
- Detailed metrics: enabled by default

## 🐛 Common Issues Fixed

1. **No EventSystem** - AutoBootstrap creates one
2. **No MainCamera** - AutoBootstrap creates one
3. **Interactions not triggering** - InteractionBus registers and arms them
4. **Autoplayer not working** - Enhanced to actively trigger interactions
5. **Headless input issues** - HeadlessInputDevices adds test devices
6. **Silent failures** - Comprehensive metrics and timeout system
7. **No NavMesh** - AutoBootstrap rebuilds at runtime
8. **Script execution order** - AutoBootstrap runs first

## 🧪 Testing

### Unit Tests
- Individual component testing
- Interface implementation validation
- Profile validation

### Integration Tests
- End-to-end workflow testing
- Interaction system testing
- Metrics collection testing

### PlayMode Tests
- Full DirectorStudio pipeline
- Headless compatibility
- CI-ready automation

## 📈 Performance

### Optimizations
- Efficient interaction registration
- Minimal overhead metrics collection
- Configurable heartbeat intervals
- Timeout protection prevents hanging

### Monitoring
- Real-time FPS tracking
- Memory usage monitoring
- Phase duration tracking
- Error rate monitoring

## 🔮 Future Enhancements

1. **Advanced Interaction Types** - More complex interaction patterns
2. **Machine Learning Metrics** - AI-driven performance analysis
3. **Distributed Testing** - Multi-machine test execution
4. **Real-time Monitoring** - Live metrics dashboard
5. **Custom Profiles** - User-defined simulation profiles

## 📝 Best Practices

1. **Always use AutoBootstrap** - Ensures consistent environment
2. **Register interactions immediately** - During object creation
3. **Use SimulationProfiles** - For data-driven configuration
4. **Monitor metrics** - Check JSON artifacts for insights
5. **Test in headless mode** - Verify CI compatibility
6. **Use timeouts** - Prevent hanging operations
7. **Implement IResettableInteraction** - For test reusability

## 🎉 Results

This robust system ensures that DirectorStudio runs:
- ✅ **Reliably** - Deterministic bootstrap and error handling
- ✅ **Automatically** - Full headless CI support
- ✅ **Observably** - Comprehensive metrics and reporting
- ✅ **Configurably** - Data-driven simulation profiles
- ✅ **Testably** - PlayMode E2E tests with assertions

The "boots fine / nothing fires" problem is now solved with a battle-tested, production-ready system that works consistently across all environments.
