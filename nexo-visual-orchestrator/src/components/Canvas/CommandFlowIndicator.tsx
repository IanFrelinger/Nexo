// src/components/Canvas/CommandFlowIndicator.tsx

/**
 * CommandFlowIndicator Component
 * 
 * Displays a visual indicator at the bottom-left of the canvas showing the
 * command execution flow. Shows the sequence of agents involved in a command
 * composition, displayed as a horizontal chain (Agent1 → Agent2 → Agent3).
 * 
 * Only visible when there are 2+ agents in the command flow.
 */

import type { RoleDefinition } from '../../types/workflow';

interface CommandFlowIndicatorProps {
  commandFlow: string[] | null;
  roles: RoleDefinition[];
}

export default function CommandFlowIndicator({
  commandFlow,
  roles,
}: CommandFlowIndicatorProps) {
  if (!commandFlow || commandFlow.length <= 1) return null;

  return (
    <div className="absolute bottom-4 left-4 bg-surface border border-slate-700 rounded-lg p-3 shadow-lg z-10">
      <div className="text-xs font-semibold text-slate-400 mb-1">Command Flow</div>
      <div className="flex items-center gap-2 text-xs text-slate-300">
        {commandFlow.map((roleId, index) => {
          const role = roles.find(r => r.id === roleId);
          if (!role) return null;
          
          return (
            <div key={roleId} className="flex items-center gap-2">
              <span className="px-2 py-1 bg-slate-700 rounded">{role.name}</span>
              {index < commandFlow.length - 1 && (
                <span className="text-slate-500">→</span>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

