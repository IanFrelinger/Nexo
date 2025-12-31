/**
 * useMockExecution Hook
 * 
 * Custom React hook providing mock workflow execution functionality for
 * development and testing. Simulates workflow execution by:
 * - Resetting all instances to idle
 * - Iterating through roles and executing their first instance
 * - Simulating work with delays
 * - Updating instance status and metrics
 * - Logging execution events
 * 
 * Provides execute, pause, and cancel functions for controlling execution.
 * 
 * @returns Object with execute, pause, and cancel functions
 */

import { useCallback } from 'react';
import { useOrchestrationStore } from '../stores/orchestrationStore';
import { useExecutionStore } from '../stores/executionStore';
import { nanoid } from 'nanoid';

/**
 * Hook for mock workflow execution
 * @returns Execution control functions
 */
export function useMockExecution() {
  const { roles, instances, relationships, updateInstance, getInstancesForRole } = useOrchestrationStore();
  const { setStatus, setExecutionId, addLog, reset: resetExecution } = useExecutionStore();

  const execute = useCallback(async () => {
    // Reset everything
    instances.forEach((instance) => {
      updateInstance(instance.id, { status: 'idle', currentTask: null });
    });
    resetExecution();

    const executionId = nanoid(8);
    setExecutionId(executionId);
    setStatus('running');
    addLog('info', `Execution ${executionId} started`);

    // Simplified execution: iterate through roles
    for (const role of roles) {
      const roleInstances = getInstancesForRole(role.id);
      if (roleInstances.length === 0) continue;

      // Execute first instance of each role
      const instance = roleInstances[0];
      updateInstance(instance.id, { 
        status: 'busy', 
        currentTask: {
          id: nanoid(),
          description: 'Processing workflow...',
          priority: 1,
          assignedAt: new Date().toISOString(),
        }
      });
      addLog('info', `${role.name} (instance #${instance.instanceNumber}) started`, role.id);

      // Simulate work
      await sleep(1000);

      // Simulate completion
      updateInstance(instance.id, { 
        status: 'idle', 
        currentTask: null,
        metrics: {
          ...instance.metrics,
          tasksCompleted: instance.metrics.tasksCompleted + 1,
        }
      });
      addLog('info', `${role.name} (instance #${instance.instanceNumber}) completed`, role.id);

      await sleep(200);
    }

    setStatus('completed');
    addLog('info', `Execution ${executionId} completed successfully`);
  }, [roles, instances, relationships, updateInstance, getInstancesForRole, setStatus, setExecutionId, addLog, resetExecution]);

  const pause = useCallback(() => {
    setStatus('paused');
    addLog('warn', 'Execution paused');
  }, [setStatus, addLog]);

  const cancel = useCallback(() => {
    setStatus('idle');
    instances.forEach((instance) => {
      updateInstance(instance.id, { status: 'idle', currentTask: null });
    });
    addLog('warn', 'Execution cancelled');
  }, [setStatus, instances, updateInstance, addLog]);

  return { execute, pause, cancel };
}

function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

