// src/components/Canvas/OrchestrationCanvas.tsx

import { useCallback, useRef, useMemo, useEffect } from 'react';
import ReactFlow, {
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  ReactFlowProvider,
} from 'reactflow';
import type { Connection } from 'reactflow';
import 'reactflow/dist/style.css';

import { useOrchestrationStore } from '../../stores/orchestrationStore';
import RoleCard from '../Nodes/RoleCard';
import RelationshipEdge from '../Edges/RelationshipEdge';
import type { RoleNodeData, RelationshipEdgeData, Relationship, RoleDefinition } from '../../types/workflow';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import { nanoid } from 'nanoid';
import { useReactFlow } from 'reactflow';

const nodeTypes = {
  role: RoleCard,
};

const edgeTypes = {
  relationship: RelationshipEdge,
};

function Flow() {
  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  const { project } = useReactFlow();

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
  useEffect(() => {
    const handleTestAddRole = (event: Event) => {
      const customEvent = event as CustomEvent<{ templateId: string; position?: { x: number; y: number } }>;
      const { templateId } = customEvent.detail;
      const template = ROLE_TEMPLATES[templateId];
      if (!template) return;

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
      };

      addRole(newRole);
    };

    window.addEventListener('test:addRole', handleTestAddRole as EventListener);
    return () => {
      window.removeEventListener('test:addRole', handleTestAddRole as EventListener);
    };
  }, [addRole]);

  // Convert roles to ReactFlow nodes
  const nodes = useMemo(() => {
    return roles.map((role) => {
      const roleInstances = getInstancesForRole(role.id);
      return {
        id: role.id,
        type: 'role',
        position: { x: 0, y: 0 }, // Will be set by layout or drag
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
  }, [roles, instances, expandedRoles, selectedRoleId, getInstancesForRole, toggleRoleExpand, setSelectedRole, removeRole, spawnInstance, terminateInstance]);

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
        // Note: We don't store position in RoleDefinition, so we'd need to add that
        // For now, positions are managed by ReactFlow
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
      project({
        x: event.clientX - bounds.left,
        y: event.clientY - bounds.top,
      });

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
      };

      addRole(newRole);
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
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        defaultEdgeOptions={{
          type: 'relationship',
        }}
        fitView
        snapToGrid
        snapGrid={[16, 16]}
        minZoom={0.1}
        maxZoom={2}
        deleteKeyCode={['Backspace', 'Delete']}
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
