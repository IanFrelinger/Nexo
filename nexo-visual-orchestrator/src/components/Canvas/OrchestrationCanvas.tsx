// src/components/Canvas/OrchestrationCanvas.tsx

import { useRef, useCallback } from 'react';
import ReactFlow, {
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  ReactFlowProvider,
} from 'reactflow';
import 'reactflow/dist/style.css';

import { useOrchestrationStore } from '../../stores/orchestrationStore';
import RoleCard from '../Nodes/RoleCard';
import RelationshipEdge from '../Edges/RelationshipEdge';
import TierBands from './TierBands';
import type { RoleNodeData } from '../../types/workflow';
import { useTestRoleAddition, useWindowExposure } from './OrchestrationCanvasHooks';
import { useOrchestrationNodes } from './OrchestrationCanvasNodes';
import { useOrchestrationEdges } from './OrchestrationCanvasEdges';
import { useNodeHandlers } from './OrchestrationCanvasNodeHandlers';
import { useEdgeHandlers } from './OrchestrationCanvasEdgeHandlers';
import { useDropHandler } from './OrchestrationCanvasDropHandler';
import { usePositionManager } from './OrchestrationCanvasPositionManager';

const nodeTypes = {
  role: RoleCard,
};

const edgeTypes = {
  relationship: RelationshipEdge,
};

function Flow() {
  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  
  const {
    roles,
    instances,
    relationships,
    expandedRoles,
    collapsedTiers,
    visibleRelationshipTypes,
    highlightedPath,
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
    highlightPathToArchitect,
    clearHighlightedPath,
  } = useOrchestrationStore();

  useTestRoleAddition(addRole);
  useWindowExposure();

  const nodes = useOrchestrationNodes(
    roles,
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
  );

  const edges = useOrchestrationEdges(
    relationships,
    selectedRelationshipId,
    visibleRelationshipTypes,
    highlightedPath,
  );

  const { onInit } = usePositionManager(roles);
  const { onNodesChange, onNodeDrag, onNodeDragStop } = useNodeHandlers(
    roles,
    setSelectedRole,
    highlightPathToArchitect,
    clearHighlightedPath,
  );
  const { onEdgesChange, onConnect } = useEdgeHandlers(addRelationship);
  const { onDragOver, onDrop } = useDropHandler(reactFlowWrapper as React.RefObject<HTMLDivElement>, addRole);

  const onPaneClick = useCallback(() => {
    setSelectedRole(null);
    clearHighlightedPath();
  }, [setSelectedRole, clearHighlightedPath]);

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
        onNodeDrag={onNodeDrag}
        onNodeDragStop={onNodeDragStop}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onPaneClick={onPaneClick}
        onInit={onInit}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        defaultEdgeOptions={{
          type: 'relationship',
        }}
        fitView={false}
        snapToGrid
        snapGrid={[16, 16]}
        minZoom={0.1}
        maxZoom={2}
        deleteKeyCode={['Backspace', 'Delete']}
        nodesDraggable={true}
        nodesConnectable={false}
      >
        <Background variant={BackgroundVariant.Dots} gap={16} size={1} color="#334155" />
        <TierBands roles={roles} collapsedTiers={collapsedTiers} />
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
