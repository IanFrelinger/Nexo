// src/components/Cards/AgentCard.tsx

import { memo } from 'react';
import * as HiIcons from 'react-icons/hi';
import { 
  HiPlus, HiMinus, HiChevronDown, HiChevronUp,
  HiCog, HiTrash, HiArrowRight, HiArrowLeft,
  HiSparkles, HiPlay, HiPencil, HiChartBar, HiAdjustments,
  HiViewGrid, HiUsers, HiCollection, HiLightBulb
} from 'react-icons/hi';
import type { RoleDefinition, AgentInstance, InstanceStatus } from '../../types/workflow';
import type { AgentDefinition, Behavior } from '../../types/agents';

interface AgentCardProps {
  agent: RoleDefinition;
  agentDefinition?: AgentDefinition;
  instances: AgentInstance[];
  isExpanded: boolean;
  isSelected: boolean;
  isHighlighted?: boolean;
  onToggleExpand: (agentId: string) => void;
  onSelect: (agentId: string) => void;
  onDelete: (agentId: string) => void;
  onSpawnInstance: (agentId: string) => void;
  onTerminateInstance: (instanceId: string) => void;
  onConnect?: (fromAgentId: string, toAgentId: string) => void;
  position?: { x: number; y: number };
  onPositionChange?: (agentId: string, position: { x: number; y: number }) => void;
}

const STATUS_STYLES: Record<InstanceStatus, { bg: string; pulse: boolean; label: string }> = {
  'initializing': { bg: 'bg-blue-500', pulse: true, label: 'Initializing' },
  'idle': { bg: 'bg-slate-500', pulse: false, label: 'Idle' },
  'busy': { bg: 'bg-green-500', pulse: true, label: 'Busy' },
  'negotiating': { bg: 'bg-yellow-500', pulse: true, label: 'Negotiating' },
  'waiting-approval': { bg: 'bg-orange-500', pulse: true, label: 'Waiting Approval' },
  'terminating': { bg: 'bg-red-500', pulse: true, label: 'Terminating' },
  'error': { bg: 'bg-red-600', pulse: false, label: 'Error' },
};

const COLOR_CLASSES: Record<string, { bg: string; text: string; border: string; accent: string }> = {
  purple: { bg: 'bg-purple-500/10', text: 'text-purple-400', border: 'border-purple-500/30', accent: 'bg-purple-500/20' },
  red: { bg: 'bg-red-500/10', text: 'text-red-400', border: 'border-red-500/30', accent: 'bg-red-500/20' },
  yellow: { bg: 'bg-yellow-500/10', text: 'text-yellow-400', border: 'border-yellow-500/30', accent: 'bg-yellow-500/20' },
  cyan: { bg: 'bg-cyan-500/10', text: 'text-cyan-400', border: 'border-cyan-500/30', accent: 'bg-cyan-500/20' },
  green: { bg: 'bg-green-500/10', text: 'text-green-400', border: 'border-green-500/30', accent: 'bg-green-500/20' },
  indigo: { bg: 'bg-indigo-500/10', text: 'text-indigo-400', border: 'border-indigo-500/30', accent: 'bg-indigo-500/20' },
  pink: { bg: 'bg-pink-500/10', text: 'text-pink-400', border: 'border-pink-500/30', accent: 'bg-pink-500/20' },
  orange: { bg: 'bg-orange-500/10', text: 'text-orange-400', border: 'border-orange-500/30', accent: 'bg-orange-500/20' },
  slate: { bg: 'bg-slate-500/10', text: 'text-slate-400', border: 'border-slate-500/30', accent: 'bg-slate-500/20' },
  teal: { bg: 'bg-teal-500/10', text: 'text-teal-400', border: 'border-teal-500/30', accent: 'bg-teal-500/20' },
  amber: { bg: 'bg-amber-500/10', text: 'text-amber-400', border: 'border-amber-500/30', accent: 'bg-amber-500/20' },
  emerald: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/30', accent: 'bg-emerald-500/20' },
};

function AgentCard({
  agent,
  agentDefinition,
  instances,
  isExpanded,
  isSelected,
  isHighlighted,
  onToggleExpand,
  onSelect,
  onDelete,
  onSpawnInstance,
  onTerminateInstance,
  position,
}: AgentCardProps) {
  const colors = COLOR_CLASSES[agent.color] || COLOR_CLASSES.slate;
  const IconComponent = (HiIcons as any)[agent.icon] || HiIcons.HiCube;
  
  const activeInstances = instances.filter(i => i.status !== 'terminating');
  const busyCount = instances.filter(i => i.status === 'busy').length;
  const canScaleUp = activeInstances.length < agent.scalingConfig.maxInstances;
  const canScaleDown = activeInstances.length > agent.scalingConfig.minInstances;

  // Get behaviors from agent definition
  const behaviors = agentDefinition?.behaviors || [];
  
  // Helper to get icon component for behavior
  const getBehaviorIcon = (iconName?: string) => {
    if (!iconName) return HiIcons.HiCube;
    return (HiIcons as any)[iconName] || HiIcons.HiCube;
  };

  const handleCardClick = (e: React.MouseEvent) => {
    if ((e.target as HTMLElement).closest('button')) return;
    onSelect(agent.id);
  };

  return (
    <div
      className={`
        w-80 rounded-lg border-2 transition-all cursor-pointer
        ${colors.border} ${colors.bg}
        ${isSelected ? 'ring-2 ring-white/50 shadow-xl scale-105' : 'shadow-md hover:shadow-lg'}
        ${isHighlighted ? 'ring-2 ring-amber-400/50' : ''}
      `}
      onClick={handleCardClick}
      style={position ? { position: 'absolute', left: position.x, top: position.y } : undefined}
    >
      {/* Header */}
      <div className={`px-4 py-3 border-b ${colors.border} ${colors.accent}`}>
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-3 min-w-0 flex-1">
            <div className={`p-2 rounded-lg ${colors.accent} flex-shrink-0`}>
              <IconComponent className={`w-6 h-6 ${colors.text}`} />
            </div>
            <div className="min-w-0 flex-1">
              <h3 className="font-bold text-white text-base truncate">{agent.name}</h3>
              <p className="text-xs text-slate-400 truncate">{agent.role}</p>
            </div>
          </div>
          
          {/* Instance count badge */}
          <div className="flex items-center gap-2 flex-shrink-0">
            <div className={`
              px-2 py-1 rounded-full text-xs font-semibold
              ${busyCount > 0 ? 'bg-green-500/20 text-green-400' : 'bg-slate-500/20 text-slate-400'}
            `}>
              {activeInstances.length}/{agent.scalingConfig.maxInstances}
            </div>
          </div>
        </div>
      </div>

      {/* Description */}
      {agentDefinition?.description && (
        <div className="px-4 py-2 border-b border-slate-700/50">
          <p className="text-xs text-slate-300 leading-relaxed">{agentDefinition.description}</p>
        </div>
      )}

      {/* Behaviors Section */}
      <div className="px-4 py-3">
        <div className="flex items-center justify-between mb-2">
          <h4 className="text-xs font-semibold text-slate-400 uppercase tracking-wide">Behaviors</h4>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onToggleExpand(agent.id);
            }}
            className="p-1 hover:bg-white/10 rounded text-slate-400 hover:text-white transition-colors"
            title={isExpanded ? 'Collapse' : 'Expand'}
          >
            {isExpanded ? <HiChevronUp className="w-4 h-4" /> : <HiChevronDown className="w-4 h-4" />}
          </button>
        </div>

        {/* Collapsed: Show behavior summary */}
        {!isExpanded && (
          <div className="space-y-2">
            {behaviors.length > 0 ? (
              <div className="space-y-1.5">
                {behaviors.slice(0, 2).map((behavior) => {
                  const BehaviorIcon = getBehaviorIcon(behavior.icon);
                  return (
                    <div
                      key={behavior.id}
                      className="flex items-center gap-2 p-1.5 bg-slate-800/50 rounded border border-slate-700/50"
                    >
                      <BehaviorIcon className="w-3.5 h-3.5 text-slate-400" />
                      <span className="text-xs text-slate-300 font-medium">{behavior.label}</span>
                      <span className="ml-auto text-xs text-slate-500">{behavior.commands.length} commands</span>
                    </div>
                  );
                })}
                {behaviors.length > 2 && (
                  <div className="text-xs text-slate-500 text-center py-1">
                    +{behaviors.length - 2} more behaviors
                  </div>
                )}
              </div>
            ) : (
              <div className="text-xs text-slate-500 italic py-2 text-center">
                No behaviors defined
              </div>
            )}

            {/* Instance status dots */}
            <div className="flex items-center gap-1.5 pt-2 border-t border-slate-700/30">
              <span className="text-xs text-slate-500">Instances:</span>
              <div className="flex items-center gap-1">
                {instances.slice(0, 8).map((instance) => {
                  const statusStyle = STATUS_STYLES[instance.status];
                  return (
                    <div
                      key={instance.id}
                      className={`w-2 h-2 rounded-full ${statusStyle.bg} ${statusStyle.pulse ? 'animate-pulse' : ''}`}
                      title={`#${instance.instanceNumber}: ${statusStyle.label}`}
                    />
                  );
                })}
                {instances.length > 8 && (
                  <span className="text-xs text-slate-500 ml-1">+{instances.length - 8}</span>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Expanded: Show all behaviors with commands */}
        {isExpanded && (
          <div className="space-y-3">
            {behaviors.length > 0 ? (
              behaviors.map((behavior) => {
                const BehaviorIcon = getBehaviorIcon(behavior.icon);
                return (
                  <div key={behavior.id} className="border border-slate-700/50 rounded-lg p-2 bg-slate-800/30">
                    <div className="flex items-center gap-2 mb-2">
                      <BehaviorIcon className="w-4 h-4 text-slate-400" />
                      <h5 className="text-xs font-semibold text-slate-300">{behavior.label}</h5>
                      {behavior.description && (
                        <span className="text-xs text-slate-500 ml-auto" title={behavior.description}>
                          {behavior.description}
                        </span>
                      )}
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs text-slate-500 mb-1">Commands:</p>
                      <div className="flex flex-wrap gap-1">
                        {behavior.commands.map((command) => (
                          <span
                            key={command.id}
                            className="text-xs bg-indigo-500/20 text-indigo-300 px-2 py-0.5 rounded border border-indigo-500/30"
                            title={command.description || command.label}
                          >
                            {command.label}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                );
              })
            ) : (
              <div className="text-xs text-slate-500 italic py-2 text-center">
                No behaviors defined
              </div>
            )}

            {/* Inputs/Outputs (collapsed in expanded view) */}
            {(agentDefinition?.inputs.length || 0) > 0 && (
              <div className="border-t border-slate-700/50 pt-2">
                <p className="text-xs font-semibold text-slate-500 mb-1.5 flex items-center gap-1">
                  <HiArrowLeft className="w-3 h-3" />
                  Inputs ({agentDefinition?.inputs.length || 0})
                </p>
                <div className="flex flex-wrap gap-1">
                  {agentDefinition?.inputs.slice(0, 3).map((input, i) => (
                    <span
                      key={`input-${i}`}
                      className="text-xs bg-blue-500/20 text-blue-300 px-1.5 py-0.5 rounded border border-blue-500/30"
                    >
                      {input.label}
                    </span>
                  ))}
                  {(agentDefinition?.inputs.length || 0) > 3 && (
                    <span className="text-xs text-slate-500">+{(agentDefinition?.inputs.length || 0) - 3}</span>
                  )}
                </div>
              </div>
            )}

            {(agentDefinition?.outputs.length || 0) > 0 && (
              <div>
                <p className="text-xs font-semibold text-slate-500 mb-1.5 flex items-center gap-1">
                  <HiArrowRight className="w-3 h-3" />
                  Outputs ({agentDefinition?.outputs.length || 0})
                </p>
                <div className="flex flex-wrap gap-1">
                  {agentDefinition?.outputs.slice(0, 3).map((output, i) => (
                    <span
                      key={`output-${i}`}
                      className="text-xs bg-green-500/20 text-green-300 px-1.5 py-0.5 rounded border border-green-500/30"
                    >
                      {output.label}
                    </span>
                  ))}
                  {(agentDefinition?.outputs.length || 0) > 3 && (
                    <span className="text-xs text-slate-500">+{(agentDefinition?.outputs.length || 0) - 3}</span>
                  )}
                </div>
              </div>
            )}

            {/* Instances List */}
            <div className="border-t border-slate-700/50 pt-2">
              <div className="flex items-center justify-between mb-2">
                <p className="text-xs font-semibold text-slate-500">
                  Instances ({activeInstances.length})
                </p>
                <div className="flex items-center gap-1">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      if (canScaleUp) onSpawnInstance(agent.id);
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
          </div>
        )}
      </div>

      {/* Footer Actions */}
      <div className={`px-4 py-2 border-t ${colors.border} ${colors.accent} flex items-center justify-between`}>
        <div className="text-xs text-slate-500">
          Tier: <span className="text-slate-400 font-semibold">{agent.modelConfig.tier}</span>
        </div>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(agent.id);
          }}
          className="p-1.5 hover:bg-red-500/20 rounded text-slate-400 hover:text-red-400 transition-colors"
          title="Delete agent"
        >
          <HiTrash className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

export default memo(AgentCard);

