import React from 'react';
import type { WorkflowExecutionState } from '../../types/workflowComposer';

interface ExecutionPanelProps {
  executionState: WorkflowExecutionState;
  onClose: () => void;
}

export const ExecutionPanel: React.FC<ExecutionPanelProps> = ({
  executionState,
  onClose,
}) => {
  const { metrics, nodeStates } = executionState;
  
  const progress = Object.values(nodeStates).filter(s => s.status === 'completed').length;
  const total = Object.keys(nodeStates).length;
  const progressPercent = total > 0 ? (progress / total) * 100 : 0;
  
  return (
    <div className="execution-panel w-96 bg-slate-900 border-l border-slate-700 flex flex-col h-full">
      <div className="execution-header p-4 border-b border-slate-700 flex items-center justify-between">
        <h3 className="text-lg font-bold">Execution</h3>
        <button
          onClick={onClose}
          className="text-slate-400 hover:text-white"
        >
          ✕
        </button>
      </div>
      
      <div className="execution-content flex-1 overflow-y-auto p-4 space-y-4">
        {/* Progress */}
        <div>
          <div className="flex justify-between text-sm mb-2">
            <span className="text-slate-400">Progress</span>
            <span className="text-white font-medium">{progressPercent.toFixed(0)}%</span>
          </div>
          <div className="h-4 bg-slate-800 rounded overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-blue-600 to-purple-600 transition-all"
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>
        
        {/* Metrics */}
        <div>
          <h4 className="text-sm font-semibold text-slate-300 mb-2">Metrics</h4>
          <div className="space-y-2 text-xs">
            <div className="flex justify-between">
              <span className="text-slate-400">Nodes Executed:</span>
              <span className="text-white">{metrics.nodesExecuted}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">⚙️ Deterministic:</span>
              <span className="text-blue-400">{metrics.deterministicBricks}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">🤖 Agentic:</span>
              <span className="text-purple-400">{metrics.agenticBricks}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Cache Hits:</span>
              <span className="text-green-400">{metrics.cacheHits}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Total Duration:</span>
              <span className="text-white">{(metrics.totalDuration / 1000).toFixed(2)}s</span>
            </div>
          </div>
        </div>
        
        {/* Node states */}
        <div>
          <h4 className="text-sm font-semibold text-slate-300 mb-2">Node States</h4>
          <div className="space-y-1">
            {Object.entries(nodeStates).map(([nodeId, state]) => (
              <div
                key={nodeId}
                className="p-2 bg-slate-800 rounded text-xs"
              >
                <div className="flex items-center justify-between">
                  <span className="text-slate-300 truncate">{nodeId}</span>
                  <span className={`px-2 py-0.5 rounded text-xs ${
                    state.status === 'completed' ? 'bg-green-600' :
                    state.status === 'failed' ? 'bg-red-600' :
                    state.status === 'running' ? 'bg-blue-600' :
                    'bg-slate-700'
                  }`}>
                    {state.status}
                  </span>
                </div>
                {state.progress !== undefined && (
                  <div className="mt-1 h-1 bg-slate-700 rounded overflow-hidden">
                    <div
                      className="h-full bg-blue-600"
                      style={{ width: `${state.progress}%` }}
                    />
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
