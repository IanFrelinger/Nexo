# Playwright UI Validation Tests

This directory contains Playwright tests for validating the Nexo Visual Orchestrator UI.

## Test Files

- **ui-validation.spec.ts**: Tests for basic UI components, layout, and interactions
- **workflow-execution.spec.ts**: Tests for workflow execution, status updates, and console logging
- **agent-interactions.spec.ts**: Tests for agent library, search, configuration, and node management

## Running Tests

### Prerequisites

Make sure the development server is running:
```bash
npm run dev
```

### Run All Tests
```bash
npm run test
```

### Run Tests with UI Mode (Interactive)
```bash
npm run test:ui
```

### Run Tests in Headed Browser
```bash
npm run test:headed
```

### Debug Tests
```bash
npm run test:debug
```

## Test Coverage

### UI Components
- ✅ Toolbar visibility and functionality
- ✅ Agent library display and organization
- ✅ Canvas rendering and interaction
- ✅ Properties panel display and editing
- ✅ Execution console visibility and filtering

### Agent Interactions
- ✅ Drag and drop agents onto canvas
- ✅ Node creation and selection
- ✅ Configuration field display
- ✅ Label editing
- ✅ Node deletion
- ✅ Agent search and filtering

### Workflow Execution
- ✅ Workflow validation before execution
- ✅ Execution start/stop/pause/resume
- ✅ Progress indicators
- ✅ Status updates on nodes
- ✅ Console log generation
- ✅ Output display after execution

### Workflow Management
- ✅ Save workflow to JSON
- ✅ Load workflow from JSON
- ✅ Auto-layout functionality
- ✅ Panel visibility toggling

## Test Results

Test results and screenshots are saved in the `test-results/` directory after each run.

HTML reports are generated in the `playwright-report/` directory. View them with:
```bash
npx playwright show-report
```

