// src/components/Canvas/OrchestrationCanvasNodeHandlers.ts

import { useCallback } from 'react';
import type { Node } from 'reactflow';
import { useReactFlow } from 'reactflow';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import { constrainToTier, getTierForPosition } from '../../utils/layoutEngine';
import { DEFAULT_MODEL_CONFIGS, DEFAULT_SCALING_CONFIGS } from '../../types/workflow';
import type { RoleDefinition } from '../../types/workflow';

export function useNodeHandlers(
  roles: RoleDefinition[],
  setSelectedRole: (roleId: string | null) => void,
  highlightPathToArchitect: (roleId: string) => void,
  clearHighlightedPath: () => void,
) {
  const { setNodes } = useReactFlow();

  const onNodesChange = useCallback((changes: any) => {
    changes.forEach((change: any) => {
      if (change.type === 'position' && change.position) {
        const role = roles.find(r => r.id === change.id);
        if (role) {
          const detectedTier = getTierForPosition(change.position.y);
          const targetTier = detectedTier || role.modelConfig.tier;
          const constrainedY = constrainToTier(targetTier, change.position.y);
          const constrainedPosition = {
            x: change.position.x,
            y: constrainedY,
          };
          
          if (constrainedY !== change.position.y) {
            change.position = constrainedPosition;
          }
          
          useOrchestrationStore.getState().updateRole(change.id, {
            position: constrainedPosition,
          });
        } else {
          useOrchestrationStore.getState().updateRole(change.id, {
            position: change.position,
          });
        }
      }
      if (change.type === 'select' && change.selected) {
        setSelectedRole(change.id);
        highlightPathToArchitect(change.id);
      }
      if (change.type === 'select' && !change.selected) {
        setSelectedRole(null);
        clearHighlightedPath();
      }
    });
  }, [roles, setSelectedRole, highlightPathToArchitect, clearHighlightedPath]);

  const onNodeDrag = useCallback((_event: any, node: Node) => {
    const role = roles.find(r => r.id === node.id);
    if (role && node.position) {
      const detectedTier = getTierForPosition(node.position.y);
      
      if (detectedTier && detectedTier !== role.modelConfig.tier) {
        const constrainedY = constrainToTier(detectedTier, node.position.y);
        if (constrainedY !== node.position.y && setNodes) {
          setNodes((nds: Node[]) => {
            return nds.map((n) => {
              if (n.id === node.id) {
                return {
                  ...n,
                  position: {
                    x: node.position.x,
                    y: constrainedY,
                  },
                };
              }
              return n;
            });
          });
        }
      } else {
        const constrainedY = constrainToTier(role.modelConfig.tier, node.position.y);
        if (constrainedY !== node.position.y && setNodes) {
          setNodes((nds: Node[]) => {
            return nds.map((n) => {
              if (n.id === node.id) {
                return {
                  ...n,
                  position: {
                    x: node.position.x,
                    y: constrainedY,
                  },
                };
              }
              return n;
            });
          });
        }
      }
    }
  }, [roles, setNodes]);

  const onNodeDragStop = useCallback((_event: any, node: Node) => {
    const role = roles.find(r => r.id === node.id);
    if (role && node.position) {
      const detectedTier = getTierForPosition(node.position.y);
      
      if (detectedTier) {
        const constrainedY = constrainToTier(detectedTier, node.position.y);
        const constrainedPosition = {
          x: node.position.x,
          y: constrainedY,
        };
        
        const updates: any = {
          position: constrainedPosition,
        };
        
        if (detectedTier !== role.modelConfig.tier) {
          const newModelConfig = DEFAULT_MODEL_CONFIGS[detectedTier];
          const newScalingConfig = DEFAULT_SCALING_CONFIGS[detectedTier];
          
          updates.modelConfig = {
            ...newModelConfig,
            preferredProvider: role.modelConfig.preferredProvider,
            preferredModel: role.modelConfig.preferredModel,
            fallbackProviders: role.modelConfig.fallbackProviders,
          };
          updates.scalingConfig = {
            ...newScalingConfig,
            minInstances: Math.max(newScalingConfig.minInstances, role.scalingConfig.minInstances),
            maxInstances: Math.min(newScalingConfig.maxInstances, role.scalingConfig.maxInstances),
          };
        }
        
        useOrchestrationStore.getState().updateRole(node.id, updates);
        
        if (constrainedY !== node.position.y && setNodes) {
          setNodes((nds: Node[]) => {
            return nds.map((n) => {
              if (n.id === node.id) {
                return {
                  ...n,
                  position: constrainedPosition,
                };
              }
              return n;
            });
          });
        }
      } else {
        const constrainedY = constrainToTier(role.modelConfig.tier, node.position.y);
        const constrainedPosition = {
          x: node.position.x,
          y: constrainedY,
        };
        
        useOrchestrationStore.getState().updateRole(node.id, {
          position: constrainedPosition,
        });
      }
    }
  }, [roles, setNodes]);

  return { onNodesChange, onNodeDrag, onNodeDragStop };
}

