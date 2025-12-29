// src/components/Canvas/OrchestrationCanvasDropHandler.ts

import { useCallback } from 'react';
import { useReactFlow } from 'reactflow';
import { nanoid } from 'nanoid';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import type { RoleDefinition } from '../../types/workflow';

export function useDropHandler(
  reactFlowWrapper: React.RefObject<HTMLDivElement>,
  addRole: (role: RoleDefinition) => void,
) {
  const { project } = useReactFlow();

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }, []);

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();
      event.stopPropagation();

      const roleTemplateId = event.dataTransfer.getData('application/reactflow');
      if (!roleTemplateId || !reactFlowWrapper.current) return;

      const template = ROLE_TEMPLATES[roleTemplateId];
      if (!template) return;

      const bounds = reactFlowWrapper.current.getBoundingClientRect();
      const dropPosition = project({
        x: event.clientX - bounds.left,
        y: event.clientY - bounds.top,
      });

      setTimeout(() => {
        const existingRoles = useOrchestrationStore.getState().roles;
        const nodeWidth = 288;
        const nodeHeight = 200;
        const spacing = 50;
        const startX = 100;
        const startY = 100;
        const nodesPerRow = 4;
        
        const hasOverlap = existingRoles.some(role => {
          if (!role.position) return false;
          const dx = Math.abs(role.position.x - dropPosition.x);
          const dy = Math.abs(role.position.y - dropPosition.y);
          return dx < (nodeWidth + spacing) && dy < (nodeHeight + spacing);
        });
        
        let finalPosition = dropPosition;
        if (hasOverlap || dropPosition.x < 0 || dropPosition.y < 0 || isNaN(dropPosition.x) || isNaN(dropPosition.y)) {
          const lastRow = Math.floor(existingRoles.length / nodesPerRow);
          const lastCol = existingRoles.length % nodesPerRow;
          finalPosition = {
            x: startX + lastCol * (nodeWidth + spacing),
            y: startY + lastRow * (nodeHeight + spacing),
          };
        }

        const newRole: RoleDefinition = {
          id: `${roleTemplateId}-${nanoid(6)}`,
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
            preferredModel: template.tier === 'strategic' ? 'claude-3-opus' : template.tier === 'tactical' ? 'claude-3-sonnet' : 'llama3',
            fallbackProviders: [],
          },
          scalingConfig: {
            minInstances: template.tier === 'strategic' ? 1 : template.tier === 'tactical' ? 1 : 0,
            maxInstances: template.tier === 'strategic' ? 1 : template.tier === 'tactical' ? 3 : 10,
            targetQueueDepth: template.tier === 'strategic' ? 5 : template.tier === 'tactical' ? 3 : 2,
            scaleUpCooldown: template.tier === 'strategic' ? 60 : template.tier === 'tactical' ? 30 : 10,
            scaleDownDelay: template.tier === 'strategic' ? 300 : template.tier === 'tactical' ? 120 : 60,
            scaleToZero: template.tier === 'execution',
          },
          autonomyLevel: 'balanced',
          systemPromptTemplate: template.systemPromptTemplate,
          position: finalPosition,
        };

        addRole(newRole);
      }, 10);
    },
    [project, addRole, reactFlowWrapper]
  );

  return { onDragOver, onDrop };
}

