// src/components/Canvas/OrchestrationCanvasHooks.ts

import { useEffect } from 'react';
import { useReactFlow } from 'reactflow';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import { nanoid } from 'nanoid';
import type { RoleDefinition } from '../../types/workflow';

export function useTestRoleAddition(addRole: (role: RoleDefinition) => void) {
  useEffect(() => {
    let pendingAdditions: Array<{ templateId: string; position?: { x: number; y: number } }> = [];
    let isProcessing = false;
    
    const processPendingAdditions = async () => {
      if (isProcessing || pendingAdditions.length === 0) return;
      isProcessing = true;
      
      while (pendingAdditions.length > 0) {
        const { templateId, position: providedPosition } = pendingAdditions.shift()!;
        const template = ROLE_TEMPLATES[templateId];
        if (!template) continue;

        await new Promise(resolve => setTimeout(resolve, 10));
        const existingRoles = useOrchestrationStore.getState().roles;
        let finalPosition: { x: number; y: number };
        
        const nodeWidth = 288;
        const nodeHeight = 200;
        const spacing = 50;
        const startX = 100;
        const startY = 100;
        const nodesPerRow = 4;
        
        if (providedPosition) {
          const hasOverlap = existingRoles.some(role => {
            const dx = Math.abs(role.position.x - providedPosition.x);
            const dy = Math.abs(role.position.y - providedPosition.y);
            return dx < (nodeWidth + spacing) && dy < (nodeHeight + spacing);
          });
          
          if (hasOverlap) {
            const lastRow = Math.floor(existingRoles.length / nodesPerRow);
            const lastCol = existingRoles.length % nodesPerRow;
            finalPosition = {
              x: startX + lastCol * (nodeWidth + spacing),
              y: startY + lastRow * (nodeHeight + spacing),
            };
          } else {
            finalPosition = providedPosition;
          }
        } else {
          const lastRow = Math.floor(existingRoles.length / nodesPerRow);
          const lastCol = existingRoles.length % nodesPerRow;
          finalPosition = {
            x: startX + lastCol * (nodeWidth + spacing),
            y: startY + lastRow * (nodeHeight + spacing),
          };
        }

        const newRole: RoleDefinition = {
          id: `${templateId}-${nanoid(6)}`,
          name: template.name,
          role: template.role,
          description: template.description,
          icon: template.icon,
          color: template.color,
          owns: [...template.owns],
          capabilities: [...template.capabilities],
          autonomyBounds: template.autonomyBounds.map(b => ({ ...b })),
          escalationRules: template.escalationTriggers.map(trigger => ({
            trigger,
            escalateTo: 'architect',
            timeout: 300,
          })),
          reportsTo: null,
          canDelegateTo: [],
          negotiatesWith: [],
          modelConfig: {
            tier: template.tier,
            preferredProvider: 'anthropic',
            preferredModel: 'claude-3-sonnet',
            fallbackProviders: [],
          },
          scalingConfig: {
            minInstances: 1,
            maxInstances: 3,
            targetQueueDepth: 3,
            scaleUpCooldown: 30,
            scaleDownDelay: 120,
            scaleToZero: false,
          },
          autonomyLevel: 'balanced',
          systemPromptTemplate: template.systemPromptTemplate,
          position: finalPosition,
        };

        addRole(newRole);
        await new Promise(resolve => setTimeout(resolve, 50));
      }
      
      isProcessing = false;
    };
    
    const handleTestAddRole = (event: Event) => {
      const customEvent = event as CustomEvent<{ templateId: string; position?: { x: number; y: number } }>;
      pendingAdditions.push(customEvent.detail);
      processPendingAdditions();
    };

    window.addEventListener('test:addRole', handleTestAddRole as EventListener);
    return () => {
      window.removeEventListener('test:addRole', handleTestAddRole as EventListener);
    };
  }, [addRole]);
}

export function useWindowExposure(getNodes: () => any) {
  useEffect(() => {
    if (typeof window !== 'undefined') {
      (window as any).__REACT_FLOW_INSTANCE__ = { getNodes };
      const storeWrapper = {
        getState: () => {
          try {
            return useOrchestrationStore.getState();
          } catch (e) {
            console.error('Error getting store state:', e);
            return null;
          }
        },
      };
      (window as any).__ZUSTAND_STORE__ = storeWrapper;
      (window as any).__ORCHESTRATION_STORE__ = useOrchestrationStore;
      (window as any).__GET_ROLE_POSITIONS__ = () => {
        try {
          const state = useOrchestrationStore.getState();
          if (state && state.roles) {
            return state.roles.map((role) => ({
              id: role.id,
              x: role.position?.x || 0,
              y: role.position?.y || 0,
            }));
          }
          return [];
        } catch (e) {
          console.error('Error getting role positions:', e);
          return [];
        }
      };
    }
  }, [getNodes]);
}

