// src/components/Nodes/RoleCard.tsx

import { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import type { RoleNodeData, InstanceStatus } from '../../types/workflow';
import * as HiIcons from 'react-icons/hi';
import { 
  HiPlus, HiMinus,
  HiChevronDown, HiChevronUp
} from 'react-icons/hi';

const STATUS_STYLES: Record<InstanceStatus, { bg: string; pulse: boolean }> = {
  'initializing': { bg: 'bg-blue-500', pulse: true },
  'idle': { bg: 'bg-slate-500', pulse: false },
  'busy': { bg: 'bg-green-500', pulse: true },
  'negotiating': { bg: 'bg-yellow-500', pulse: true },
  'waiting-approval': { bg: 'bg-orange-500', pulse: true },
  'terminating': { bg: 'bg-red-500', pulse: true },
  'error': { bg: 'bg-red-600', pulse: false },
};

const TIER_BORDERS: Record<string, string> = {
  'strategic': 'border-l-purple-500',
  'tactical': 'border-l-blue-500',
  'execution': 'border-l-slate-500',
};

const COLOR_CLASSES: Record<string, { bg: string; text: string; border: string }> = {
  purple: { bg: 'bg-purple-500/20', text: 'text-purple-400', border: 'border-purple-500/50' },
  red: { bg: 'bg-red-500/20', text: 'text-red-400', border: 'border-red-500/50' },
  yellow: { bg: 'bg-yellow-500/20', text: 'text-yellow-400', border: 'border-yellow-500/50' },
  cyan: { bg: 'bg-cyan-500/20', text: 'text-cyan-400', border: 'border-cyan-500/50' },
  green: { bg: 'bg-green-500/20', text: 'text-green-400', border: 'border-green-500/50' },
  indigo: { bg: 'bg-indigo-500/20', text: 'text-indigo-400', border: 'border-indigo-500/50' },
  pink: { bg: 'bg-pink-500/20', text: 'text-pink-400', border: 'border-pink-500/50' },
  orange: { bg: 'bg-orange-500/20', text: 'text-orange-400', border: 'border-orange-500/50' },
  slate: { bg: 'bg-slate-500/20', text: 'text-slate-400', border: 'border-slate-500/50' },
  teal: { bg: 'bg-teal-500/20', text: 'text-teal-400', border: 'border-teal-500/50' },
  amber: { bg: 'bg-amber-500/20', text: 'text-amber-400', border: 'border-amber-500/50' },
  emerald: { bg: 'bg-emerald-500/20', text: 'text-emerald-400', border: 'border-emerald-500/50' },
};

function RoleCard({ data, selected }: NodeProps<RoleNodeData>) {
  const { 
    role, 
    instances, 
    isExpanded,
    onToggleExpand,
    onSpawnInstance,
    onTerminateInstance,
  } = data;
  
  const colors = COLOR_CLASSES[role.color] || COLOR_CLASSES.slate;
  const tierBorder = TIER_BORDERS[role.modelConfig.tier];
  const IconComponent = (HiIcons as any)[role.icon] || HiIcons.HiCube;
  
  const activeInstances = instances.filter(i => i.status !== 'terminating');
  const busyCount = instances.filter(i => i.status === 'busy').length;
  const canScaleUp = activeInstances.length < role.scalingConfig.maxInstances;
  const canScaleDown = activeInstances.length > role.scalingConfig.minInstances;
  
  return (
    <div
      className={`
        w-72 rounded-lg border border-l-4 transition-all
        ${colors.bg} ${colors.border} ${tierBorder}
        ${selected ? 'ring-2 ring-white/50 shadow-lg' : 'shadow-md'}
      `}
    >
      {/* Top Handle */}
      <Handle
        type="target"
        position={Position.Top}
        className="w-3 h-3 bg-slate-400 border-2 border-slate-600"
      />
      
      {/* Header */}
      <div className={`px-3 py-2 border-b ${colors.border}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className={`p-1.5 rounded ${colors.bg}`}>
              <IconComponent className={`w-5 h-5 ${colors.text}`} />
            </div>
            <div className="min-w-0">
              <h3 className="font-semibold text-white text-sm truncate">{role.name}</h3>
              <p className="text-xs text-slate-400 truncate">{role.role}</p>
            </div>
          </div>
          
          {/* Instance count badge */}
          <div className="flex items-center gap-2">
            <div className={`
              px-2 py-0.5 rounded-full text-xs font-medium
              ${busyCount > 0 ? 'bg-green-500/20 text-green-400' : 'bg-slate-500/20 text-slate-400'}
            `}>
              {activeInstances.length}/{role.scalingConfig.maxInstances}
            </div>
            
            {/* Actions */}
            <div className="flex items-center">
              <button
                onClick={() => canScaleUp && onSpawnInstance?.(role.id)}
                disabled={!canScaleUp}
                className={`p-1 rounded transition-colors ${
                  canScaleUp 
                    ? 'hover:bg-white/10 text-slate-400 hover:text-green-400' 
                    : 'text-slate-600 cursor-not-allowed'
                }`}
                title="Spawn instance"
              >
                <HiPlus className="w-4 h-4" />
              </button>
              <button
                onClick={() => onToggleExpand?.(role.id)}
                className="p-1 hover:bg-white/10 rounded text-slate-400"
                title={isExpanded ? 'Collapse' : 'Expand'}
              >
                {isExpanded ? <HiChevronUp className="w-4 h-4" /> : <HiChevronDown className="w-4 h-4" />}
              </button>
            </div>
          </div>
        </div>
      </div>
      
      {/* Collapsed: Show summary */}
      {!isExpanded && (
        <div className="px-3 py-2">
          {/* Instance status dots */}
          <div className="flex items-center gap-1 mb-2">
            {instances.slice(0, 10).map((instance) => {
              const statusStyle = STATUS_STYLES[instance.status];
              return (
                <div
                  key={instance.id}
                  className={`w-2 h-2 rounded-full ${statusStyle.bg} ${statusStyle.pulse ? 'animate-pulse' : ''}`}
                  title={`${instance.instanceNumber}: ${instance.status}`}
                />
              );
            })}
            {instances.length > 10 && (
              <span className="text-xs text-slate-500">+{instances.length - 10}</span>
            )}
          </div>
          
          {/* Owns summary */}
          <div className="flex flex-wrap gap-1">
            {role.owns.slice(0, 3).map((item, i) => (
              <span key={i} className="text-xs bg-slate-700/50 text-slate-300 px-1.5 py-0.5 rounded truncate max-w-[80px]">
                {item}
              </span>
            ))}
            {role.owns.length > 3 && (
              <span className="text-xs text-slate-500">+{role.owns.length - 3}</span>
            )}
          </div>
        </div>
      )}
      
      {/* Expanded: Show instances */}
      {isExpanded && (
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
      )}
      
      {/* Bottom Handle */}
      <Handle
        type="source"
        position={Position.Bottom}
        className="w-3 h-3 bg-slate-400 border-2 border-slate-600"
      />
      
      {/* Side Handles for negotiations */}
      <Handle
        type="source"
        position={Position.Right}
        id="negotiate-out"
        className="w-2 h-2 bg-yellow-500 border-2 border-yellow-600"
        style={{ top: '50%' }}
      />
      <Handle
        type="target"
        position={Position.Left}
        id="negotiate-in"
        className="w-2 h-2 bg-yellow-500 border-2 border-yellow-600"
        style={{ top: '50%' }}
      />
    </div>
  );
}

export default memo(RoleCard);

