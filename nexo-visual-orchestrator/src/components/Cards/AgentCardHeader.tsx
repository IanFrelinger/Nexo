// src/components/Cards/AgentCardHeader.tsx

/**
 * AgentCardHeader Component
 * 
 * Header section of an agent card displaying:
 * - Agent icon (color-coded)
 * - Agent name and role
 * - Active instance count badge (shows busy/idle status)
 */

import * as HiIcons from 'react-icons/hi';
import type { RoleDefinition } from '../../types/workflow';
import { COLOR_CLASSES } from './AgentCardConstants';

interface AgentCardHeaderProps {
  agent: RoleDefinition;
  activeInstances: number;
  busyCount: number;
  maxInstances: number;
}

export default function AgentCardHeader({
  agent,
  activeInstances,
  busyCount,
  maxInstances,
}: AgentCardHeaderProps) {
  const colors = COLOR_CLASSES[agent.color] || COLOR_CLASSES.slate;
  const IconComponent = (HiIcons as any)[agent.icon] || HiIcons.HiCube;

  return (
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
        
        <div className={`
          px-2 py-1 rounded-full text-xs font-semibold
          ${busyCount > 0 ? 'bg-green-500/20 text-green-400' : 'bg-slate-500/20 text-slate-400'}
        `}>
          {activeInstances}/{maxInstances}
        </div>
      </div>
    </div>
  );
}

