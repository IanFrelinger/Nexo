// src/components/Canvas/CardCanvas.tsx

import { useCallback, useRef, useMemo, useState, useEffect } from 'react';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import AgentCard from '../Cards/AgentCard';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import type { RoleDefinition, Relationship } from '../../types/workflow';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import { nanoid } from 'nanoid';

interface ConnectionLine {
  from: { x: number; y: number };
  to: { x: number; y: number };
  relationship: Relationship;
}

export default function CardCanvas() {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [draggedCardId, setDraggedCardId] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState<{ x: number; y: number } | null>(null);
  const [isConnecting, setIsConnecting] = useState<string | null>(null);
  const [connectionPreview, setConnectionPreview] = useState<{ from: string; to: { x: number; y: number } } | null>(null);

  const {
    roles,
    instances,
    relationships,
    expandedRoles,
    selectedRoleId,
    setSelectedRole,
    removeRole,
    addRelationship,
    addRole,
    toggleRoleExpand,
    spawnInstance,
    terminateInstance,
    getInstancesForRole,
    highlightedPath,
  } = useOrchestrationStore();

  // Calculate connection lines between cards
  const connectionLines = useMemo<ConnectionLine[]>(() => {
    const lines: ConnectionLine[] = [];
    
    relationships.forEach((rel) => {
      const sourceRole = roles.find(r => r.id === rel.sourceRoleId);
      const targetRole = roles.find(r => r.id === rel.targetRoleId);
      
      if (sourceRole?.position && targetRole?.position) {
        lines.push({
          from: { x: sourceRole.position.x + 160, y: sourceRole.position.y + 100 }, // Center of card
          to: { x: targetRole.position.x + 160, y: targetRole.position.y + 100 },
          relationship: rel,
        });
      }
    });

    return lines;
  }, [relationships, roles]);

  // Handle card drag start
  const handleCardDragStart = useCallback((e: React.MouseEvent, agentId: string) => {
    const card = e.currentTarget as HTMLElement;
    const rect = card.getBoundingClientRect();
    const canvasRect = canvasRef.current?.getBoundingClientRect();
    
    if (!canvasRect) return;
    
    setDraggedCardId(agentId);
    setDragOffset({
      x: e.clientX - rect.left - canvasRect.left,
      y: e.clientY - rect.top - canvasRect.top,
    });
  }, []);

  // Handle card drag
  const handleCardDrag = useCallback((e: React.MouseEvent) => {
    if (!draggedCardId || !dragOffset || !canvasRef.current) return;
    
    const canvasRect = canvasRef.current.getBoundingClientRect();
    const newX = e.clientX - canvasRect.left - dragOffset.x;
    const newY = e.clientY - canvasRect.top - dragOffset.y;
    
    // Update role position in store
    const role = roles.find(r => r.id === draggedCardId);
    if (role) {
      useOrchestrationStore.getState().updateRole(draggedCardId, {
        position: { x: Math.max(0, newX), y: Math.max(0, newY) },
      });
    }
  }, [draggedCardId, dragOffset, roles]);

  // Handle card drag end
  const handleCardDragEnd = useCallback(() => {
    setDraggedCardId(null);
    setDragOffset(null);
  }, []);

  // Handle canvas drop (from library)
  const handleCanvasDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    const roleId = e.dataTransfer.getData('application/reactflow');
    if (!roleId) return;

    const template = ROLE_TEMPLATES[roleId];
    if (!template) return;

    const canvasRect = canvasRef.current?.getBoundingClientRect();
    if (!canvasRect) return;

    const x = e.clientX - canvasRect.left - 160; // Center card on drop point
    const y = e.clientY - canvasRect.top - 100;

    const newRole: RoleDefinition = {
      ...template,
      id: nanoid(),
      position: { x: Math.max(0, x), y: Math.max(0, y) },
    };

    addRole(newRole);
  }, [addRole]);

  const handleCanvasDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }, []);

  // Handle connection start
  const handleConnectStart = useCallback((agentId: string) => {
    setIsConnecting(agentId);
  }, []);

  // Handle connection end
  const handleConnectEnd = useCallback((targetAgentId: string) => {
    if (!isConnecting || isConnecting === targetAgentId) {
      setIsConnecting(null);
      setConnectionPreview(null);
      return;
    }

    // Create relationship
    const newRelationship: Relationship = {
      id: nanoid(),
      sourceRoleId: isConnecting,
      targetRoleId: targetAgentId,
      type: 'delegates',
      description: 'Agent delegation',
    };

    addRelationship(newRelationship);
    setIsConnecting(null);
    setConnectionPreview(null);
  }, [isConnecting, addRelationship]);

  // Track mouse for connection preview
  useEffect(() => {
    if (!isConnecting) return;

    const handleMouseMove = (e: MouseEvent) => {
      const canvasRect = canvasRef.current?.getBoundingClientRect();
      if (!canvasRect) return;

      const sourceRole = roles.find(r => r.id === isConnecting);
      if (!sourceRole?.position) return;

      setConnectionPreview({
        from: isConnecting,
        to: {
          x: e.clientX - canvasRect.left,
          y: e.clientY - canvasRect.top,
        },
      });
    };

    window.addEventListener('mousemove', handleMouseMove);
    return () => window.removeEventListener('mousemove', handleMouseMove);
  }, [isConnecting, roles]);

  // Get agent definition for each role
  const getAgentDefinition = useCallback((role: RoleDefinition) => {
    // Extract base role ID from composite ID (e.g., "architect-123-0" -> "architect")
    const baseRoleId = role.id.split('-')[0];
    
    // Map role template IDs to agent types
    const roleToAgentMap: Record<string, keyof typeof AGENT_REGISTRY> = {
      'architect': 'architect',
      'combat': 'combat',
      'economy': 'economy',
      'ai-behavior': 'ai-behavior',
      'level-design': 'level-design',
      'narrative': 'narrative',
      'image-gen': 'image-gen',
      'audio-gen': 'audio-gen',
      'model-3d-gen': 'model-3d-gen',
      'unity-build': 'unity-build',
      'ai-player': 'ai-player',
      'balance-analyzer': 'balance-analyzer',
      'feedback-synthesizer': 'feedback-synthesizer',
      'validator': 'balance-analyzer', // Map validator to balance-analyzer
      'build-agent': 'unity-build', // Map build-agent to unity-build
      'playtest': 'ai-player', // Map playtest to ai-player
      'feedback': 'feedback-synthesizer', // Map feedback to feedback-synthesizer
    };

    // Try to find by base role ID first
    let agentType = roleToAgentMap[baseRoleId];
    
    // If not found, try to match by role name
    if (!agentType) {
      agentType = Object.keys(AGENT_REGISTRY).find(
        key => AGENT_REGISTRY[key as keyof typeof AGENT_REGISTRY].label.toLowerCase() === role.name.toLowerCase()
      ) as keyof typeof AGENT_REGISTRY | undefined;
    }
    
    return agentType ? AGENT_REGISTRY[agentType] : undefined;
  }, []);

  // Calculate command composition visualization
  const commandFlow = useMemo(() => {
    // Find architect (entry point)
    const architect = roles.find(r => r.role.toLowerCase().includes('architect'));
    if (!architect) return null;

    // Build flow from architect through relationships
    const flow: string[] = [architect.id];
    const visited = new Set<string>([architect.id]);

    const buildFlow = (roleId: string) => {
      relationships
        .filter(r => r.sourceRoleId === roleId && !visited.has(r.targetRoleId))
        .forEach(rel => {
          visited.add(rel.targetRoleId);
          flow.push(rel.targetRoleId);
          buildFlow(rel.targetRoleId);
        });
    };

    buildFlow(architect.id);
    return flow;
  }, [roles, relationships]);

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

      {/* Connection lines */}
      <svg className="absolute inset-0 pointer-events-none" style={{ zIndex: 1 }}>
        {connectionLines.map((line) => (
          <line
            key={`${line.relationship.sourceRoleId}-${line.relationship.targetRoleId}`}
            x1={line.from.x}
            y1={line.from.y}
            x2={line.to.x}
            y2={line.to.y}
            stroke="#6366F1"
            strokeWidth="2"
            strokeDasharray="5,5"
            opacity={0.5}
          />
        ))}
        
        {/* Connection preview */}
        {connectionPreview && (() => {
          const sourceRole = roles.find(r => r.id === connectionPreview.from);
          if (!sourceRole?.position) return null;
          
          return (
            <line
              x1={sourceRole.position.x + 160}
              y1={sourceRole.position.y + 100}
              x2={connectionPreview.to.x}
              y2={connectionPreview.to.y}
              stroke="#10B981"
              strokeWidth="2"
              strokeDasharray="3,3"
              opacity={0.7}
            />
          );
        })()}
      </svg>

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
              
              {/* Command flow indicator */}
              {isFlowStart && (
                <div className="absolute -top-2 -left-2 w-4 h-4 bg-green-500 rounded-full border-2 border-surface-dark animate-pulse" />
              )}
            </div>
          );
        })}
      </div>

      {/* Command Composition Info */}
      {commandFlow && commandFlow.length > 1 && (
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
      )}

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

