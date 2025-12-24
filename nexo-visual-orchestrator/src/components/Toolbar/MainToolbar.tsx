import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { useExecutionStore } from '../../stores/executionStore';
import { useMockExecution } from '../../hooks/useMockExecution';
import { validateWorkflow } from '../../utils/validation';
import { getLayoutedElements } from '../../utils/layoutEngine';

export default function MainToolbar() {
  const { nodes, edges, loadWorkflow, clearWorkflow } = useOrchestrationStore();
  const { status } = useExecutionStore();
  const { execute, pause, cancel } = useMockExecution();

  const handleRun = async () => {
    const workflow = {
      id: 'current',
      name: 'Current Workflow',
      nodes,
      edges,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const errors = validateWorkflow(workflow);
    const hasErrors = errors.some((e) => e.severity === 'error');

    if (hasErrors) {
      alert(`Workflow has errors:\n${errors.map((e) => e.message).join('\n')}`);
      return;
    }

    await execute();
  };

  const handleSave = () => {
    const workflow = {
      id: crypto.randomUUID(),
      name: 'Saved Workflow',
      nodes,
      edges,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(workflow, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `nexo-workflow-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleLoad = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file) return;

      const text = await file.text();
      const workflow = JSON.parse(text);
      loadWorkflow(workflow.nodes, workflow.edges);
    };
    input.click();
  };

  const handleAutoLayout = () => {
    const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(nodes, edges, 'LR');
    loadWorkflow(layoutedNodes, layoutedEdges);
  };

  const isRunning = status === 'running';
  const isPaused = status === 'paused';

  return (
    <div className="h-12 bg-surface border-b border-slate-700 flex items-center justify-between px-4">
      {/* Left: File operations */}
      <div className="flex items-center gap-2">
        <button
          onClick={handleLoad}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors"
        >
          Open
        </button>
        <button
          onClick={handleSave}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors"
        >
          Save
        </button>
        <div className="w-px h-6 bg-slate-700 mx-2" />
        <button
          onClick={clearWorkflow}
          className="px-3 py-1.5 text-sm text-slate-400 hover:bg-surface-light rounded transition-colors"
        >
          Clear
        </button>
      </div>

      {/* Center: Execution controls */}
      <div className="flex items-center gap-2">
        {!isRunning && !isPaused && (
          <button
            onClick={handleRun}
            disabled={nodes.length === 0}
            className="px-4 py-1.5 bg-green-600 text-white text-sm font-medium rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Run
          </button>
        )}
        {isRunning && (
          <>
            <button
              onClick={pause}
              className="px-4 py-1.5 bg-yellow-600 text-white text-sm font-medium rounded hover:bg-yellow-700 transition-colors"
            >
              Pause
            </button>
            <button
              onClick={cancel}
              className="px-4 py-1.5 bg-red-600 text-white text-sm font-medium rounded hover:bg-red-700 transition-colors"
            >
              Stop
            </button>
          </>
        )}
        {isPaused && (
          <>
            <button
              onClick={execute}
              className="px-4 py-1.5 bg-green-600 text-white text-sm font-medium rounded hover:bg-green-700 transition-colors"
            >
              Resume
            </button>
            <button
              onClick={cancel}
              className="px-4 py-1.5 bg-red-600 text-white text-sm font-medium rounded hover:bg-red-700 transition-colors"
            >
              Stop
            </button>
          </>
        )}
      </div>

      {/* Right: View controls */}
      <div className="flex items-center gap-2">
        <button
          onClick={handleAutoLayout}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors"
        >
          Layout
        </button>
      </div>
    </div>
  );
}

