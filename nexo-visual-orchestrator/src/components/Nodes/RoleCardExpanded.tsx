// src/components/Nodes/RoleCardExpanded.tsx

import { HiMinus } from 'react-icons/hi';
import type { RoleDefinition, AgentInstance } from '../../types/workflow';
import { STATUS_STYLES } from './RoleCardConstants';

interface RoleCardExpandedProps {
  role: RoleDefinition;
  instances: AgentInstance[];
  activeInstances: AgentInstance[];
  canScaleDown: boolean;
  onTerminateInstance?: (instanceId: string) => void;
}

export default function RoleCardExpanded({
  role,
  instances,
  activeInstances,
  canScaleDown,
  onTerminateInstance,
}: RoleCardExpandedProps) {
  return (
    <div className="px-3 py-2 space-y-2 max-h-64 overflow-y-auto">
      {/* Role info */}
      <div className="mb-3">
        <p className="text-xs text-slate-500 uppercase tracking-wide mb-1">Owns</p>
        <div className="flex flex-wrap gap-1">
          {role.owns.map((item, i) => (
            <span key={i} className="text-xs bg-slate-700/50 text-slate-300 px-1.5 py-0.5 rounded">
              {item}
            </span>
          ))}
        </div>
      </div>
      
      <div className="border-t border-slate-700 pt-2">
        <p className="text-xs text-slate-500 uppercase tracking-wide mb-2">
          Instances ({activeInstances.length})
        </p>
        
        {instances.length === 0 ? (
          <div className="text-xs text-slate-500 italic py-2 text-center">
            No instances running
          </div>
        ) : (
          <div className="space-y-1">
            {instances.map((instance) => {
              const statusStyle = STATUS_STYLES[instance.status];
              return (
                <div
                  key={instance.id}
                  className="flex items-center justify-between p-2 bg-slate-800/50 rounded"
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <div className={`w-2 h-2 rounded-full ${statusStyle.bg} ${statusStyle.pulse ? 'animate-pulse' : ''}`} />
                    <span className="text-xs text-slate-300 font-mono">
                      #{instance.instanceNumber}
                    </span>
                    <span className="text-xs text-slate-500 truncate">
                      {instance.status}
                    </span>
                  </div>
                  
                  <div className="flex items-center gap-1">
                    {instance.currentTask && (
                      <span className="text-xs text-blue-400 truncate max-w-[60px]" title={instance.currentTask.description}>
                        {instance.currentTask.description.slice(0, 10)}...
                      </span>
                    )}
                    {canScaleDown && instance.status === 'idle' && (
                      <button
                        onClick={() => onTerminateInstance?.(instance.id)}
                        className="p-0.5 hover:bg-red-500/20 rounded text-slate-500 hover:text-red-400"
                        title="Terminate"
                      >
                        <HiMinus className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
      
      {/* Scaling info */}
      <div className="pt-2 border-t border-slate-700 text-xs text-slate-500">
        Scale: {role.scalingConfig.minInstances}-{role.scalingConfig.maxInstances} instances
        {role.scalingConfig.scaleToZero && ' • Can scale to zero'}
      </div>
    </div>
  );
}

