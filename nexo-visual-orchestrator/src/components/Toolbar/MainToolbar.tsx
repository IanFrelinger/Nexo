import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { useExecutionStore } from '../../stores/executionStore';
import { useMockExecution } from '../../hooks/useMockExecution';
import { HiSparkles } from 'react-icons/hi';
import { HiFolderOpen, HiSave, HiPlay, HiPause, HiStop, HiRefresh } from 'react-icons/hi';
import type { Workflow } from '../../types/workflow';

interface MainToolbarProps {
  onShowGuidedMode?: () => void;
}

export default function MainToolbar({ onShowGuidedMode }: MainToolbarProps) {
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
    // Use layout engine for role-based layout
    if (roles.length === 0) return;
    
    // Group by tier for layout
    const tiers: Record<string, typeof roles> = {
      strategic: [],
      tactical: [],
      execution: [],
    };
    
    roles.forEach(role => {
      const tier = role.modelConfig.tier;
      tiers[tier].push(role);
    });
    
    // Layout by tier
    const centerX = 400;
    let currentY = 100;
    
    const layoutedRoles = roles.map((role) => {
      const tier = role.modelConfig.tier;
      const tierIndex = tier === 'strategic' ? 0 : tier === 'tactical' ? 1 : 2;
      const rolesInTier = tiers[tier];
      const indexInTier = rolesInTier.indexOf(role);
      const rolesPerRow = 4;
      
      return {
        ...role,
        position: {
          x: centerX + (indexInTier % rolesPerRow - rolesPerRow / 2) * 300,
          y: currentY + tierIndex * 350 + Math.floor(indexInTier / rolesPerRow) * 320,
        },
      };
    });
    
    loadWorkflow(layoutedRoles, relationships);
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

