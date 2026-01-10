# Nexo Demo Testing Agent

AI-powered testing agent for the Nexo visual workflow composer demo. **Tests against your LIVE running GUI** (not a test environment).

## Overview

This testing agent uses a combination of:
- **Visual regression testing** - Screenshot comparison with LLM evaluation
- **Interaction testing** - Real user actions (click, drag, type)
- **Accessibility testing** - ARIA labels, keyboard navigation
- **Performance testing** - Load times, frame rates
- **State validation** - Framework state updates

## Quick Start

### 1. Start Your Dev Server

```bash
# In one terminal, start the dev server
npm run dev
```

The dev server will run on `http://localhost:5173` (Vite default).

### 2. Run the AI Testing Agent

```bash
# In another terminal, run the tests
npm run test:live
```

This will:
- ✅ Check if your dev server is running
- ✅ Open a browser window (you can watch it interact!)
- ✅ Test your actual running GUI
- ✅ Generate a detailed report

## Usage

### Watch the Browser Interact (Recommended)

```bash
npm run test:live
# or
npm run test:demo
```

This runs in **headed mode** by default so you can watch the AI agent interact with your GUI in real-time.

### Run Headless (No Browser Window)

```bash
npm run test:live:headless
# or
npm run test:demo:headless
```

### Custom URL

If your dev server runs on a different port:

```bash
npm run test:live -- --url=http://localhost:3000
```

### CI Mode

```bash
npm run test:demo:ci
```

## Setup

### 1. Install Dependencies

```bash
npm install
```

### 2. Set Anthropic API Key (Optional)

For visual evaluation with LLM, set your API key:

**Option 1: Create `.env.local` file (recommended):**
```bash
echo "ANTHROPIC_API_KEY=your-key-here" > .env.local
```

**Option 2: Set as environment variable:**
```bash
export ANTHROPIC_API_KEY=your-key-here
```

Get your API key from: https://console.anthropic.com/

> **Note:** The agent will work without the API key, but visual evaluation will use fallback mode.

## What It Tests

### Visual Tests
- ✅ Initial layout and appearance
- ✅ Library panel visibility
- ✅ Sample workflow rendering
- ✅ Node positioning and connections

### Interaction Tests
- ✅ Loading sample workflows
- ✅ Selecting nodes
- ✅ Toggling implementation modes
- ✅ Dragging from library to canvas

### Execution Tests
- ✅ Run button visibility
- ✅ Framework state sidebar
- ✅ Execution visualization

### Performance Tests
- ✅ Page load time
- ✅ Network requests

### Accessibility Tests
- ✅ Keyboard navigation
- ✅ Focus indicators
- ✅ ARIA labels

## Test Structure

- `DemoTestingAgent.ts` - Main orchestrator
- `VisualEvaluator.ts` - LLM-powered visual evaluation
- `InteractionSimulator.ts` - Real user interaction simulation
- `StateValidator.ts` - State validation
- `ReportGenerator.ts` - HTML/JSON report generation

## Reports

Reports are generated in `test-results/demo-tests/`:
- `report.html` - Visual HTML report with screenshots
- `report.json` - Machine-readable JSON report

## How It Works

1. **Connects to Live GUI**: The agent connects to your running dev server (default: `http://localhost:5173`)
2. **Real Interactions**: Uses Playwright to actually click, drag, and interact with your GUI
3. **Visual Inspection**: Takes screenshots and uses LLM to evaluate UI quality
4. **State Validation**: Checks DOM state, element visibility, and framework state
5. **Generates Report**: Creates detailed HTML report with screenshots and recommendations

## Tips

1. **Watch It Work**: Run in headed mode to see exactly what the agent is doing
2. **Check Console**: The agent logs what it's doing in real-time
3. **Review Reports**: Check the HTML report for detailed analysis
4. **Fix Issues**: The agent provides specific recommendations for failures

## Troubleshooting

### "Server is not running"

Make sure your dev server is running:
```bash
npm run dev
```

### Tests are slow

This is normal! The agent:
- Waits for animations
- Takes screenshots
- Makes LLM API calls (if configured)
- Simulates real user interactions

### Browser doesn't open

Use `--headed` flag or run `npm run test:live` (headed by default).

### Can't find elements

The agent uses multiple selector strategies. If elements aren't found:
1. Check that your GUI is fully loaded
2. Verify the dev server is running
3. Check the browser console for errors
4. Review the HTML report for specific failure details

## Example Output

```
🔍 Checking if server is running at http://localhost:5173...
✅ Server is running at http://localhost:5173
🌐 Testing against LIVE GUI (not preview/test environment)

👁️  Running in HEADED mode - you can watch the browser interact with your GUI

🤖 Demo Testing Agent initialized
📍 Connecting to: http://localhost:5173
📡 Navigating to application...
✅ Application loaded

👁️  Running visual tests...
  ├─ Initial Load - Empty Canvas
  │  ✓ 1.00 - Perfect layout and appearance
  ├─ Library Panel - All Categories
  │  ✓ 0.67 - Library panel accessible
  ...

📊 TEST SUMMARY
════════════════════════════════════════════════════════════
Total:  10
Passed: 8 ✓
Failed: 2 ✗
Score:  79.0%
Time:   132.4s
════════════════════════════════════════════════════════════
```
