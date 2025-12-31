// src/components/Toolbar/ExecutionControls.tsx

/**
 * ExecutionControls Component
 * 
 * Toolbar section providing workflow execution controls. Dynamically renders
 * different button sets based on execution status:
 * - Idle: Run button
 * - Running: Pause and Stop buttons
 * - Paused: Resume and Stop buttons
 */

import { HiPlay, HiPause, HiStop } from 'react-icons/hi';
import { useExecutionStore } from '../../stores/executionStore';
import { useMockExecution } from '../../hooks/useMockExecution';
import { useOrchestrationStore } from '../../stores/orchestrationStore';

/**
 * ExecutionControls - Workflow execution control buttons
 * @returns JSX element with context-appropriate execution buttons
 */
export default function ExecutionControls() {
  const { roles } = useOrchestrationStore();
  const { status } = useExecutionStore();
  const { execute, pause, cancel } = useMockExecution();

  const handleRun = async () => {
    if (roles.length === 0) {
      alert('Please add at least one role to the workflow');
      return;
    }
    await execute();
  };

  const isRunning = status === 'running';
  const isPaused = status === 'paused';

  if (!isRunning && !isPaused) {
    return (
      <button
        onClick={handleRun}
        disabled={roles.length === 0}
        className="px-4 py-1.5 bg-green-600 text-white text-sm font-medium rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-1.5"
        title="Run workflow"
      >
        <HiPlay className="w-4 h-4" />
        <span>Run</span>
      </button>
    );
  }

  if (isRunning) {
    return (
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
    );
  }

  if (isPaused) {
    return (
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
    );
  }

  return null;
}

