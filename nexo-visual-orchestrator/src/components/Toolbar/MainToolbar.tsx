import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { useExecutionStore } from '../../stores/executionStore';
import { useMockExecution } from '../../hooks/useMockExecution';
import { hierarchicalTierLayout } from '../../utils/layoutEngine';
import { HiSparkles, HiCollection } from 'react-icons/hi';
import { HiFolderOpen, HiSave, HiPlay, HiPause, HiStop, HiRefresh } from 'react-icons/hi';
import type { Workflow } from '../../types/workflow';

interface MainToolbarProps {
  onShowGuidedMode?: () => void;
  onShowDeckBuilder?: () => void;
}

export default function MainToolbar({ onShowGuidedMode, onShowDeckBuilder }: MainToolbarProps) {
  const { roles, relationships, settings, loadWorkflow, clearWorkflow } = useOrchestrationStore();
  const { status } = useExecutionStore();
  const { execute, pause, cancel } = useMockExecution();

  const handleRun = async () => {
    if (roles.length === 0) {
      alert('Please add at least one role to the workflow');
      return;
    }

    await execute();
  };

  const handleSave = () => {
    const workflow: Workflow = {
      id: crypto.randomUUID(),
      settings,
      roles,
      relationships,
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
      const workflow: Workflow = JSON.parse(text);
      // Support new format (roles/relationships)
      if (workflow.roles && workflow.relationships) {
        loadWorkflow(workflow.roles, workflow.relationships);
        if (workflow.settings) {
          useOrchestrationStore.getState().updateSettings(workflow.settings);
        }
      } else {
        alert('This workflow file uses an unsupported format. Please regenerate it using the guided mode.');
      }
    };
    input.click();
  };

  const handleAutoLayout = () => {
    // Use hierarchical tier-based layout
    if (roles.length === 0) return;
    
    // Get current roles and relationships from store
    const currentRoles = roles;
    const currentRelationships = relationships;
    
    // Apply layout - this returns new role objects with updated positions
    const layoutedRoles = hierarchicalTierLayout(currentRoles);
    
    // Debug: Log layouted positions
    console.log('Layouted roles positions:', layoutedRoles.map(r => ({
      id: r.id,
      tier: r.modelConfig.tier,
      position: r.position,
    })));
    
    // Load the layouted workflow - positions should already be set correctly by layout function
    loadWorkflow(layoutedRoles, currentRelationships);
  };

  const isRunning = status === 'running';
  const isPaused = status === 'paused';

  return (
    <div className="h-12 bg-surface border-b border-slate-700 flex items-center justify-between px-4">
      {/* Left: File operations */}
      <div className="flex items-center gap-2">
        <button
          onClick={handleLoad}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
          title="Open workflow"
        >
          <HiFolderOpen className="w-4 h-4" />
          <span>Open</span>
        </button>
        <button
          onClick={handleSave}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
          title="Save workflow"
        >
          <HiSave className="w-4 h-4" />
          <span>Save</span>
        </button>
        <div className="w-px h-6 bg-slate-700 mx-2" />
        <button
          onClick={clearWorkflow}
          className="px-3 py-1.5 text-sm text-slate-400 hover:bg-surface-light rounded transition-colors"
        >
          Clear
        </button>
        {onShowGuidedMode && (
          <>
            <div className="w-px h-6 bg-slate-700 mx-2" />
            <button
              onClick={onShowGuidedMode}
              className="px-3 py-1.5 text-sm text-blue-400 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
              title="Start guided setup"
            >
              <HiSparkles className="w-4 h-4" />
              <span>Guided Setup</span>
            </button>
          </>
        )}
        {onShowDeckBuilder && (
          <>
            <div className="w-px h-6 bg-slate-700 mx-2" />
            <button
              onClick={onShowDeckBuilder}
              className="px-3 py-1.5 text-sm text-indigo-400 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
              title="Open deck builder"
            >
              <HiCollection className="w-4 h-4" />
              <span>Deck Builder</span>
            </button>
          </>
        )}
      </div>

      {/* Center: Execution controls */}
      <div className="flex items-center gap-2">
        {!isRunning && !isPaused && (
          <button
            onClick={handleRun}
            disabled={roles.length === 0}
            className="px-4 py-1.5 bg-green-600 text-white text-sm font-medium rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-1.5"
            title="Run workflow"
          >
            <HiPlay className="w-4 h-4" />
            <span>Run</span>
          </button>
        )}
        {isRunning && (
          <>
            <button
              onClick={pause}
              className="px-4 py-1.5 bg-yellow-600 text-white text-sm font-medium rounded hover:bg-yellow-700 transition-colors flex items-center gap-1.5"
              title="Pause workflow"
            >
              <HiPause className="w-4 h-4" />
              <span>Pause</span>
            </button>
            <button
              onClick={cancel}
              className="px-4 py-1.5 bg-red-600 text-white text-sm font-medium rounded hover:bg-red-700 transition-colors flex items-center gap-1.5"
              title="Stop workflow"
            >
              <HiStop className="w-4 h-4" />
              <span>Stop</span>
            </button>
          </>
        )}
        {isPaused && (
          <>
            <button
              onClick={execute}
              className="px-4 py-1.5 bg-green-600 text-white text-sm font-medium rounded hover:bg-green-700 transition-colors flex items-center gap-1.5"
              title="Resume workflow"
            >
              <HiPlay className="w-4 h-4" />
              <span>Resume</span>
            </button>
            <button
              onClick={cancel}
              className="px-4 py-1.5 bg-red-600 text-white text-sm font-medium rounded hover:bg-red-700 transition-colors flex items-center gap-1.5"
              title="Stop workflow"
            >
              <HiStop className="w-4 h-4" />
              <span>Stop</span>
            </button>
          </>
        )}
      </div>

      {/* Right: View controls */}
      <div className="flex items-center gap-2">
        <button
          onClick={handleAutoLayout}
          className="px-3 py-1.5 text-sm text-slate-300 hover:bg-surface-light rounded transition-colors flex items-center gap-1.5"
          title="Auto-layout nodes"
        >
          <HiRefresh className="w-4 h-4" />
          <span>Layout</span>
        </button>
      </div>
    </div>
  );
}

