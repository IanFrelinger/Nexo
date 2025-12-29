# Playwright UI Validation Tests

This directory contains Playwright tests for validating the Nexo Visual Orchestrator UI.

## Test Files

- **ui-validation.spec.ts**: Tests for basic UI components, layout, and interactions
- **workflow-execution.spec.ts**: Tests for workflow execution, status updates, and console logging
- **agent-interactions.spec.ts**: Tests for agent library, search, configuration, and node management
- **deck-builder.spec.ts**: Tests for deck builder UI, deck creation, agent management, and deck operations
- **deck-store.spec.ts**: Tests for deck store persistence, localStorage operations, and state management
- **deck-integration.spec.ts**: Tests for deck-to-workflow conversion and canvas integration
- **custom-agent-library.spec.ts**: Tests for custom agent library, creation, management, and cross-project reuse

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

### Deck Builder
- ✅ Deck builder UI and modal
- ✅ Create, update, and delete decks
- ✅ Add/remove agents from decks
- ✅ Agent count management
- ✅ Deck search and filtering
- ✅ Deck duplication
- ✅ Deck sharing (private/shared)
- ✅ Deck persistence in localStorage
- ✅ Deck-to-workflow conversion
- ✅ Loading decks onto canvas
- ✅ Cross-project deck usage

### Custom Agent Library
- ✅ Custom agent library UI and panel
- ✅ Create custom agents based on built-in types
- ✅ View modes (All, Favorites, Recent)
- ✅ Search and filter custom agents
- ✅ Toggle favorite status
- ✅ Share/unshare agents across projects
- ✅ Delete and duplicate custom agents
- ✅ Custom agent persistence in localStorage
- ✅ Integration with deck builder
- ✅ Use custom agents in decks

## Test Results

Test results and screenshots are saved in the `test-results/` directory after each run.

HTML reports are generated in the `playwright-report/` directory. View them with:
```bash
npx playwright show-report
```

