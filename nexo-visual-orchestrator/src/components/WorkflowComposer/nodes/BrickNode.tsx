import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import { ExecutionOverlay } from '../ExecutionOverlay';

interface BrickNodeData {
  brickId: string;
  name: string;
  description?: string;
  implementation: 'Deterministic' | 'Agentic' | 'Auto';
  providerOverride?: string;
  parameters: Record<string, any>;
  executionState?: NodeExecutionState;
  inputs?: NodePort[];
  outputs?: NodePort[];
}

interface NodePort {
  id: string;
  name: string;
  dataType: string;
}

interface NodeExecutionState {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  progress?: number;
}

export const BrickNode: React.FC<NodeProps<BrickNodeData>> = memo(({ data, selected }) => {
  const { name, implementation, executionState } = data;
  
  const implColor = implementation === 'Deterministic' ? 'blue' : 
                   implementation === 'Agentic' ? 'purple' : 'slate';
  
  const statusClass = executionState?.status === 'running' ? 'running' :
                     executionState?.status === 'completed' ? 'complete' :
                     executionState?.status === 'failed' ? 'failed' : '';

  return (
    <div className={`brick-node min-w-[180px] bg-slate-800 rounded-lg border-2 relative ${
      selected ? 'border-blue-500' : `border-${implColor}-600`
    } ${statusClass}`}>
      <ExecutionOverlay
        status={executionState?.status || 'waiting'}
        progress={executionState?.progress}
      />
      {/* Input handles */}
      {data.inputs?.map((input, i) => (
        <Handle
          key={input.id}
          type="target"
          position={Position.Left}
          id={input.id}
          style={{ top: 30 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white"
        />
      ))}
      
      {/* Header */}
      <div className="node-header p-2 bg-slate-900 rounded-t-lg flex items-center gap-2">
        <span className="node-icon text-lg">🧱</span>
        <span className="node-name flex-1 font-medium text-sm text-white truncate">{name}</span>
        <span className={`node-mode text-xs px-2 py-0.5 rounded ${
          implementation === 'Deterministic' ? 'bg-blue-600' :
          implementation === 'Agentic' ? 'bg-purple-600' :
          'bg-slate-600'
        }`}>
          {implementation === 'Deterministic' ? '⚙️' :
           implementation === 'Agentic' ? '🤖' : '○'}
        </span>
      </div>
      
      {/* Execution state */}
      {executionState?.status === 'running' && (
        <div className="execution-progress p-2 bg-slate-900">
          <div className="h-1 bg-slate-700 rounded overflow-hidden">
            <div 
              className="h-full bg-gradient-to-r from-blue-600 to-purple-600 animate-pulse"
              style={{ width: `${executionState.progress || 0}%` }}
            />
          </div>
        </div>
      )}
      
      {/* Output handles */}
      {data.outputs?.map((output, i) => (
        <Handle
          key={output.id}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: 30 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white"
        />
      ))}
    </div>
  );
});

BrickNode.displayName = 'BrickNode';
