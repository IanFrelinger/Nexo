// src/components/Cards/AgentCardFooter.tsx

import { HiTrash } from 'react-icons/hi';
import type { RoleDefinition } from '../../types/workflow';
import { COLOR_CLASSES } from './AgentCardConstants';

interface AgentCardFooterProps {
  agent: RoleDefinition;
  onDelete: () => void;
}

export default function AgentCardFooter({ agent, onDelete }: AgentCardFooterProps) {
  const colors = COLOR_CLASSES[agent.color] || COLOR_CLASSES.slate;

  return (
    <div className={`px-4 py-2 border-t ${colors.border} ${colors.accent} flex items-center justify-between`}>
      <div className="text-xs text-slate-500">
        Tier: <span className="text-slate-400 font-semibold">{agent.modelConfig.tier}</span>
      </div>
      <button
        onClick={(e) => {
          e.stopPropagation();
          onDelete();
        }}
        className="p-1.5 hover:bg-red-500/20 rounded text-slate-400 hover:text-red-400 transition-colors"
        title="Delete agent"
      >
        <HiTrash className="w-4 h-4" />
      </button>
    </div>
  );
}

