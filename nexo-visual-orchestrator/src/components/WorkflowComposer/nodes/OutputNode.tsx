import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';

interface OutputNodeData {
  type: 'Display' | 'File' | 'Webhook' | 'Database';
  format: 'Json' | 'Xml' | 'Csv' | 'Markdown' | 'Html' | 'Pdf';
  filePath?: string;
  webhookUrl?: string;
  inputs?: NodePort[];
}

interface NodePort {
  id: string;
  name: string;
  dataType: string;
}

export const OutputNode: React.FC<NodeProps<OutputNodeData>> = memo(({ data, selected }) => {
  return (
    <div className={`output-node min-w-[160px] bg-slate-800 rounded-lg border-2 ${
      selected ? 'border-blue-500' : 'border-orange-600'
    }`}>
      {/* Input handle */}
      {data.inputs?.map((input, i) => (
        <Handle
          key={input.id}
          type="target"
          position={Position.Left}
          id={input.id}
          style={{ top: 40 + i * 20 }}
          className="handle w-3 h-3 border-2 border-white bg-orange-500"
        />
      ))}
      
      {/* Header */}
      <div className="node-header p-3 bg-orange-900/30 rounded-t-lg flex items-center gap-2 border-b border-slate-700">
        <span className="node-icon text-xl">📤</span>
        <span className="node-name font-semibold text-sm text-white">OUTPUT</span>
      </div>
      
      {/* Content */}
      <div className="node-content p-3 text-xs text-slate-400">
        <div className="mb-2">
          <span className="text-slate-500">Format:</span> {data.format}
        </div>
        {data.type === 'File' && data.filePath && (
          <div className="text-slate-300 truncate">{data.filePath}</div>
        )}
      </div>
    </div>
  );
});

OutputNode.displayName = 'OutputNode';
