// src/components/Nodes/RoleCardHeader.tsx

import { HiPlus, HiChevronDown, HiChevronUp } from 'react-icons/hi';
import * as HiIcons from 'react-icons/hi';
import type { RoleDefinition, AgentInstance } from '../../types/workflow';
import { COLOR_CLASSES } from './RoleCardConstants';

interface RoleCardHeaderProps {
  role: RoleDefinition;
  activeInstances: AgentInstance[];
  busyCount: number;
  isExpanded: boolean;
  canScaleUp: boolean;
  onSpawnInstance?: (roleId: string) => void;
  onToggleExpand?: (roleId: string) => void;
}

export default function RoleCardHeader({
  role,
  activeInstances,
  busyCount,
  isExpanded,
  canScaleUp,
  onSpawnInstance,
  onToggleExpand,
}: RoleCardHeaderProps) {
  const colors = COLOR_CLASSES[role.color] || COLOR_CLASSES.slate;
  const IconComponent = (HiIcons as any)[role.icon] || HiIcons.HiCube;

  return (
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
        
        <div className="flex items-center gap-2">
          <div className={`
            px-2 py-0.5 rounded-full text-xs font-medium
            ${busyCount > 0 ? 'bg-green-500/20 text-green-400' : 'bg-slate-500/20 text-slate-400'}
          `}>
            {activeInstances.length}/{role.scalingConfig.maxInstances}
          </div>
          
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
  );
}

