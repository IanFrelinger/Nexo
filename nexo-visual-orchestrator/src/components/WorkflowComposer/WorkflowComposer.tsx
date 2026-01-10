import React, { useState, useCallback } from 'react';
import ReactFlow, {
  addEdge,
  useNodesState,
  useEdgesState,
  Controls,
  Background,
  MiniMap,
  ReactFlowProvider,
} from 'reactflow';
import type { Node, Edge, Connection, NodeTypes, EdgeTypes } from 'reactflow';
import 'reactflow/dist/style.css';

import { LibraryPanel } from './LibraryPanel';
import { InspectorPanel } from './InspectorPanel';
import { ExecutionPanel } from './ExecutionPanel';
import { AgentNode } from './nodes/AgentNode';
import { BrickNode } from './nodes/BrickNode';
import { ClusterNode } from './nodes/ClusterNode';
import { InputNode } from './nodes/InputNode';
import { OutputNode } from './nodes/OutputNode';
import { TransformNode } from './nodes/TransformNode';
import { ConditionalNode } from './nodes/ConditionalNode';
import { AnimatedEdge } from './edges/AnimatedEdge';
import { useWorkflowExecution } from '../../hooks/useWorkflowExecution';
import { workflowToNodes, workflowToEdges } from '../../utils/workflowConverter';
import type { WorkflowDefinition } from '../../types/workflowComposer';

const nodeTypes: NodeTypes = {
  agent: AgentNode,
  brick: BrickNode,
  cluster: ClusterNode,
  input: InputNode,
  output: OutputNode,
  transform: TransformNode,
  conditional: ConditionalNode,
};

const edgeTypes: EdgeTypes = {
  animated: AnimatedEdge,
};

interface WorkflowComposerProps {
  initialWorkflow?: WorkflowDefinition;
  onWorkflowChange?: (workflow: WorkflowDefinition) => void;
}

export const WorkflowComposer: React.FC<WorkflowComposerProps> = ({
  initialWorkflow,
  onWorkflowChange,
}) => {
  const [workflow, setWorkflow] = useState<WorkflowDefinition>(
    initialWorkflow || {
      id: crypto.randomUUID(),
      name: 'Untitled Workflow',
      nodes: [],
      connections: [],
      defaultMode: 'Auto',
      metadata: { zoom: 1.0, viewportCenter: { x: 0, y: 0 }, customData: {} },
      createdAt: new Date().toISOString(),
      modifiedAt: new Date().toISOString(),
    }
  );
  
  const [nodes, setNodes, onNodesChange] = useNodesState(workflowToNodes(workflow));
  const [edges, setEdges, onEdgesChange] = useEdgesState(workflowToEdges(workflow));
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [showLibrary, setShowLibrary] = useState(true);
  const [showInspector, setShowInspector] = useState(true);
  const [showExecution, setShowExecution] = useState(false);
  
  const { execute, executionState } = useWorkflowExecution();

  const onConnect = useCallback(
    (connection: Connection) => {
      if (!isValidConnection(connection, nodes)) {
        return;
      }
      
      setEdges((eds) => addEdge({
        ...connection,
        type: 'animated',
        animated: false,
      }, eds));
      
      const newConnection = {
        id: `${connection.source}-${connection.target}`,
        fromNodeId: connection.source!,
        fromPortId: connection.sourceHandle!,
        toNodeId: connection.target!,
        toPortId: connection.targetHandle!,
        type: 'Data' as const,
      };
      
      const updatedWorkflow = {
        ...workflow,
        connections: [...workflow.connections, newConnection],
      };
      
      setWorkflow(updatedWorkflow);
      onWorkflowChange?.(updatedWorkflow);
    },
    [nodes, workflow, onWorkflowChange]
  );

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();
      
      const type = event.dataTransfer.getData('application/nexo-node-type');
      const itemId = event.dataTransfer.getData('application/nexo-item-id');
      
      if (!type) return;
      
      const reactFlowBounds = (event.target as Element).closest('.react-flow')?.getBoundingClientRect();
      const position = {
        x: event.clientX - (reactFlowBounds?.left || 0) - 200,
        y: event.clientY - (reactFlowBounds?.top || 0) - 100,
      };
      
      const newNode = createNode(type, itemId, position);
      
      setNodes((nds) => [...nds, newNode]);
      
      const workflowNode = nodeToWorkflowNode(newNode);
      const updatedWorkflow = {
        ...workflow,
        nodes: [...workflow.nodes, workflowNode],
      };
      
      setWorkflow(updatedWorkflow);
      onWorkflowChange?.(updatedWorkflow);
    },
    [workflow, onWorkflowChange]
  );

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  const handleRunWorkflow = useCallback(async () => {
    setShowExecution(true);
    await execute(workflow);
  }, [workflow, execute]);

  // Apply execution state to nodes/edges
  const styledNodes = applyExecutionState(nodes, executionState);
  const styledEdges = applyExecutionStateToEdges(edges, executionState);

  return (
    <div className="workflow-composer flex h-screen bg-slate-900 text-white">
      {showLibrary && (
        <LibraryPanel
          onDragStart={(_type, _itemId) => {
            // Drag started
          }}
        />
      )}
      
      <div className="flex-1 flex flex-col">
        <div className="toolbar bg-slate-800 border-b border-slate-700 p-2 flex items-center gap-2">
          <button
            onClick={() => setShowLibrary(!showLibrary)}
            className="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded text-sm"
          >
            {showLibrary ? '◀' : '▶'} Library
          </button>
          <button
            onClick={() => setShowInspector(!showInspector)}
            className="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded text-sm"
          >
            {showInspector ? '◀' : '▶'} Inspector
          </button>
          <div className="flex-1" />
          <button
            onClick={handleRunWorkflow}
            className="px-4 py-2 bg-green-600 hover:bg-green-700 rounded font-medium"
            disabled={executionState?.status === 'running'}
          >
            ▶ Run Workflow
          </button>
          <button className="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded text-sm">
            💾 Save
          </button>
          <button className="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded text-sm">
            📤 Export
          </button>
        </div>
        
        <div className="flex-1 relative" onDrop={onDrop} onDragOver={onDragOver}>
          <ReactFlowProvider>
            <ReactFlow
              nodes={styledNodes}
              edges={styledEdges}
              onNodesChange={onNodesChange}
              onEdgesChange={onEdgesChange}
              onConnect={onConnect}
              onNodeClick={(_, node) => setSelectedNode(node.id)}
              nodeTypes={nodeTypes}
              edgeTypes={edgeTypes}
              fitView
            >
              <Controls />
              <MiniMap />
              <Background />
            </ReactFlow>
          </ReactFlowProvider>
        </div>
      </div>
      
      {showInspector && selectedNode && (
        <InspectorPanel
          node={nodes.find(n => n.id === selectedNode)!}
          workflow={workflow}
          onUpdate={(updated) => {
            // Update node in workflow
            const updatedNodes = workflow.nodes.map(n => 
              n.id === updated.id ? updated : n
            );
            const updatedWorkflow = { ...workflow, nodes: updatedNodes };
            setWorkflow(updatedWorkflow);
            onWorkflowChange?.(updatedWorkflow);
          }}
          executionState={executionState?.nodeStates[selectedNode]}
        />
      )}
      
      {showExecution && executionState && (
        <ExecutionPanel
          executionState={executionState}
          onClose={() => setShowExecution(false)}
        />
      )}
    </div>
  );
};

// Helper functions

function isValidConnection(connection: Connection, nodes: Node[]): boolean {
  const fromNode = nodes.find(n => n.id === connection.source);
  const toNode = nodes.find(n => n.id === connection.target);
  
  if (!fromNode || !toNode) return false;
  
  // TODO: Add type compatibility checking
  return true;
}

function createNode(type: string, itemId: string, position: { x: number; y: number }): Node {
  const id = crypto.randomUUID();
  
  switch (type) {
    case 'agent':
      return {
        id,
        type: 'agent',
        position,
        data: { agentId: itemId },
      };
    case 'brick':
      return {
        id,
        type: 'brick',
        position,
        data: { brickId: itemId },
      };
    case 'cluster':
      return {
        id,
        type: 'cluster',
        position,
        data: { clusterId: itemId },
      };
    case 'input':
      return {
        id,
        type: 'input',
        position,
        data: { type: 'File' },
      };
    case 'output':
      return {
        id,
        type: 'output',
        position,
        data: { type: 'Display', format: 'Json' },
      };
    default:
      throw new Error(`Unknown node type: ${type}`);
  }
}

function nodeToWorkflowNode(node: Node): any {
  // Convert React Flow node to WorkflowNode
  // Implementation depends on node type
  return {
    id: node.id,
    name: node.data.name || node.id,
    position: node.position,
    // ... map other properties
  };
}

function applyExecutionState(
  nodes: Node[],
  state?: any
): Node[] {
  if (!state) return nodes;
  
  return nodes.map(node => {
    const nodeState = state.nodeStates?.[node.id];
    if (!nodeState) return node;
    
    return {
      ...node,
      className: `
        ${node.className || ''}
        ${nodeState.status === 'running' ? 'node-running' : ''}
        ${nodeState.status === 'completed' ? 'node-completed' : ''}
        ${nodeState.status === 'failed' ? 'node-failed' : ''}
        ${nodeState.status === 'waiting' ? 'node-waiting' : ''}
      `,
      data: {
        ...node.data,
        executionState: nodeState,
      },
    };
  });
}

function applyExecutionStateToEdges(edges: Edge[], state?: any): Edge[] {
  if (!state) return edges;
  
  return edges.map(edge => {
    const isExecuting = state.activeConnections?.some(
      (c: any) => c.from === edge.source && c.to === edge.target
    );
    
    return {
      ...edge,
      className: isExecuting ? 'edge-executing' : '',
      animated: isExecuting,
    };
  });
}
