// Type definitions for the workflow composer

export interface WorkflowDefinition {
  id: string;
  name: string;
  description?: string;
  nodes: WorkflowNode[];
  connections: VisualWorkflowConnection[];
  defaultMode: ImplementationMode;
  metadata: WorkflowMetadata;
  createdAt: string;
  modifiedAt: string;
}

export type WorkflowNode =
  | AgentNode
  | BrickNode
  | ClusterNode
  | InputNode
  | OutputNode
  | TransformNode
  | ConditionalNode;

export interface AgentNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  agentId: string;
  mode: ImplementationMode;
  behaviorOverrides: Record<string, ImplementationMode>;
  brickOverrides: Record<string, ImplementationType>;
  parameters: Record<string, any>;
}

export interface BrickNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  brickId: string;
  implementation: ImplementationType;
  providerOverride?: string;
  parameters: Record<string, any>;
}

export interface ClusterNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  clusterId: string;
  mode: ImplementationMode;
  parameters: Record<string, any>;
}

export interface InputNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  type: InputType;
  filePath?: string;
  content?: string;
  webhookUrl?: string;
}

export interface OutputNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  type: OutputType;
  format: OutputFormat;
  filePath?: string;
  webhookUrl?: string;
}

export interface TransformNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  operation: TransformOperation;
  expression: string;
}

export interface ConditionalNode {
  id: string;
  name: string;
  position: Position;
  inputs: NodePort[];
  outputs: NodePort[];
  condition: string;
}

export interface NodePort {
  id: string;
  name: string;
  direction: PortDirection;
  dataType: string;
  required: boolean;
  allowMultiple: boolean;
}

export interface VisualWorkflowConnection {
  id: string;
  fromNodeId: string;
  fromPortId: string;
  toNodeId: string;
  toPortId: string;
  type: ConnectionType;
}

export interface Position {
  x: number;
  y: number;
}

export interface WorkflowMetadata {
  zoom: number;
  viewportCenter: Position;
  customData: Record<string, any>;
}

export type ImplementationMode = 'Auto' | 'DeterministicOnly' | 'AgenticPreferred' | 'Mixed';
export type ImplementationType = 'Deterministic' | 'Agentic' | 'Auto';

export type InputType = 'File' | 'Content' | 'Webhook' | 'Database';
export type OutputType = 'Display' | 'File' | 'Webhook' | 'Database';
export type OutputFormat = 'Json' | 'Xml' | 'Csv' | 'Markdown' | 'Html' | 'Pdf';
export type TransformOperation = 'Map' | 'Filter' | 'Reduce' | 'Sort' | 'GroupBy' | 'Merge';
export type PortDirection = 'Input' | 'Output';
export type ConnectionType = 'Data' | 'Control' | 'Event';

// Execution state types

export interface WorkflowExecutionState {
  status: 'idle' | 'running' | 'completed' | 'failed';
  correlationId?: string;
  nodeStates: Record<string, NodeExecutionState>;
  activeConnections: Array<{ from: string; to: string }>;
  metrics: WorkflowMetrics;
}

export interface NodeExecutionState {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  progress?: number;
  currentBrick?: string;
  currentImplementation?: ImplementationType;
  brickStates?: Record<string, BrickExecutionState>;
  error?: string;
}

export interface BrickExecutionState {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  implementation: ImplementationType;
  duration?: number;
  tokensUsed?: number;
  cacheHit?: boolean;
}

export interface WorkflowMetrics {
  totalDuration: number;
  nodesExecuted: number;
  deterministicBricks: number;
  agenticBricks: number;
  totalTokensUsed: number;
  cacheHits: number;
}
