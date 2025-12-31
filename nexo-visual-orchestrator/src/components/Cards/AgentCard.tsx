// src/components/Cards/AgentCard.tsx

/**
 * AgentCard Component
 * 
 * Main card component representing an agent in the visual orchestrator. Displays
 * agent information including header (name, role, instance count), description,
 * behaviors (collapsible), instances list (when expanded), and footer (tier, delete).
 * 
 * Supports selection, expansion/collapse, instance spawning/termination, and
 * positioning on the canvas. Uses memoization for performance optimization.
 */

import { memo } from 'react';
import type { RoleDefinition, AgentInstance } from '../../types/workflow';
import type { AgentDefinition } from '../../types/agents';
import { COLOR_CLASSES } from './AgentCardConstants';
import AgentCardHeader from './AgentCardHeader';
import AgentCardBehaviors from './AgentCardBehaviors';
import AgentCardInstances from './AgentCardInstances';
import AgentCardFooter from './AgentCardFooter';

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
  
  const activeInstances = instances.filter(i => i.status !== 'terminating');
  const busyCount = instances.filter(i => i.status === 'busy').length;
  const canScaleUp = activeInstances.length < agent.scalingConfig.maxInstances;
  const canScaleDown = activeInstances.length > agent.scalingConfig.minInstances;

  const behaviors = agentDefinition?.behaviors || [];

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
      <AgentCardHeader
        agent={agent}
        activeInstances={activeInstances.length}
        busyCount={busyCount}
        maxInstances={agent.scalingConfig.maxInstances}
      />

      {agentDefinition?.description && (
        <div className="px-4 py-2 border-b border-slate-700/50">
          <p className="text-xs text-slate-300 leading-relaxed">{agentDefinition.description}</p>
        </div>
      )}

      <AgentCardBehaviors
        behaviors={behaviors}
        agentDefinition={agentDefinition}
        instances={instances}
        isExpanded={isExpanded}
        onToggleExpand={() => onToggleExpand(agent.id)}
      />

      {isExpanded && (
        <div className="px-4 pb-3">
          <AgentCardInstances
            instances={instances}
            activeInstances={activeInstances}
            canScaleUp={canScaleUp}
            canScaleDown={canScaleDown}
            onSpawnInstance={() => onSpawnInstance(agent.id)}
            onTerminateInstance={onTerminateInstance}
          />
          </div>
        )}

      <AgentCardFooter
        agent={agent}
        onDelete={() => onDelete(agent.id)}
      />
    </div>
  );
}

export default memo(AgentCard);
