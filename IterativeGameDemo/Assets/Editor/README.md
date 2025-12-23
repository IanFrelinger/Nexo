# Unity Editor Integration for Iterative Demo

This folder contains Unity Editor scripts that integrate the iterative game development demo with the Unity Editor, allowing you to view and interact with iterations in real-time.

## Features

### 1. Iterative Demo Window
A real-time status window that shows:
- Current iteration number
- Quality score and progress
- Status of latest iteration
- List of all iterations with quality scores
- Auto-refresh every 2 seconds

**Access:** `Nexo > Iterative Demo Window`

### 2. Menu Commands
- **Nexo > Refresh Iteration Assets** - Manually refresh assets for the latest iteration
- **Nexo > Open Iterative Demo Window** - Open the status window
- **Nexo > Run Single Iteration** - Run one iteration from within Unity Editor

### 3. Automatic Asset Updates
When running the demo script with `OPEN_EDITOR=true`, the demo will:
- Keep the Unity Editor open throughout iterations
- Automatically refresh assets after each iteration
- Update the project view to show new iteration assets

## Usage

### Running the Demo with Editor Integration

```bash
# Run with editor open
OPEN_EDITOR=true ./scripts/iterative-game-demo.sh

# Or with custom settings
OPEN_EDITOR=true MAX_ITERATIONS=3 MIN_QUALITY_SCORE=7.0 ./scripts/iterative-game-demo.sh
```

### Viewing Iterations in Unity

1. Open Unity Editor (it will open automatically if `OPEN_EDITOR=true`)
2. Go to `Nexo > Iterative Demo Window`
3. The window will show:
   - Current iteration status
   - Quality scores
   - List of all iterations
4. Click "View" next to any iteration to open its folder

### Manual Control

- **Refresh Assets**: Use `Nexo > Refresh Iteration Assets` to manually update
- **Run Iteration**: Use `Nexo > Run Single Iteration` to run one iteration from Unity
- **Auto-Refresh**: Toggle "Auto Refresh" in the demo window to enable/disable automatic updates

## Files

- `IterativeDemoWindow.cs` - Main status window
- `IterativeDemoController.cs` - Menu commands and asset management
- `IterativeDemoAssetUpdater.cs` - CLI-callable asset refresh method

## How It Works

1. The demo script creates assets in `Assets/Iteration{N}/` folders
2. After each iteration, it calls Unity Editor to refresh assets
3. The demo window reads iteration results from `Artifacts/iteration-{N}/` folders
4. Auto-refresh polls for new iterations every 2 seconds
5. Asset database refresh ensures Unity shows new files immediately

## Requirements

- Unity 2021.2 or newer (for System.Text.Json support)
- Iterative demo script must be run from project root
- Unity Editor must be open (or will be opened automatically)
