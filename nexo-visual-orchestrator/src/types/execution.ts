export type ExecutionStatus =
  | 'idle'
  | 'pending'
  | 'running'
  | 'completed'
  | 'failed'
  | 'cancelled'
  | 'conflict';

export type WorkflowExecutionStatus =
  | 'idle'
  | 'running'
  | 'paused'
  | 'completed'
  | 'failed';

export interface LogEntry {
  id: string;
  timestamp: number;
  level: 'debug' | 'info' | 'warn' | 'error';
  agentId?: string;
  message: string;
  data?: unknown;
}

export interface ConflictInfo {
  id: string;
  type: 'schema' | 'resource' | 'constraint' | 'philosophy';
  agentIds: string[];
  description: string;
  suggestedResolutions: Resolution[];
  status: 'pending' | 'resolved' | 'escalated';
}

export interface Resolution {
  id: string;
  description: string;
  confidence: number;
  changes?: Record<string, unknown>;
}

export interface ExecutionSummary {
  executionId: string;
  totalDuration: number;
  agentsExecuted: number;
  agentsFailed: number;
  conflictsResolved: number;
  outputs: Record<string, unknown>;
}

