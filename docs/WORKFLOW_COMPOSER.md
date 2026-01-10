# Nexo Visual Workflow Composer

## Overview

The Visual Workflow Composer is an interactive, drag-and-drop interface for building Nexo workflows. Users can visually compose pipelines by dragging agents, behaviors, bricks, and clusters onto a canvas, wire them together, toggle implementations (⚙️/🤖), and execute workflows while watching the framework state in real-time.

**Core Principle:** "Build it like Lego, run it like production." Users assemble pipelines from pre-built pieces without writing code.

## Features

### 1. Visual Canvas
- Drag-and-drop interface using React Flow
- Support for multiple node types:
  - **Input Nodes**: File, content, webhook, or database inputs
  - **Agent Nodes**: Full agents with behaviors and brick-level control
  - **Brick Nodes**: Standalone processing bricks
  - **Cluster Nodes**: Pre-built combinations of agents/bricks
  - **Transform Nodes**: Data manipulation (map, filter, reduce, etc.)
  - **Conditional Nodes**: Branching logic
  - **Output Nodes**: Display, file, webhook, or database outputs

### 2. Implementation Toggle
- **Global Mode**: Set default implementation for entire workflow
- **Node-Level**: Override implementation per agent/brick node
- **Brick-Level**: Fine-grained control over individual bricks within behaviors
- Visual indicators show ⚙️ (deterministic) vs 🤖 (agentic) distribution
- Real-time execution time estimates

### 3. Library Panel
- Browse available agents, bricks, and clusters
- Search and filter capabilities
- Drag items from library to canvas
- Visual indicators for available implementations

### 4. Inspector Panel
- Configure selected nodes
- Adjust implementation modes
- Set parameters
- View execution state in real-time

### 5. Execution Visualization
- Live execution monitoring
- Node state updates (waiting, running, completed, failed)
- Connection animation during data flow
- Framework state display (circuit breakers, cache hits, etc.)
- Metrics dashboard

### 6. Workflow Management
- Save workflows to JSON
- Load workflows from files
- Export to multiple formats (JSON, Docker, C# code)
- Create clusters from selected nodes

## Architecture

### Backend Components

#### Domain Models (`src/Nexo.Core.Domain/Workflows/`)
- `WorkflowDefinition`: Complete workflow structure
- `WorkflowNode`: Base class for all node types
- `WorkflowConnection`: Connections between nodes
- `NodePort`: Input/output ports for connections

#### Application Service (`src/Nexo.Core.Application/Workflows/`)
- `WorkflowExecutor`: Executes user-composed workflows
  - Validates workflow structure
  - Builds execution plan (topological sort)
  - Executes nodes in order
  - Streams execution events
  - Gathers outputs

### Frontend Components

#### Main Composer (`nexo-visual-orchestrator/src/components/WorkflowComposer/`)
- `WorkflowComposer.tsx`: Main component orchestrating the UI
- `LibraryPanel.tsx`: Browse and drag items
- `InspectorPanel.tsx`: Configure selected nodes
- `ExecutionPanel.tsx`: Monitor execution state

#### Node Components (`nexo-visual-orchestrator/src/components/WorkflowComposer/nodes/`)
- `AgentNode.tsx`: Visual representation of agent nodes
- `BrickNode.tsx`: Standalone brick nodes
- `ClusterNode.tsx`: Pre-built clusters
- `InputNode.tsx`: Input data sources
- `OutputNode.tsx`: Output destinations
- `TransformNode.tsx`: Data transformations
- `ConditionalNode.tsx`: Conditional branching

#### Implementation Toggle (`nexo-visual-orchestrator/src/components/WorkflowComposer/ImplementationToggle.tsx`)
- Interactive slider for implementation mode selection
- Per-brick override controls
- Execution time estimates

## Usage

### Accessing the Composer

1. Start the visual orchestrator:
   ```bash
   cd nexo-visual-orchestrator
   npm install
   npm run dev
   ```

2. Click "Workflow Composer" in the toolbar

### Building a Workflow

1. **Add Input Node**: Drag "Input" from library or use the toolbar
2. **Add Processing Nodes**: Drag agents, bricks, or clusters from the library
3. **Connect Nodes**: Click and drag from output ports to input ports
4. **Configure Nodes**: Click a node to open the inspector panel
5. **Set Implementation Mode**: Use the toggle to choose ⚙️ or 🤖
6. **Run Workflow**: Click "Run Workflow" button

### Implementation Toggle

- **Global Mode**: Set in workflow settings
- **Node Mode**: Click node → Inspector → Implementation Mode
- **Brick Overrides**: In Mixed mode, expand "Per-Brick Settings"

### Execution Monitoring

- Watch nodes change color as they execute
- See connection lines animate during data flow
- Monitor metrics in the Execution Panel
- View framework state (circuit breakers, cache, etc.)

## Integration with Backend

### API Endpoint

The workflow executor is available as a service in the .NET backend:

```csharp
// Register in DI container
builder.Services.AddScoped<WorkflowExecutor>();

// Execute workflow
var executor = serviceProvider.GetRequiredService<WorkflowExecutor>();
var result = await executor.ExecuteAsync(workflowDefinition, input);
```

### WebSocket Integration (Future)

Real-time execution updates via SignalR:
- `WorkflowStarted`
- `NodeStarted`
- `NodeCompleted`
- `BrickExecutionEvent`
- `WorkflowCompleted`

## Demo Scenarios

### Scenario 1: Security Pipeline

1. Drag Input node → Configure: `./src/**/*.ts`
2. Drag "Dependency Scanner" agent
3. Drag "OWASP Scanner" agent
4. Connect: Input → Dependency Scanner → OWASP Scanner
5. Toggle OWASP Scanner to ⚙️ (deterministic)
6. Add "Report Generator" agent
7. Toggle Report Generator's "WriteNarrative" brick to 🤖
8. Add Output node → Connect → Run

### Scenario 2: Comparing Implementations

1. Build pipeline (from Scenario 1)
2. Set global mode to "Agentic Preferred" → Run
3. Note: Time, cost, findings
4. Set global mode to "⚙️ Only" → Run
5. Compare: 24x faster, $0 cost, 1 fewer finding

### Scenario 3: Creating Clusters

1. Select all nodes in pipeline
2. Right-click → "Create Cluster"
3. Name: "TypeScript Security Pipeline"
4. Save to library
5. Clear canvas → Drag cluster from library
6. Use as single node in larger workflow

## Technical Details

### Node Types

```typescript
interface AgentNode {
  agentId: string;
  mode: ImplementationMode;
  behaviorOverrides: Record<string, ImplementationMode>;
  brickOverrides: Record<string, ImplementationType>;
  parameters: Record<string, any>;
}

interface BrickNode {
  brickId: string;
  implementation: ImplementationType;
  providerOverride?: string;
  parameters: Record<string, any>;
}
```

### Connection Validation

- Type compatibility checking
- Cycle detection (DAG only)
- Multiple outputs → single input (merge)
- Single output → multiple inputs (fan-out)

### Execution Plan

- Topological sort of nodes
- Parallel execution groups
- Dependency resolution
- Error handling and rollback

## Future Enhancements

- [ ] Real-time WebSocket execution updates
- [ ] Transform expression editor
- [ ] Conditional logic builder
- [ ] Workflow templates library
- [ ] Version control integration
- [ ] Collaborative editing
- [ ] Performance profiling
- [ ] Cost estimation
- [ ] Export to Docker/Kubernetes
- [ ] Export to pure C# code

## Related Documentation

- [Architecture Guide](ARCHITECTURE.md)
- [Quick Start Guide](QUICK_START.md)
- [Defense Deployment](DEFENSE_DEPLOYMENT.md)
