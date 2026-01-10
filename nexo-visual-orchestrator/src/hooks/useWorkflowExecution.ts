import { useState, useCallback } from 'react';
import { useNexoClient } from './useNexoClient';
import type { WorkflowDefinition, WorkflowExecutionState } from '../types/workflowComposer';

export function useWorkflowExecution() {
  const [executionState, setExecutionState] = useState<WorkflowExecutionState | null>(null);
  const { client } = useNexoClient();

  const execute = useCallback(async (workflow: WorkflowDefinition) => {
    if (!client) return;

    setExecutionState({
      status: 'running',
      nodeStates: {},
      activeConnections: [],
      metrics: {
        totalDuration: 0,
        nodesExecuted: 0,
        deterministicBricks: 0,
        agenticBricks: 0,
        totalTokensUsed: 0,
        cacheHits: 0,
      },
    });

    try {
      // Call the workflow execution API
      const result = await client.executeWorkflow(workflow);
      
      // Stream execution events
      // This would be connected to SignalR or WebSocket in production
      // For now, simulate with polling or direct updates
      
      setExecutionState({
        status: 'completed',
        correlationId: result.correlationId,
        nodeStates: result.nodeStates || {},
        activeConnections: [],
        metrics: result.metrics || {
          totalDuration: 0,
          nodesExecuted: 0,
          deterministicBricks: 0,
          agenticBricks: 0,
          totalTokensUsed: 0,
          cacheHits: 0,
        },
      });
    } catch (error) {
      setExecutionState(prev => prev ? {
        ...prev,
        status: 'failed',
      } : null);
    }
  }, [client]);

  return {
    execute,
    executionState,
  };
}
