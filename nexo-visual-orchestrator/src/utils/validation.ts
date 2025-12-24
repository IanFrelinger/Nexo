import type { Workflow, WorkflowValidationError } from '../types/workflow';
import { AGENT_REGISTRY } from './agentRegistry';

export function validateWorkflow(workflow: Workflow): WorkflowValidationError[] {
  const errors: WorkflowValidationError[] = [];

  // Check for architect node
  const hasArchitect = workflow.nodes.some((n) => n.data.agentType === 'architect');
  if (!hasArchitect) {
    errors.push({
      code: 'MISSING_ARCHITECT',
      message: 'Workflow must have an Architect node as entry point',
      severity: 'error',
    });
  }

  // Check for cycles
  if (hasCycle(workflow)) {
    errors.push({
      code: 'CYCLE_DETECTED',
      message: 'Workflow contains a cycle, which is not allowed',
      severity: 'error',
    });
  }

  // Check required inputs are connected
  workflow.nodes.forEach((node) => {
    const agentDef = AGENT_REGISTRY[node.data.agentType];
    if (!agentDef) return;

    const incomingEdges = workflow.edges.filter((e) => e.target === node.id);
    const connectedPorts = new Set(incomingEdges.map((e) => e.data?.targetPort));

    agentDef.inputs
      .filter((input) => input.required)
      .forEach((input) => {
        if (!connectedPorts.has(input.id)) {
          errors.push({
            nodeId: node.id,
            code: 'MISSING_REQUIRED_INPUT',
            message: `"${node.data.label}" is missing required input "${input.label}"`,
            severity: 'error',
          });
        }
      });
  });

  // Check for disconnected nodes (except architect)
  workflow.nodes.forEach((node) => {
    if (node.data.agentType === 'architect') return;

    const hasIncoming = workflow.edges.some((e) => e.target === node.id);
    const hasOutgoing = workflow.edges.some((e) => e.source === node.id);

    if (!hasIncoming && !hasOutgoing) {
      errors.push({
        nodeId: node.id,
        code: 'DISCONNECTED_NODE',
        message: `"${node.data.label}" is not connected to the workflow`,
        severity: 'warning',
      });
    }
  });

  return errors;
}

function hasCycle(workflow: Workflow): boolean {
  const visited = new Set<string>();
  const recursionStack = new Set<string>();

  function dfs(nodeId: string): boolean {
    visited.add(nodeId);
    recursionStack.add(nodeId);

    const outgoing = workflow.edges.filter((e) => e.source === nodeId);
    for (const edge of outgoing) {
      if (!visited.has(edge.target)) {
        if (dfs(edge.target)) return true;
      } else if (recursionStack.has(edge.target)) {
        return true;
      }
    }

    recursionStack.delete(nodeId);
    return false;
  }

  for (const node of workflow.nodes) {
    if (!visited.has(node.id)) {
      if (dfs(node.id)) return true;
    }
  }

  return false;
}

export function topologicalSort(workflow: Workflow): string[] {
  const inDegree = new Map<string, number>();
  const adjacency = new Map<string, string[]>();

  workflow.nodes.forEach((node) => {
    inDegree.set(node.id, 0);
    adjacency.set(node.id, []);
  });

  workflow.edges.forEach((edge) => {
    adjacency.get(edge.source)?.push(edge.target);
    inDegree.set(edge.target, (inDegree.get(edge.target) || 0) + 1);
  });

  const queue: string[] = [];
  inDegree.forEach((degree, nodeId) => {
    if (degree === 0) queue.push(nodeId);
  });

  const result: string[] = [];
  while (queue.length > 0) {
    const nodeId = queue.shift()!;
    result.push(nodeId);

    adjacency.get(nodeId)?.forEach((neighbor) => {
      const newDegree = (inDegree.get(neighbor) || 0) - 1;
      inDegree.set(neighbor, newDegree);
      if (newDegree === 0) queue.push(neighbor);
    });
  }

  return result;
}

