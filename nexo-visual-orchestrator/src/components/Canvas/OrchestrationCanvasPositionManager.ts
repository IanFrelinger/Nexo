// src/components/Canvas/OrchestrationCanvasPositionManager.ts

import { useCallback, useEffect } from 'react';
import type { Node } from 'reactflow';
import { useReactFlow } from 'reactflow';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { constrainToTier } from '../../utils/layoutEngine';
import type { RoleDefinition } from '../../types/workflow';

export function usePositionManager(roles: RoleDefinition[]) {
  const { setNodes, getNodes } = useReactFlow();

  const onInit = useCallback(() => {
    if (setNodes && roles.length > 0) {
      requestAnimationFrame(() => {
        setNodes((nds: Node[]) => {
          return nds.map((node) => {
            const role = roles.find((r) => r.id === node.id);
            if (role && role.position) {
              return {
                ...node,
                position: role.position,
              };
            }
            return node;
          });
        });
      });
    }
  }, [roles, setNodes]);

  useEffect(() => {
    if (setNodes && roles.length > 0) {
      const currentNodes = getNodes();
      const isDragging = currentNodes.some(node => node.dragging === true);
      
      if (!isDragging) {
        requestAnimationFrame(() => {
          setNodes((nds: Node[]) => {
            return nds.map((node) => {
              const role = roles.find((r) => r.id === node.id);
              if (role && role.position) {
                const constrainedY = constrainToTier(role.modelConfig.tier, role.position.y);
                const constrainedPosition = {
                  x: role.position.x,
                  y: constrainedY,
                };
                
                if (constrainedY !== role.position.y) {
                  useOrchestrationStore.getState().updateRole(role.id, {
                    position: constrainedPosition,
                  });
                }
                
                return {
                  ...node,
                  position: constrainedPosition,
                };
              }
              return node;
            });
          });
        });
      }
    }
  }, [roles.map(r => `${r.id}:${r.position?.x || 0},${r.position?.y || 0}`).join('|'), setNodes, getNodes]);

  return { onInit };
}

