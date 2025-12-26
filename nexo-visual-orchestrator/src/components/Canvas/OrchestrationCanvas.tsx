// src/components/Canvas/OrchestrationCanvas.tsx

import { useCallback, useRef, useMemo, useEffect } from 'react';
import ReactFlow, {
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  ReactFlowProvider,
  useReactFlow,
  type Node,
} from 'reactflow';
import type { Connection } from 'reactflow';
import 'reactflow/dist/style.css';

import { useOrchestrationStore } from '../../stores/orchestrationStore';
import RoleCard from '../Nodes/RoleCard';
import RelationshipEdge from '../Edges/RelationshipEdge';
import type { RoleNodeData, RelationshipEdgeData, Relationship, RoleDefinition } from '../../types/workflow';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import { nanoid } from 'nanoid';

const nodeTypes = {
  role: RoleCard,
};

const edgeTypes = {
  relationship: RelationshipEdge,
};

function Flow() {
  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  const { project, setNodes, getNodes } = useReactFlow();
  
  // Expose ReactFlow instance and store to window for testing
  useEffect(() => {
    if (typeof window !== 'undefined') {
      (window as any).__REACT_FLOW_INSTANCE__ = { getNodes };
      // Expose the store with a getState method that the test can call
      // Zustand stores have a getState method directly on the store
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
      // Also expose the store directly in case the test needs it
      (window as any).__ORCHESTRATION_STORE__ = useOrchestrationStore;
      // Expose a simple helper function that returns role positions
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

  const {
    roles,
    instances,
    relationships,
    expandedRoles,
    selectedRoleId,
    selectedRelationshipId,
    setSelectedRole,
    removeRole,
    addRelationship,
    addRole,
    toggleRoleExpand,
    spawnInstance,
    terminateInstance,
    getInstancesForRole,
  } = useOrchestrationStore();

  // Listen for test events to add roles programmatically
  // Use a queue to ensure roles are added sequentially with correct positions
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

        // Get current roles from store - this ensures we see all previously added roles
        // Use a small delay to ensure the previous addRole has updated the store
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
          // Check for overlap with existing roles
          const hasOverlap = existingRoles.some(role => {
            const dx = Math.abs(role.position.x - providedPosition.x);
            const dy = Math.abs(role.position.y - providedPosition.y);
            return dx < (nodeWidth + spacing) && dy < (nodeHeight + spacing);
          });
          
          if (hasOverlap) {
            // Calculate next available position
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
          // Calculate next available position based on current role count
          // This ensures each new role gets a unique position
          const lastRow = Math.floor(existingRoles.length / nodesPerRow);
          const lastCol = existingRoles.length % nodesPerRow;
          finalPosition = {
            x: startX + lastCol * (nodeWidth + spacing),
            y: startY + lastRow * (nodeHeight + spacing),
          };
        }

        // Create role from template
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
        
        // Wait for store to update before processing next role
        await new Promise(resolve => setTimeout(resolve, 50));
      }
      
      isProcessing = false;
    };
    
    const handleTestAddRole = (event: Event) => {
      const customEvent = event as CustomEvent<{ templateId: string; position?: { x: number; y: number } }>;
      pendingAdditions.push(customEvent.detail);
      // Process additions asynchronously to ensure they're handled sequentially
      processPendingAdditions();
    };

    window.addEventListener('test:addRole', handleTestAddRole as EventListener);
    return () => {
      window.removeEventListener('test:addRole', handleTestAddRole as EventListener);
    };
  }, [addRole]);

  // Convert roles to ReactFlow nodes
  // Include position in dependency to ensure nodes update when positions change
  const nodes = useMemo(() => {
    return roles.map((role) => {
      const roleInstances = getInstancesForRole(role.id);
      // Ensure position is always set - use stored position or default
      const position = role.position || { x: 0, y: 0 };
      return {
        id: role.id,
        type: 'role',
        position, // Use stored position - ReactFlow will respect this
        data: {
          role,
          instances: roleInstances,
          isExpanded: expandedRoles.has(role.id),
          onToggleExpand: toggleRoleExpand,
          onEditRole: (id: string) => {
            setSelectedRole(id);
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
      };
    });
  }, [
    roles.map(r => `${r.id}:${r.position?.x || 0},${r.position?.y || 0}`).join('|'), // Include positions in deps
    instances,
    expandedRoles,
    selectedRoleId,
    getInstancesForRole,
    toggleRoleExpand,
    setSelectedRole,
    removeRole,
    spawnInstance,
    terminateInstance,
  ]);

  // Use onInit callback to ensure positions are set correctly after ReactFlow initializes
  const onInit = useCallback(() => {
    // After ReactFlow initializes, ensure all node positions match role positions
    if (setNodes && roles.length > 0) {
      // Use requestAnimationFrame to ensure this runs after ReactFlow has fully initialized
      requestAnimationFrame(() => {
        setNodes((nds: Node[]) => {
          return nds.map((node) => {
            const role = roles.find((r) => r.id === node.id);
            if (role && role.position) {
              // Always update to ensure position matches role
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
  
  // Also update positions when roles change (not just on init)
  // This ensures ReactFlow nodes always reflect the stored role positions
  useEffect(() => {
    if (setNodes && roles.length > 0) {
      // Use requestAnimationFrame to batch updates and avoid conflicts with ReactFlow's internal updates
      requestAnimationFrame(() => {
        setNodes((nds: Node[]) => {
          return nds.map((node) => {
            const role = roles.find((r) => r.id === node.id);
            if (role && role.position) {
              // Always update to ensure position matches role
              // This is important when roles are added or positions change
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
  }, [roles.map(r => `${r.id}:${r.position?.x || 0},${r.position?.y || 0}`).join('|'), setNodes]);

  // Convert relationships to ReactFlow edges
  const edges = useMemo(() => {
    return relationships.map((rel) => ({
      id: rel.id,
      source: rel.sourceRoleId,
      target: rel.targetRoleId,
      type: 'relationship',
      data: {
        relationship: rel,
        isActive: true,
        trafficVolume: 1,
      } as RelationshipEdgeData,
      // Use different handles for negotiation edges
      sourceHandle: rel.type === 'negotiates' ? 'negotiate-out' : undefined,
      targetHandle: rel.type === 'negotiates' ? 'negotiate-in' : undefined,
      selected: rel.id === selectedRelationshipId,
    }));
  }, [relationships, selectedRelationshipId]);

  const onNodesChange = useCallback((changes: any) => {
    // Update role positions when nodes are dragged
    changes.forEach((change: any) => {
      if (change.type === 'position' && change.position) {
        // Update the role's position in the store
        useOrchestrationStore.getState().updateRole(change.id, {
          position: change.position,
        });
      }
      if (change.type === 'select' && change.selected) {
        setSelectedRole(change.id);
      }
      if (change.type === 'select' && !change.selected) {
        setSelectedRole(null);
      }
    });
  }, [setSelectedRole]);

  const onEdgesChange = useCallback((changes: any) => {
    // Handle edge changes if needed
    changes.forEach((change: any) => {
      if (change.type === 'remove') {
        useOrchestrationStore.getState().removeRelationship(change.id);
      }
    });
  }, []);

  const onConnect = useCallback((connection: Connection) => {
    // Determine relationship type based on handles
    const isNegotiation = connection.sourceHandle === 'negotiate-out' || connection.targetHandle === 'negotiate-in';
    const type: Relationship['type'] = isNegotiation ? 'negotiates' : 'delegates';

    const newRelationship: Relationship = {
      id: `rel-${nanoid(6)}`,
      sourceRoleId: connection.source!,
      targetRoleId: connection.target!,
      type,
      metadata: isNegotiation ? { topics: [] } : undefined,
    };

    addRelationship(newRelationship);
  }, [addRelationship]);

  const onPaneClick = useCallback(() => {
    setSelectedRole(null);
  }, [setSelectedRole]);

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

      // Use a small delay to ensure we get the latest state after any previous additions
      // This prevents race conditions when dragging multiple roles quickly
      setTimeout(() => {
        // Get the current state to ensure we have the latest roles
        const existingRoles = useOrchestrationStore.getState().roles;
        const nodeWidth = 288;
        const nodeHeight = 200;
        const spacing = 50;
        const startX = 100;
        const startY = 100;
        const nodesPerRow = 4;
        
        // Check if drop position overlaps with existing nodes
        // Use a more generous overlap check to ensure nodes don't overlap
        const hasOverlap = existingRoles.some(role => {
          if (!role.position) return false;
          const dx = Math.abs(role.position.x - dropPosition.x);
          const dy = Math.abs(role.position.y - dropPosition.y);
          // Check if positions are too close (within node dimensions + spacing)
          return dx < (nodeWidth + spacing) && dy < (nodeHeight + spacing);
        });
        
        let finalPosition = dropPosition;
        if (hasOverlap || dropPosition.x < 0 || dropPosition.y < 0 || isNaN(dropPosition.x) || isNaN(dropPosition.y)) {
          // Calculate next available position in grid layout
          // This ensures each new role gets a unique position
          const lastRow = Math.floor(existingRoles.length / nodesPerRow);
          const lastCol = existingRoles.length % nodesPerRow;
          finalPosition = {
            x: startX + lastCol * (nodeWidth + spacing),
            y: startY + lastRow * (nodeHeight + spacing),
          };
        }

        // Create role from template
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
      }, 10); // Small delay to ensure we get latest state
    },
    [project, addRole]
  );

  return (
    <div 
      ref={reactFlowWrapper} 
      className="flex-1 h-full"
      onDragOver={onDragOver}
      onDrop={onDrop}
    >
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onPaneClick={onPaneClick}
        onInit={onInit}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        defaultEdgeOptions={{
          type: 'relationship',
        }}
        fitView={false} // Disable fitView to preserve node positions
        snapToGrid
        snapGrid={[16, 16]}
        minZoom={0.1}
        maxZoom={2}
        deleteKeyCode={['Backspace', 'Delete']}
        nodesDraggable={true}
        nodesConnectable={false}
      >
        <Background variant={BackgroundVariant.Dots} gap={16} size={1} color="#334155" />
        <Controls className="!bg-surface !border-slate-700" />
        <MiniMap
          nodeColor={(node) => {
            const roleData = (node.data as RoleNodeData)?.role;
            if (!roleData) return '#64748B';
            const colorMap: Record<string, string> = {
              purple: '#A855F7',
              red: '#EF4444',
              yellow: '#EAB308',
              cyan: '#06B6D4',
              green: '#22C55E',
              indigo: '#6366F1',
              pink: '#EC4899',
              orange: '#F97316',
              slate: '#64748B',
              teal: '#14B8A6',
              amber: '#F59E0B',
              emerald: '#10B981',
            };
            return colorMap[roleData.color] || '#64748B';
          }}
          maskColor="rgba(15, 23, 42, 0.8)"
        />
      </ReactFlow>
    </div>
  );
}

export default function OrchestrationCanvas() {
  return (
    <ReactFlowProvider>
      <Flow />
    </ReactFlowProvider>
  );
}
