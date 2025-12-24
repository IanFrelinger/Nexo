import type { Workflow, WorkflowValidationError } from './workflow';
import type { ConflictInfo, Resolution, ExecutionSummary, LogEntry } from './execution';

// Client -> Server messages
export type ClientMessage =
  | { type: 'execute'; workflow: Workflow }
  | { type: 'pause' }
  | { type: 'resume' }
  | { type: 'cancel' }
  | { type: 'resolve_conflict'; conflictId: string; resolutionId: string }
  | { type: 'validate'; workflow: Workflow };

// Server -> Client messages
export type ServerMessage =
  | { type: 'execution_started'; executionId: string }
  | { type: 'agent_started'; agentId: string; timestamp: number }
  | { type: 'agent_progress'; agentId: string; progress: number; message?: string }
  | { type: 'agent_completed'; agentId: string; output: unknown; duration: number }
  | { type: 'agent_failed'; agentId: string; error: string }
  | { type: 'conflict_detected'; conflict: ConflictInfo }
  | { type: 'conflict_resolved'; conflictId: string; resolution: Resolution }
  | { type: 'execution_completed'; summary: ExecutionSummary }
  | { type: 'execution_failed'; error: string }
  | { type: 'log'; entry: LogEntry }
  | { type: 'validation_result'; valid: boolean; errors: WorkflowValidationError[] };

