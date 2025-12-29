// src/components/Nodes/RoleCard.tsx

import { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import type { RoleNodeData } from '../../types/workflow';
import RoleCardHeader from './RoleCardHeader';
import RoleCardCollapsed from './RoleCardCollapsed';
import RoleCardExpanded from './RoleCardExpanded';
import { COLOR_CLASSES, TIER_BORDERS } from './RoleCardConstants';

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
      
      <RoleCardHeader
        role={role}
        activeInstances={activeInstances}
        busyCount={busyCount}
        isExpanded={isExpanded}
        canScaleUp={canScaleUp}
        onSpawnInstance={onSpawnInstance}
        onToggleExpand={onToggleExpand}
      />
      
      {!isExpanded && (
        <RoleCardCollapsed role={role} instances={instances} />
      )}
      
      {isExpanded && (
        <RoleCardExpanded
          role={role}
          instances={instances}
          activeInstances={activeInstances}
          canScaleDown={canScaleDown}
          onTerminateInstance={onTerminateInstance}
        />
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
