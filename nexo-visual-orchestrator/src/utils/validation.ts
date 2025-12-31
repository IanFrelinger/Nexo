/**
 * Validation Utilities
 * 
 * Provides workflow validation functions to ensure workflow integrity:
 * - Architecture validation (presence of architect role)
 * - Cycle detection in delegation relationships
 * - Disconnected role detection
 * - Topological sorting for execution order
 * 
 * Used before workflow execution and for displaying validation warnings
 * in the UI.
 */

import type { Workflow, WorkflowValidationError } from '../types/workflow';

/**
 * Validates a workflow and returns a list of validation errors
 * @param workflow - The workflow to validate
 * @returns Array of validation errors (warnings and errors)
 */
export function validateWorkflow(workflow: Workflow): WorkflowValidationError[] {
  const errors: WorkflowValidationError[] = [];

  // Check for architect role (optional - not all org patterns need it)
  const hasArchitect = workflow.roles.some((r) => r.name === 'Architect' || r.modelConfig.tier === 'strategic');
  if (!hasArchitect && workflow.roles.length > 0) {
    errors.push({
      code: 'MISSING_ARCHITECT',
      message: 'Workflow should have an Architect role as coordinator (optional for federated/swarm patterns)',
      severity: 'warning',
    });
  }

  // Check for cycles in delegation relationships
  if (hasCycle(workflow)) {
    errors.push({
      code: 'CYCLE_DETECTED',
      message: 'Workflow contains a cycle in delegation relationships',
      severity: 'error',
    });
  }

  // Check for disconnected roles
  workflow.roles.forEach((role) => {
    if (role.name === 'Architect') return;

    const hasIncoming = workflow.relationships.some((r) => r.targetRoleId === role.id);
    const hasOutgoing = workflow.relationships.some((r) => r.sourceRoleId === role.id);

    if (!hasIncoming && !hasOutgoing && workflow.roles.length > 1) {
      errors.push({
        roleId: role.id,
        code: 'DISCONNECTED_ROLE',
        message: `"${role.name}" is not connected to the workflow`,
        severity: 'warning',
      });
    }
  });

  return errors;
}

/**
 * Detects cycles in delegation relationships using DFS
 * @param workflow - The workflow to check
 * @returns True if a cycle exists, false otherwise
 */
function hasCycle(workflow: Workflow): boolean {
  const visited = new Set<string>();
  const recursionStack = new Set<string>();

  function dfs(roleId: string): boolean {
    visited.add(roleId);
    recursionStack.add(roleId);

    const outgoing = workflow.relationships
      .filter((r) => r.sourceRoleId === roleId && r.type === 'delegates')
      .map((r) => r.targetRoleId);
    
    for (const targetId of outgoing) {
      if (!visited.has(targetId)) {
        if (dfs(targetId)) return true;
      } else if (recursionStack.has(targetId)) {
        return true;
      }
    }

    recursionStack.delete(roleId);
    return false;
  }

  for (const role of workflow.roles) {
    if (!visited.has(role.id)) {
      if (dfs(role.id)) return true;
    }
  }

  return false;
}

/**
 * Performs topological sort on workflow roles based on delegation relationships
 * Returns roles in execution order (no role executes before its dependencies)
 * @param workflow - The workflow to sort
 * @returns Array of role IDs in topological order
 */
export function topologicalSort(workflow: Workflow): string[] {
  const inDegree = new Map<string, number>();
  const adjacency = new Map<string, string[]>();

  workflow.roles.forEach((role) => {
    inDegree.set(role.id, 0);
    adjacency.set(role.id, []);
  });

  workflow.relationships
    .filter((r) => r.type === 'delegates')
    .forEach((rel) => {
      adjacency.get(rel.sourceRoleId)?.push(rel.targetRoleId);
      inDegree.set(rel.targetRoleId, (inDegree.get(rel.targetRoleId) || 0) + 1);
    });

  const queue: string[] = [];
  inDegree.forEach((degree, roleId) => {
    if (degree === 0) queue.push(roleId);
  });

  const result: string[] = [];
  while (queue.length > 0) {
    const roleId = queue.shift()!;
    result.push(roleId);

    adjacency.get(roleId)?.forEach((neighbor) => {
      const newDegree = (inDegree.get(neighbor) || 0) - 1;
      inDegree.set(neighbor, newDegree);
      if (newDegree === 0) queue.push(neighbor);
    });
  }

  return result;
}
