// src/components/Cards/AgentCardInstances.tsx

import { HiPlus, HiMinus } from 'react-icons/hi';
import type { AgentInstance } from '../../types/workflow';
import { STATUS_STYLES } from './AgentCardConstants';

interface AgentCardInstancesProps {
  instances: AgentInstance[];
  activeInstances: AgentInstance[];
  canScaleUp: boolean;
  canScaleDown: boolean;
  onSpawnInstance: () => void;
  onTerminateInstance: (instanceId: string) => void;
}

export default function AgentCardInstances({
  instances,
  activeInstances,
  canScaleUp,
  canScaleDown,
  onSpawnInstance,
  onTerminateInstance,
}: AgentCardInstancesProps) {
  return (
    <div className="border-t border-slate-700/50 pt-2">
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-semibold text-slate-500">
          Instances ({activeInstances.length})
        </p>
        <button
          onClick={(e) => {
            e.stopPropagation();
            if (canScaleUp) onSpawnInstance();
          }}
          disabled={!canScaleUp}
          className={`p-1 rounded transition-colors ${
            canScaleUp 
              ? 'hover:bg-green-500/20 text-green-400 hover:text-green-300' 
              : 'text-slate-600 cursor-not-allowed'
          }`}
          title="Spawn instance"
        >
          <HiPlus className="w-4 h-4" />
        </button>
      </div>
      
      {instances.length === 0 ? (
        <div className="text-xs text-slate-500 italic py-2 text-center">
          No instances running
        </div>
      ) : (
        <div className="space-y-1 max-h-32 overflow-y-auto">
          {instances.map((instance) => {
            const statusStyle = STATUS_STYLES[instance.status];
            return (
              <div
                key={instance.id}
                className="flex items-center justify-between p-1.5 bg-slate-800/50 rounded text-xs"
              >
                <div className="flex items-center gap-2 min-w-0">
                  <div className={`w-2 h-2 rounded-full ${statusStyle.bg} ${statusStyle.pulse ? 'animate-pulse' : ''}`} />
                  <span className="text-slate-300 font-mono">#{instance.instanceNumber}</span>
                  <span className="text-slate-500 truncate">{statusStyle.label}</span>
                </div>
                
                {canScaleDown && instance.status === 'idle' && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onTerminateInstance(instance.id);
                    }}
                    className="p-0.5 hover:bg-red-500/20 rounded text-slate-500 hover:text-red-400 transition-colors"
                    title="Terminate"
                  >
                    <HiMinus className="w-3 h-3" />
                  </button>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

