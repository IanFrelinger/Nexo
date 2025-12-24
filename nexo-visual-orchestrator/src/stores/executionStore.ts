import { create } from 'zustand';
import type { WorkflowExecutionStatus, LogEntry, ConflictInfo } from '../types/execution';
import { nanoid } from 'nanoid';

interface ExecutionState {
  status: WorkflowExecutionStatus;
  executionId: string | null;
  logs: LogEntry[];
  conflicts: ConflictInfo[];

  // Actions
  setStatus: (status: WorkflowExecutionStatus) => void;
  setExecutionId: (id: string | null) => void;
  addLog: (level: LogEntry['level'], message: string, agentId?: string, data?: unknown) => void;
  clearLogs: () => void;
  addConflict: (conflict: ConflictInfo) => void;
  resolveConflict: (conflictId: string) => void;
  clearConflicts: () => void;
  reset: () => void;
}

export const useExecutionStore = create<ExecutionState>((set) => ({
  status: 'idle',
  executionId: null,
  logs: [],
  conflicts: [],

  setStatus: (status) => set({ status }),

  setExecutionId: (executionId) => set({ executionId }),

  addLog: (level, message, agentId?, data?) => {
    const entry: LogEntry = {
      id: nanoid(),
      timestamp: Date.now(),
      level,
      message,
      agentId,
      data,
    };
    set((state) => ({ logs: [...state.logs, entry] }));
  },

  clearLogs: () => set({ logs: [] }),

  addConflict: (conflict) => {
    set((state) => ({ conflicts: [...state.conflicts, conflict] }));
  },

  resolveConflict: (conflictId) => {
    set((state) => ({
      conflicts: state.conflicts.map((c) =>
        c.id === conflictId ? { ...c, status: 'resolved' as const } : c
      ),
    }));
  },

  clearConflicts: () => set({ conflicts: [] }),

  reset: () =>
    set({
      status: 'idle',
      executionId: null,
      logs: [],
      conflicts: [],
    }),
}));

