import { memo, useMemo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import type { AgentNodeData } from '../../types/workflow';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { useOrchestrationStore } from '../../stores/orchestrationStore';

const statusConfig = {
  idle: { border: 'border-slate-600', bg: 'bg-surface', icon: null },
  pending: { border: 'border-status-pending', bg: 'bg-surface', icon: '[PENDING]' },
  running: { border: 'border-status-running', bg: 'bg-surface', icon: '[RUNNING]' },
  completed: { border: 'border-status-completed', bg: 'bg-surface', icon: '[DONE]' },
  failed: { border: 'border-status-failed', bg: 'bg-surface', icon: '[FAILED]' },
  cancelled: { border: 'border-slate-600', bg: 'bg-surface', icon: '[CANCELLED]' },
  conflict: { border: 'border-status-conflict', bg: 'bg-surface', icon: '[CONFLICT]' },
};

function AgentNode({ id, data, selected }: NodeProps<AgentNodeData>) {
  const selectNode = useOrchestrationStore((s) => s.selectNode);
  const agentDef = AGENT_REGISTRY[data.agentType];
  const status = statusConfig[data.status];

  const configPreview = useMemo(() => {
    const entries = Object.entries(data.config).slice(0, 2);
    return entries
      .map(([key, value]) => {
        if (typeof value === 'boolean') return `${key}: ${value ? '[ON]' : '[OFF]'}`;
        if (typeof value === 'string' && value.length > 20) return `${key}: ${value.slice(0, 20)}...`;
        return `${key}: ${value}`;
      })
      .join(', ');
  }, [data.config]);

  return (
    <div
      className={`w-[280px] rounded-lg border-2 ${status.border} ${status.bg} shadow-lg transition-all duration-200 ${
        selected ? 'ring-2 ring-blue-500 ring-offset-2 ring-offset-surface-dark' : ''
      } ${data.status === 'running' ? 'animate-pulse-slow' : ''}`}
      onClick={() => selectNode(id)}
    >
      {/* Header */}
      <div
        className="flex items-center justify-between px-3 py-2 rounded-t-md"
        style={{ backgroundColor: `${agentDef.color}20` }}
      >
        <div className="flex items-center gap-2">
          <span className="text-lg">{agentDef.icon}</span>
          <span className="font-medium text-sm text-slate-100">{data.label}</span>
        </div>
        <div className="flex items-center gap-2">
          {status.icon && <span className="text-sm">{status.icon}</span>}
          <div
            className="w-3 h-3 rounded-full"
            style={{
              backgroundColor:
                data.status === 'idle'
                  ? '#6B7280'
                  : data.status === 'running'
                  ? '#3B82F6'
                  : data.status === 'completed'
                  ? '#22C55E'
                  : data.status === 'failed'
                  ? '#EF4444'
                  : data.status === 'conflict'
                  ? '#F97316'
                  : '#6B7280',
            }}
          />
        </div>
      </div>

      {/* Ports */}
      <div className="px-3 py-2 border-t border-slate-700">
        <div className="flex justify-between text-xs text-slate-400">
          <div className="space-y-1">
            {agentDef.inputs.map((input) => (
              <div key={input.id} className="flex items-center gap-1">
                <Handle
                  type="target"
                  position={Position.Left}
                  id={input.id}
                  className="!w-3 !h-3 !bg-slate-400 !border-2 !border-slate-600"
                  style={{ top: 'auto', position: 'relative', transform: 'none' }}
                />
                <span className={input.required ? 'text-slate-300' : ''}>{input.label}</span>
                {input.required && <span className="text-red-400">*</span>}
              </div>
            ))}
          </div>
          <div className="space-y-1 text-right">
            {agentDef.outputs.map((output) => (
              <div key={output.id} className="flex items-center justify-end gap-1">
                <span>{output.label}</span>
                <Handle
                  type="source"
                  position={Position.Right}
                  id={output.id}
                  className="!w-3 !h-3 !bg-slate-400 !border-2 !border-slate-600"
                  style={{ top: 'auto', position: 'relative', transform: 'none' }}
                />
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Config Preview / Progress */}
      <div className="px-3 py-2 border-t border-slate-700">
        {data.status === 'running' && data.progress !== undefined ? (
          <div className="space-y-1">
            <div className="flex justify-between text-xs text-slate-400">
              <span>Progress</span>
              <span>{Math.round(data.progress)}%</span>
            </div>
            <div className="w-full h-1.5 bg-slate-700 rounded-full overflow-hidden">
              <div
                className="h-full bg-blue-500 transition-all duration-300"
                style={{ width: `${data.progress}%` }}
              />
            </div>
          </div>
        ) : data.error ? (
          <div className="text-xs text-red-400 truncate">{data.error}</div>
        ) : (
          <div className="text-xs text-slate-500 truncate">{configPreview || 'No configuration'}</div>
        )}
      </div>

      {/* Invisible handles for React Flow connection detection */}
      {agentDef.inputs.map((input, i) => (
        <Handle
          key={`target-${input.id}`}
          type="target"
          position={Position.Left}
          id={input.id}
          style={{ top: `${60 + i * 24}px`, opacity: 0 }}
        />
      ))}
      {agentDef.outputs.map((output, i) => (
        <Handle
          key={`source-${output.id}`}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: `${60 + i * 24}px`, opacity: 0 }}
        />
      ))}
    </div>
  );
}

export default memo(AgentNode);

