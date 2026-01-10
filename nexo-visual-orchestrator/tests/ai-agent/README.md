# Nexo Demo Testing Agent

AI-powered testing agent for the Nexo visual workflow composer demo.

## Overview

This testing agent uses a combination of:
- **Visual regression testing** - Screenshot comparison with LLM evaluation
- **Interaction testing** - Simulated user actions
- **Accessibility testing** - ARIA labels, keyboard navigation
- **Performance testing** - Load times, frame rates
- **State validation** - Framework state updates

## Setup

1. Install dependencies:
```bash
npm install
```

2. Set your Anthropic API key (optional - will use fallback mode if not set):
```bash
export ANTHROPIC_API_KEY=your-key-here
```

## Usage

### Run Tests Locally

```bash
# Start the preview server first
npm run preview

# In another terminal, run tests
npm run test:demo

# Run with visible browser
npm run test:demo:headed

# Run with custom URL
npm run test:demo -- --url=http://localhost:3000
```

### CI Integration

```bash
npm run test:demo:ci
```

## Test Structure

- `DemoTestingAgent.ts` - Main orchestrator
- `VisualEvaluator.ts` - LLM-powered visual evaluation
- `InteractionSimulator.ts` - User interaction simulation
- `StateValidator.ts` - State validation
- `ReportGenerator.ts` - HTML/JSON report generation

## Test Categories

1. **Visual Tests** - Layout, appearance, connections
2. **Interaction Tests** - Drag-drop, wiring, toggles
3. **Execution Tests** - Node states, progress, framework state
4. **Performance Tests** - Load time, FPS, memory
5. **Accessibility Tests** - Keyboard, ARIA, contrast

## Reports

Reports are generated in `test-results/demo-tests/`:
- `report.html` - Visual HTML report with screenshots
- `report.json` - Machine-readable JSON report

## Notes

- Without an API key, visual evaluation uses fallback mode (basic checks)
- Tests are designed to be lenient and focus on smoke testing
- Screenshots are captured for all test scenarios
