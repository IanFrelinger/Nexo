import type { Node, Edge } from 'reactflow';
import type { AgentType } from './agents';
import type { ExecutionStatus } from './execution';

export interface AgentNodeData {
  agentType: AgentType;
  label: string;
  config: Record<string, unknown>;
  status: ExecutionStatus;
  progress?: number;
  output?: unknown;
  error?: string;
  startTime?: number;
  endTime?: number;
}

export type AgentNode = Node<AgentNodeData>;

export interface DependencyEdgeData {
  sourcePort: string;
  targetPort: string;
  animated?: boolean;
  hasConflict?: boolean;
}

export type DependencyEdge = Edge<DependencyEdgeData>;

export interface Workflow {
  id: string;
  name: string;
  description?: string;
  nodes: AgentNode[];
  edges: DependencyEdge[];
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowValidationError {
  nodeId?: string;
  edgeId?: string;
  code: string;
  message: string;
  severity: 'error' | 'warning';
}

