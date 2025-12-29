// src/components/Cards/AgentCardBehaviors.tsx

import { HiChevronDown, HiChevronUp, HiArrowLeft, HiArrowRight } from 'react-icons/hi';
import * as HiIcons from 'react-icons/hi';
import type { Behavior, AgentDefinition } from '../../types/agents';
import { STATUS_STYLES } from './AgentCardConstants';
import type { AgentInstance } from '../../types/workflow';

interface AgentCardBehaviorsProps {
  behaviors: Behavior[];
  agentDefinition?: AgentDefinition;
  instances: AgentInstance[];
  isExpanded: boolean;
  onToggleExpand: () => void;
}

export default function AgentCardBehaviors({
  behaviors,
  agentDefinition,
  instances,
  isExpanded,
  onToggleExpand,
}: AgentCardBehaviorsProps) {
  const getBehaviorIcon = (iconName?: string) => {
    if (!iconName) return HiIcons.HiCube;
    return (HiIcons as any)[iconName] || HiIcons.HiCube;
  };

  return (
    <div className="px-4 py-3">
      <div className="flex items-center justify-between mb-2">
        <h4 className="text-xs font-semibold text-slate-400 uppercase tracking-wide">Behaviors</h4>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onToggleExpand();
          }}
          className="p-1 hover:bg-white/10 rounded text-slate-400 hover:text-white transition-colors"
          title={isExpanded ? 'Collapse' : 'Expand'}
        >
          {isExpanded ? <HiChevronUp className="w-4 h-4" /> : <HiChevronDown className="w-4 h-4" />}
        </button>
      </div>

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
        </div>
      )}
    </div>
  );
}

