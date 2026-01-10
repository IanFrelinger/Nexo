// src/components/Canvas/CardCanvas.tsx

/**
 * CardCanvas Component
 * 
 * Main canvas component for the card-based visual orchestrator. Displays agent cards
 * positioned on a grid background with connection lines between related agents.
 * 
 * Features:
 * - Drag-and-drop agent cards
 * - Visual connection lines between agents
 * - Connection preview when creating new relationships
 * - Command flow indicator showing the execution path
 * - Empty state when no agents are present
 * 
 * Uses custom hooks for drag handling, drop handling, connection preview, and
 * command flow calculation.
 */

import { useRef, useMemo, useState } from 'react';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import AgentCard from '../Cards/AgentCard';
import ConnectionLines from './ConnectionLines';
import CommandFlowIndicator from './CommandFlowIndicator';
import {
  useCardDrag,
  useCanvasDrop,
  useConnectionPreview,
  useAgentDefinition,
  useCommandFlow,
} from './CardCanvasHooks';
import type { Relationship } from '../../types/workflow';

interface ConnectionLine {
  from: { x: number; y: number };
  to: { x: number; y: number };
  relationship: Relationship;
}

/**
 * CardCanvas - Main canvas for agent card visualization
 * @returns JSX element
 */
export default function CardCanvas() {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [isConnecting] = useState<string | null>(null);

  const {
    roles,
    relationships,
    expandedRoles,
    selectedRoleId,
    setSelectedRole,
    removeRole,
    toggleRoleExpand,
    spawnInstance,
    terminateInstance,
    getInstancesForRole,
  } = useOrchestrationStore();

  const { draggedCardId, handleCardDragStart, handleCardDrag, handleCardDragEnd } = useCardDrag(canvasRef as React.RefObject<HTMLDivElement>);
  const { handleCanvasDrop, handleCanvasDragOver } = useCanvasDrop(canvasRef as React.RefObject<HTMLDivElement>);
  const connectionPreview = useConnectionPreview(canvasRef as React.RefObject<HTMLDivElement>, isConnecting);
  const getAgentDefinition = useAgentDefinition();
  const commandFlow = useCommandFlow(roles, relationships);

  // Calculate connection lines
  const connectionLines = useMemo<ConnectionLine[]>(() => {
    const lines: ConnectionLine[] = [];
    
    relationships.forEach((rel) => {
      const sourceRole = roles.find(r => r.id === rel.sourceRoleId);
      const targetRole = roles.find(r => r.id === rel.targetRoleId);
      
      if (sourceRole?.position && targetRole?.position) {
        lines.push({
          from: { x: sourceRole.position.x + 160, y: sourceRole.position.y + 100 },
          to: { x: targetRole.position.x + 160, y: targetRole.position.y + 100 },
          relationship: rel,
        });
      }
    });

    return lines;
  }, [relationships, roles]);

  // Connection handlers removed - not currently used in the UI

  return (
    <div
      ref={canvasRef}
      className="flex-1 h-full relative overflow-hidden bg-surface-dark"
      onDrop={handleCanvasDrop}
      onDragOver={handleCanvasDragOver}
      onMouseMove={draggedCardId ? handleCardDrag : undefined}
      onMouseUp={draggedCardId ? handleCardDragEnd : undefined}
      onMouseLeave={draggedCardId ? handleCardDragEnd : undefined}
    >
      {/* Grid background */}
      <div 
        className="absolute inset-0 opacity-20"
        style={{
          backgroundImage: `
            linear-gradient(to right, rgba(148, 163, 184, 0.1) 1px, transparent 1px),
            linear-gradient(to bottom, rgba(148, 163, 184, 0.1) 1px, transparent 1px)
          `,
          backgroundSize: '32px 32px',
        }}
      />

      <ConnectionLines
        connectionLines={connectionLines}
        connectionPreview={connectionPreview}
        roles={roles}
      />

      {/* Agent Cards */}
      <div className="relative" style={{ zIndex: 2 }}>
        {roles.map((role) => {
          const agentDefinition = getAgentDefinition(role);
          const roleInstances = getInstancesForRole(role.id);
          const isInCommandFlow = commandFlow?.includes(role.id) || false;
          const isFlowStart = commandFlow?.[0] === role.id;

          return (
            <div
              key={role.id}
              onMouseDown={(e) => handleCardDragStart(e, role.id)}
              style={{
                position: 'absolute',
                left: role.position?.x || 0,
                top: role.position?.y || 0,
                cursor: draggedCardId === role.id ? 'grabbing' : 'grab',
              }}
            >
              <AgentCard
                agent={role}
                agentDefinition={agentDefinition}
                instances={roleInstances}
                isExpanded={expandedRoles.has(role.id)}
                isSelected={selectedRoleId === role.id}
                isHighlighted={isInCommandFlow}
                onToggleExpand={toggleRoleExpand}
                onSelect={setSelectedRole}
                onDelete={removeRole}
                onSpawnInstance={spawnInstance}
                onTerminateInstance={terminateInstance}
                position={role.position}
              />
              
              {isFlowStart && (
                <div className="absolute -top-2 -left-2 w-4 h-4 bg-green-500 rounded-full border-2 border-surface-dark animate-pulse" />
              )}
            </div>
          );
        })}
      </div>

      <CommandFlowIndicator commandFlow={commandFlow} roles={roles} />

      {/* Empty state */}
      {roles.length === 0 && (
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center text-slate-500">
            <p className="text-lg mb-2">No agents yet</p>
            <p className="text-sm">Drag agents from the library to start composing commands</p>
          </div>
        </div>
      )}
    </div>
  );
}
