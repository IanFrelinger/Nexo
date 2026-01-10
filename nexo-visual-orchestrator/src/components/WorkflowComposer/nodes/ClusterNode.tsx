import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';

interface ClusterNodeData {
  clusterId: string;
  name: string;
  description?: string;
  mode: 'Auto' | 'DeterministicOnly' | 'AgenticPreferred' | 'Mixed';
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
}

export const ClusterNode: React.FC<NodeProps<ClusterNodeData>> = memo(({ data, selected }) => {
  const { name, mode } = data;
  
  return (
    <div className={`cluster-node min-w-[200px] bg-slate-800 rounded-lg border-2 ${
      selected ? 'border-blue-500' : 'border-slate-700'
    }`}>
      {/* Input handles */}
      {data.inputs?.map((input, i) => (
        <Handle
          key={input.id}
          type="target"
          position={Position.Left}
          id={input.id}
          style={{ top: 40 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white"
        />
      ))}
      
      {/* Header */}
      <div className="node-header p-3 bg-slate-900 rounded-t-lg flex items-center gap-2 border-b border-slate-700">
        <span className="node-icon text-xl">📦</span>
        <span className="node-name flex-1 font-semibold text-sm text-white truncate">{name}</span>
        <span className="node-mode text-xs">
          {mode === 'DeterministicOnly' && '⚙️'}
          {mode === 'AgenticPreferred' && '🤖'}
          {mode === 'Mixed' && '⚙️🤖'}
        </span>
      </div>
      
      {/* Content */}
      <div className="node-content p-3 text-xs text-slate-400">
        Pre-built combination of agents/bricks
      </div>
      
      {/* Output handles */}
      {data.outputs?.map((output, i) => (
        <Handle
          key={output.id}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: 40 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white"
        />
      ))}
    </div>
  );
});

ClusterNode.displayName = 'ClusterNode';
