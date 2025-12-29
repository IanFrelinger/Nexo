// src/components/Canvas/OrchestrationCanvasNodes.ts

import { useMemo } from 'react';
import type { Node } from 'reactflow';
import type { RoleDefinition, Relationship } from '../../types/workflow';
import type { RoleNodeData } from '../../types/workflow';

export function useOrchestrationNodes(
  roles: RoleDefinition[],
  instances: any[],
  expandedRoles: Set<string>,
  collapsedTiers: Set<string>,
  highlightedPath: string[] | null,
  selectedRoleId: string | null,
  getInstancesForRole: (roleId: string) => any[],
  toggleRoleExpand: (roleId: string) => void,
  setSelectedRole: (roleId: string | null) => void,
  removeRole: (roleId: string) => void,
  spawnInstance: (roleId: string) => void,
  terminateInstance: (instanceId: string) => void,
  highlightPathToArchitect: (roleId: string) => void,
): Node<RoleNodeData>[] {
  return useMemo(() => {
    return roles
      .filter(role => {
        const tier = role.modelConfig.tier;
        return !collapsedTiers.has(tier);
      })
      .map((role) => {
        const roleInstances = getInstancesForRole(role.id);
        const position = role.position || { x: 0, y: 0 };
        const isInPath = highlightedPath?.includes(role.id) || false;
        
        return {
          id: role.id,
          type: 'role',
          position,
          data: {
            role,
            instances: roleInstances,
            isExpanded: expandedRoles.has(role.id),
            isHighlighted: isInPath,
            onToggleExpand: toggleRoleExpand,
            onEditRole: (id: string) => {
              setSelectedRole(id);
              highlightPathToArchitect(id);
            },
            onDeleteRole: (id: string) => {
              removeRole(id);
            },
            onSpawnInstance: (roleId: string) => {
              try {
                spawnInstance(roleId);
              } catch (e) {
                console.error('Failed to spawn instance:', e);
              }
            },
            onTerminateInstance: (instanceId: string) => {
              terminateInstance(instanceId);
            },
          } as RoleNodeData,
          selected: role.id === selectedRoleId,
          style: isInPath ? {
            border: '3px solid #fbbf24',
            borderRadius: '8px',
          } : undefined,
        };
      });
  }, [
    roles.map(r => `${r.id}:${r.position?.x || 0},${r.position?.y || 0}`).join('|'),
    instances,
    expandedRoles,
    collapsedTiers,
    highlightedPath,
    selectedRoleId,
    getInstancesForRole,
    toggleRoleExpand,
    setSelectedRole,
    removeRole,
    spawnInstance,
    terminateInstance,
    highlightPathToArchitect,
  ]);
}

