// src/components/Nodes/RoleCardCollapsed.tsx

import type { RoleDefinition, AgentInstance } from '../../types/workflow';
import { STATUS_STYLES } from './RoleCardConstants';

interface RoleCardCollapsedProps {
  role: RoleDefinition;
  instances: AgentInstance[];
}

export default function RoleCardCollapsed({ role, instances }: RoleCardCollapsedProps) {
  return (
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
  );
}

