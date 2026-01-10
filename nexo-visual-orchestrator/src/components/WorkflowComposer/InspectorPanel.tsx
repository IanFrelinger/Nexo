import React from 'react';
import type { Node } from 'reactflow';
import { ImplementationToggle } from './ImplementationToggle';
import type { WorkflowDefinition, NodeExecutionState } from '../../types/workflowComposer';

interface InspectorPanelProps {
  node: Node;
  workflow: WorkflowDefinition;
  onUpdate: (node: any) => void;
  executionState?: NodeExecutionState;
}

export const InspectorPanel: React.FC<InspectorPanelProps> = ({
  node,
  workflow: _workflow,
  onUpdate,
  executionState,
}) => {
  const nodeData = node.data;
  
  return (
    <div className="inspector-panel w-80 bg-slate-900 border-l border-slate-700 flex flex-col h-full">
      <div className="inspector-header p-4 border-b border-slate-700">
        <h3 className="text-lg font-bold">Inspector</h3>
        <div className="text-sm text-slate-400 mt-1">{node.type} Node</div>
      </div>
      
      <div className="inspector-content flex-1 overflow-y-auto p-4 space-y-4">
        {/* Node name */}
        <div>
          <label className="block text-sm font-medium text-slate-300 mb-1">Name</label>
          <input
            type="text"
            value={nodeData.name || node.id}
            onChange={(e) => onUpdate({ ...nodeData, name: e.target.value })}
            className="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded text-white text-sm"
          />
        </div>
        
        {/* Implementation toggle for agent/brick nodes */}
        {(node.type === 'agent' || node.type === 'brick') && (
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Implementation Mode
            </label>
            <ImplementationToggle
              value={nodeData.mode || nodeData.implementation || 'Auto'}
              onChange={(value) => {
                if (node.type === 'agent') {
                  onUpdate({ ...nodeData, mode: value });
                } else {
                  onUpdate({ ...nodeData, implementation: value });
                }
              }}
              availableImplementations={{
                deterministic: true,
                agentic: true,
              }}
              showPerBrickOverrides={node.type === 'agent' && nodeData.mode === 'Mixed'}
              bricks={nodeData.behaviors?.flatMap((b: any) => b.bricks) || []}
              brickOverrides={nodeData.brickOverrides || {}}
              onBrickOverrideChange={(brickId, impl) => {
                onUpdate({
                  ...nodeData,
                  brickOverrides: {
                    ...(nodeData.brickOverrides || {}),
                    [brickId]: impl,
                  },
                });
              }}
            />
          </div>
        )}
        
        {/* Parameters */}
        {nodeData.parameters && Object.keys(nodeData.parameters).length > 0 && (
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">Parameters</label>
            <div className="space-y-2">
              {Object.entries(nodeData.parameters).map(([key, value]) => (
                <div key={key} className="flex items-center gap-2">
                  <span className="text-xs text-slate-400 w-24 truncate">{key}:</span>
                  <input
                    type="text"
                    value={String(value)}
                    onChange={(e) => {
                      onUpdate({
                        ...nodeData,
                        parameters: {
                          ...nodeData.parameters,
                          [key]: e.target.value,
                        },
                      });
                    }}
                    className="flex-1 px-2 py-1 bg-slate-800 border border-slate-700 rounded text-white text-xs"
                  />
                </div>
              ))}
            </div>
          </div>
        )}
        
        {/* Execution state */}
        {executionState && (
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">Execution</label>
            <div className="p-3 bg-slate-800 rounded text-xs space-y-1">
              <div className="flex justify-between">
                <span className="text-slate-400">Status:</span>
                <span className={`font-medium ${
                  executionState.status === 'completed' ? 'text-green-400' :
                  executionState.status === 'failed' ? 'text-red-400' :
                  executionState.status === 'running' ? 'text-blue-400' :
                  'text-slate-400'
                }`}>
                  {executionState.status}
                </span>
              </div>
              {executionState.progress !== undefined && (
                <div>
                  <div className="h-2 bg-slate-700 rounded overflow-hidden mt-2">
                    <div
                      className="h-full bg-gradient-to-r from-blue-600 to-purple-600"
                      style={{ width: `${executionState.progress}%` }}
                    />
                  </div>
                  <div className="text-slate-400 mt-1">{executionState.progress}%</div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
