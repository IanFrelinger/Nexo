import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';

interface ConditionalNodeData {
  condition: string;
  inputs?: NodePort[];
  outputs?: NodePort[];
}

interface NodePort {
  id: string;
  name: string;
  dataType: string;
}

export const ConditionalNode: React.FC<NodeProps<ConditionalNodeData>> = memo(({ data, selected }) => {
  return (
    <div className={`conditional-node min-w-[160px] bg-slate-800 rounded-lg border-2 ${
      selected ? 'border-blue-500' : 'border-cyan-600'
    }`}>
      {/* Input handle */}
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
      <div className="node-header p-2 bg-cyan-900/30 rounded-t-lg flex items-center gap-2 border-b border-slate-700">
        <span className="node-icon text-lg">❓</span>
        <span className="node-name font-medium text-sm text-white">Condition</span>
      </div>
      
      {/* Content */}
      <div className="node-content p-2 text-xs text-slate-400">
        <div className="text-slate-300">{data.condition || 'No condition'}</div>
      </div>
      
      {/* Output handles (true/false) */}
      {data.outputs?.map((output, i) => (
        <Handle
          key={output.id}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: 30 + i * 20 }}
          className={`handle w-3 h-3 border-2 border-white ${
            i === 0 ? 'bg-green-500' : 'bg-red-500'
          }`}
        />
      ))}
    </div>
  );
});

ConditionalNode.displayName = 'ConditionalNode';
