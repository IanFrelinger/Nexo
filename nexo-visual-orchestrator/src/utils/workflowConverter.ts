import type { Node, Edge } from 'reactflow';
import type { WorkflowDefinition, WorkflowNode } from '../types/workflowComposer';

export function workflowToNodes(workflow: WorkflowDefinition): Node[] {
  return workflow.nodes.map(node => ({
    id: node.id,
    type: getNodeType(node),
    position: { x: node.position.x, y: node.position.y },
    data: {
      ...node,
      inputs: node.inputs,
      outputs: node.outputs,
    },
  }));
}

export function workflowToEdges(workflow: WorkflowDefinition): Edge[] {
  return workflow.connections.map(conn => ({
    id: conn.id,
    source: conn.fromNodeId,
    target: conn.toNodeId,
    sourceHandle: conn.fromPortId,
    targetHandle: conn.toPortId,
    type: 'animated',
  }));
}

export function nodesToWorkflow(nodes: Node[], edges: Edge[]): WorkflowDefinition {
  return {
    id: crypto.randomUUID(),
    name: 'Workflow',
    nodes: nodes.map(node => nodeToWorkflowNode(node)),
    connections: edges.map(edge => ({
      id: edge.id || `${edge.source}-${edge.target}`,
      fromNodeId: edge.source,
      fromPortId: edge.sourceHandle || '',
      toNodeId: edge.target,
      toPortId: edge.targetHandle || '',
      type: 'Data',
    })),
    defaultMode: 'Auto',
    metadata: { zoom: 1.0, viewportCenter: { x: 0, y: 0 }, customData: {} },
    createdAt: new Date().toISOString(),
    modifiedAt: new Date().toISOString(),
  };
}

function getNodeType(node: WorkflowNode): string {
  if ('agentId' in node) return 'agent';
  if ('brickId' in node) return 'brick';
  if ('clusterId' in node) return 'cluster';
  if ('type' in node && node.type === 'File') return 'input';
  if ('type' in node && node.type === 'Display') return 'output';
  if ('operation' in node) return 'transform';
  if ('condition' in node) return 'conditional';
  return 'default';
}

function nodeToWorkflowNode(node: Node): WorkflowNode {
  const data = node.data;
  
  if (data.agentId) {
    return {
      id: node.id,
      name: data.name || node.id,
      position: node.position,
      inputs: data.inputs || [],
      outputs: data.outputs || [],
      agentId: data.agentId,
      mode: data.mode || 'Auto',
      behaviorOverrides: data.behaviorOverrides || {},
      brickOverrides: data.brickOverrides || {},
      parameters: data.parameters || {},
    };
  }
  
  if (data.brickId) {
    return {
      id: node.id,
      name: data.name || node.id,
      position: node.position,
      inputs: data.inputs || [],
      outputs: data.outputs || [],
      brickId: data.brickId,
      implementation: data.implementation || 'Auto',
      providerOverride: data.providerOverride,
      parameters: data.parameters || {},
    };
  }
  
  // Default fallback
  return {
    id: node.id,
    name: data.name || node.id,
    position: node.position,
    inputs: data.inputs || [],
    outputs: data.outputs || [],
    agentId: '',
    mode: 'Auto',
    behaviorOverrides: {},
    brickOverrides: {},
    parameters: {},
  };
}
