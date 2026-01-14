# Desktop Adapter (Windows UI Automation)

On Windows, `DesktopAdapter` will automatically delegate to the optional `WindowsDesktopAdapter` if the assembly is present.

## What you get on Windows

- Screenshot capture
- UI tree (structure/accessibility) export (depth-limited JSON)
- Interactive element discovery (common control types)
- Basic action execution: click / right-click / double-click / type / keypress / focus window

## Project

- `src/Nexo.Agents.UniversalTester.Windows` (targets `net8.0-windows`)
  - Uses FlaUI (`FlaUI.Core`, `FlaUI.UIA3`)

Non-Windows platforms keep the existing “process only” behavior (connect/metrics) and return `null/empty` for UI automation capabilities.

