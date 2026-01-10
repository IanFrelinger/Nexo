import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import { ExecutionOverlay } from '../ExecutionOverlay';

interface InputNodeData {
  type: 'File' | 'Content' | 'Webhook' | 'Database';
  filePath?: string;
  content?: string;
  webhookUrl?: string;
  outputs?: NodePort[];
  executionState?: NodeExecutionState;
}

interface NodeExecutionState {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  progress?: number;
}

interface NodePort {
  id: string;
  name: string;
  dataType: string;
}

export const InputNode: React.FC<NodeProps<InputNodeData>> = memo(({ data, selected }) => {
  const statusClass = data.executionState?.status === 'running' ? 'running' :
                     data.executionState?.status === 'completed' ? 'complete' :
                     data.executionState?.status === 'failed' ? 'failed' : '';

  return (
    <div className={`input-node min-w-[160px] bg-slate-800 rounded-lg border-2 relative ${
      selected ? 'border-blue-500' : 'border-green-600'
    } ${statusClass}`}>
      <ExecutionOverlay
        status={data.executionState?.status || 'waiting'}
        progress={data.executionState?.progress}
      />
      {/* Header */}
      <div className="node-header p-3 bg-green-900/30 rounded-t-lg flex items-center gap-2 border-b border-slate-700">
        <span className="node-icon text-xl">📥</span>
        <span className="node-name font-semibold text-sm text-white">INPUT</span>
      </div>
      
      {/* Content */}
      <div className="node-content p-3 text-xs text-slate-400">
        <div className="mb-2">
          <span className="text-slate-500">Type:</span> {data.type}
        </div>
        {data.type === 'File' && data.filePath && (
          <div className="text-slate-300 truncate">{data.filePath}</div>
        )}
      </div>
      
      {/* Output handle */}
      {data.outputs?.map((output, i) => (
        <Handle
          key={output.id}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: 40 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white bg-green-500"
        />
      ))}
    </div>
  );
});

InputNode.displayName = 'InputNode';
