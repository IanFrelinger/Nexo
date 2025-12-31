// src/components/Toolbar/FileOperations.tsx

/**
 * FileOperations Component
 * 
 * Toolbar section providing file and workflow management operations:
 * - Open: Load a workflow from a JSON file
 * - Save: Export the current workflow to a JSON file
 * - Clear: Reset the current workflow
 * - Guided Setup: Open the guided workflow creation wizard
 * - Deck Builder: Open the agent deck builder
 */

import { HiFolderOpen, HiSave, HiSparkles, HiCollection } from 'react-icons/hi';
import type { Workflow } from '../../types/workflow';
import { useOrchestrationStore } from '../../stores/orchestrationStore';

interface FileOperationsProps {
  /** Callback to open guided setup mode */
  onShowGuidedMode?: () => void;
  /** Callback to open deck builder */
  onShowDeckBuilder?: () => void;
}

/**
 * FileOperations - File and workflow management operations
 * @param props - Component props
 * @returns JSX element
 */
export default function FileOperations({ onShowGuidedMode, onShowDeckBuilder }: FileOperationsProps) {
  const { roles, relationships, settings, loadWorkflow, clearWorkflow } = useOrchestrationStore();

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

  return (
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
  );
}

