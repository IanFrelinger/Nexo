// src/components/Canvas/CardCanvasHooks.ts

/**
 * Card Canvas Hooks
 * 
 * Custom React hooks for the card-based canvas functionality:
 * - useCardDrag: Handles drag-and-drop of agent cards
 * - useCanvasDrop: Handles dropping new agents onto the canvas
 * - useConnectionPreview: Shows preview line when creating connections
 * - useAgentDefinition: Retrieves agent definitions for roles
 * - useCommandFlow: Calculates command execution flow through agents
 * 
 * These hooks encapsulate the complex interaction logic for the card canvas.
 */

import { useCallback, useMemo, useState, useEffect, useRef } from 'react';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { nanoid } from 'nanoid';
import type { RoleDefinition, Relationship } from '../../types/workflow';

/**
 * Hook for handling card drag operations
 * @param canvasRef - Reference to the canvas container element
 * @returns Drag state and handlers
 */
export function useCardDrag(canvasRef: React.RefObject<HTMLDivElement>) {
  const [draggedCardId, setDraggedCardId] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState<{ x: number; y: number } | null>(null);
  const { roles } = useOrchestrationStore();

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
  }, [canvasRef]);

  const handleCardDrag = useCallback((e: React.MouseEvent) => {
    if (!draggedCardId || !dragOffset || !canvasRef.current) return;
    
    const canvasRect = canvasRef.current.getBoundingClientRect();
    const newX = e.clientX - canvasRect.left - dragOffset.x;
    const newY = e.clientY - canvasRect.top - dragOffset.y;
    
    const role = roles.find(r => r.id === draggedCardId);
    if (role) {
      useOrchestrationStore.getState().updateRole(draggedCardId, {
        position: { x: Math.max(0, newX), y: Math.max(0, newY) },
      });
    }
  }, [draggedCardId, dragOffset, roles]);

  const handleCardDragEnd = useCallback(() => {
    setDraggedCardId(null);
    setDragOffset(null);
  }, []);

  return {
    draggedCardId,
    handleCardDragStart,
    handleCardDrag,
    handleCardDragEnd,
  };
}

/**
 * Hook for handling canvas drop operations (adding new agents)
 * @param canvasRef - Reference to the canvas container element
 * @returns Drop handlers
 */
export function useCanvasDrop(canvasRef: React.RefObject<HTMLDivElement>) {
  const { addRole } = useOrchestrationStore();

  const handleCanvasDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    const roleId = e.dataTransfer.getData('application/reactflow');
    if (!roleId) return;

    const template = ROLE_TEMPLATES[roleId];
    if (!template) return;

    const canvasRect = canvasRef.current?.getBoundingClientRect();
    if (!canvasRect) return;

    const x = e.clientX - canvasRect.left - 160;
    const y = e.clientY - canvasRect.top - 100;

    const newRole: RoleDefinition = {
      ...template,
      id: nanoid(),
      position: { x: Math.max(0, x), y: Math.max(0, y) },
    };

    addRole(newRole);
  }, [addRole, canvasRef]);

  const handleCanvasDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }, []);

  return { handleCanvasDrop, handleCanvasDragOver };
}

/**
 * Hook for connection preview when creating relationships between agents
 * @param canvasRef - Reference to the canvas container element
 * @param isConnecting - ID of the source agent being connected, or null
 * @returns Connection preview state with source and target positions
 */
export function useConnectionPreview(
  canvasRef: React.RefObject<HTMLDivElement>,
  isConnecting: string | null
) {
  const [connectionPreview, setConnectionPreview] = useState<{ from: string; to: { x: number; y: number } } | null>(null);
  const { roles } = useOrchestrationStore();

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
    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      setConnectionPreview(null);
    };
  }, [isConnecting, roles, canvasRef]);

  return connectionPreview;
}

/**
 * Hook for retrieving agent definitions from role definitions
 * @returns Function that takes a role and returns its agent definition
 */
export function useAgentDefinition() {
  return useCallback((role: RoleDefinition) => {
    const baseRoleId = role.id.split('-')[0];
    
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
      'validator': 'balance-analyzer',
      'build-agent': 'unity-build',
      'playtest': 'ai-player',
      'feedback': 'feedback-synthesizer',
    };

    let agentType = roleToAgentMap[baseRoleId];
    
    if (!agentType) {
      agentType = Object.keys(AGENT_REGISTRY).find(
        key => AGENT_REGISTRY[key as keyof typeof AGENT_REGISTRY].label.toLowerCase() === role.name.toLowerCase()
      ) as keyof typeof AGENT_REGISTRY | undefined;
    }
    
    return agentType ? AGENT_REGISTRY[agentType] : undefined;
  }, []);
}

/**
 * Hook for calculating command execution flow through agents
 * Traces the path from architect through delegation relationships
 * @param roles - Array of role definitions
 * @param relationships - Array of relationships between roles
 * @returns Array of role IDs in execution order, or null if no architect found
 */
export function useCommandFlow(roles: RoleDefinition[], relationships: Relationship[]) {
  return useMemo(() => {
    const architect = roles.find(r => r.role.toLowerCase().includes('architect'));
    if (!architect) return null;

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
}

