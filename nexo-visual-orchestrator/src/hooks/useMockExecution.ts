import { useCallback } from 'react';
import { useOrchestrationStore } from '../stores/orchestrationStore';
import { useExecutionStore } from '../stores/executionStore';
import { topologicalSort } from '../utils/validation';
import { nanoid } from 'nanoid';

export function useMockExecution() {
  const { nodes, edges, setNodeStatus, setNodeOutput, resetAllStatus } = useOrchestrationStore();
  const { setStatus, setExecutionId, addLog, reset: resetExecution } = useExecutionStore();

  const execute = useCallback(async () => {
    // Reset everything
    resetAllStatus();
    resetExecution();

    const executionId = nanoid(8);
    setExecutionId(executionId);
    setStatus('running');
    addLog('info', `Execution ${executionId} started`);

    // Get execution order
    const workflow = {
      id: 'mock',
      name: 'Mock Workflow',
      nodes,
      edges,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    const order = topologicalSort(workflow);

    // Execute each agent in order
    for (const nodeId of order) {
      const node = nodes.find((n) => n.id === nodeId);
      if (!node) continue;

      // Start agent
      setNodeStatus(nodeId, 'running');
      addLog('info', `${node.data.label} started`, nodeId);

      // Simulate progress
      for (let progress = 0; progress <= 100; progress += 20) {
        await sleep(300);
        setNodeStatus(nodeId, 'running', progress);
      }

      // Simulate completion with mock output
      const mockOutput = generateMockOutput(node.data.agentType);
      setNodeOutput(nodeId, mockOutput);
      setNodeStatus(nodeId, 'completed');
      addLog('info', `${node.data.label} completed`, nodeId);

      await sleep(200);
    }

    setStatus('completed');
    addLog('info', `Execution ${executionId} completed successfully`);
  }, [nodes, edges, setNodeStatus, setNodeOutput, resetAllStatus, setStatus, setExecutionId, addLog, resetExecution]);

  const pause = useCallback(() => {
    setStatus('paused');
    addLog('warn', 'Execution paused');
  }, [setStatus, addLog]);

  const cancel = useCallback(() => {
    setStatus('idle');
    resetAllStatus();
    addLog('warn', 'Execution cancelled');
  }, [setStatus, resetAllStatus, addLog]);

  return { execute, pause, cancel };
}

function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function generateMockOutput(agentType: string): unknown {
  switch (agentType) {
    case 'architect':
      return {
        gameType: 'extraction-shooter',
        suggestedAgents: ['combat', 'economy', 'ai-behavior', 'level-design'],
        scope: 'MVP with core loop',
      };
    case 'combat':
      return {
        ttk: { min: 0.5, max: 2.0 },
        weapons: ['rifle', 'pistol', 'shotgun'],
        damageModel: 'hitzone-based',
        permadeath: true,
      };
    case 'economy':
      return {
        currencies: [{ name: 'Credits', maxStack: 1000000 }],
        lootTables: ['common', 'rare', 'legendary'],
        extractionBonus: 1.5,
      };
    case 'ai-behavior':
      return {
        behaviorTree: 'patrol-investigate-engage',
        difficultyLevels: 3,
        squadSize: { min: 2, max: 4 },
      };
    default:
      return { status: 'completed', timestamp: new Date().toISOString() };
  }
}

