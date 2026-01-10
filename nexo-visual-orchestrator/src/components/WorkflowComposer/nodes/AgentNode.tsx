import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';
import type { NodeProps } from 'reactflow';
import { ExecutionOverlay } from '../ExecutionOverlay';

interface AgentNodeData {
  agentId: string;
  name: string;
  description?: string;
  mode: 'Auto' | 'DeterministicOnly' | 'AgenticPreferred' | 'Mixed';
  behaviors?: BehaviorInfo[];
  brickOverrides: Record<string, 'Deterministic' | 'Agentic'>;
  parameters: Record<string, any>;
  executionState?: NodeExecutionState;
  inputs?: NodePort[];
  outputs?: NodePort[];
}

interface BehaviorInfo {
  id: string;
  name: string;
  bricks: BrickInfo[];
}

interface BrickInfo {
  id: string;
  name: string;
  hasDeterministic: boolean;
  hasAgentic: boolean;
  defaultImplementation: 'Deterministic' | 'Agentic';
}

interface NodePort {
  id: string;
  name: string;
  dataType: string;
}

interface NodeExecutionState {
  status: 'waiting' | 'running' | 'completed' | 'failed';
  progress?: number;
  currentBrick?: string;
  currentImplementation?: 'Deterministic' | 'Agentic';
}

export const AgentNode: React.FC<NodeProps<AgentNodeData>> = memo(({ data, selected }) => {
  const { name, behaviors = [], mode, brickOverrides, executionState } = data;
  
  // Calculate implementation distribution
  const allBricks = behaviors.flatMap(b => b.bricks);
  const distribution = calculateDistribution(allBricks, brickOverrides);
  
  const statusClass = executionState?.status === 'running' ? 'running' :
                     executionState?.status === 'completed' ? 'complete' :
                     executionState?.status === 'failed' ? 'failed' : '';

  return (
    <div className={`agent-node min-w-[200px] bg-slate-800 rounded-lg border-2 relative ${
      selected ? 'border-blue-500' : 'border-slate-700'
    } ${statusClass}`}>
      <ExecutionOverlay
        status={executionState?.status || 'waiting'}
        progress={executionState?.progress}
        currentBrick={executionState?.currentBrick}
        implementation={executionState?.currentImplementation}
      />
      {/* Input handles */}
      {data.inputs?.map((input, i) => (
        <Handle
          key={input.id}
          type="target"
          position={Position.Left}
          id={input.id}
          style={{ top: 40 + i * 20 }}
          className={`handle handle-${input.dataType} w-3 h-3 border-2 border-white`}
        />
      ))}
      
      {/* Header */}
      <div className="node-header p-3 bg-slate-900 rounded-t-lg flex items-center gap-2 border-b border-slate-700">
        <span className="node-icon text-xl">🛡️</span>
        <span className="node-name flex-1 font-semibold text-sm text-white truncate">{name}</span>
        <span className="node-mode text-xs">
          {mode === 'DeterministicOnly' && '⚙️'}
          {mode === 'AgenticPreferred' && '🤖'}
          {mode === 'Mixed' && '⚙️🤖'}
          {mode === 'Auto' && '○'}
        </span>
      </div>
      
      {/* Implementation distribution bar */}
      <div className="implementation-bar h-1 flex bg-slate-700">
        <div 
          className="bar-deterministic bg-blue-600 transition-all"
          style={{ width: `${distribution.deterministic}%` }}
        />
        <div 
          className="bar-agentic bg-purple-600 transition-all"
          style={{ width: `${distribution.agentic}%` }}
        />
      </div>
      
      {/* Behaviors (collapsed by default) */}
      {selected && behaviors.length > 0 && (
        <div className="node-behaviors p-2 bg-slate-800 space-y-1">
          {behaviors.map(behavior => (
            <div key={behavior.id} className="behavior text-xs">
              <div className="behavior-name font-medium text-slate-300 mb-1">{behavior.name}</div>
              <div className="bricks-list space-y-1">
                {behavior.bricks.map(brick => {
                  const impl = brickOverrides[brick.id] || brick.defaultImplementation;
                  return (
                    <div key={brick.id} className="brick-item flex items-center gap-2 text-xs text-slate-400">
                      <span className="w-2 h-2 rounded-full bg-slate-600"></span>
                      <span className="flex-1">{brick.name}</span>
                      <span>{impl === 'Deterministic' ? '⚙️' : '🤖'}</span>
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}
      
      {/* Execution progress */}
      {executionState?.status === 'running' && (
        <div className="execution-progress p-2 bg-slate-900 border-t border-slate-700">
          <div className="progress-bar h-4 bg-gradient-to-r from-blue-600 to-purple-600 rounded relative overflow-hidden">
            <div 
              className="absolute inset-0 bg-white/20 animate-pulse"
              style={{ width: `${executionState.progress || 0}%` }}
            />
          </div>
          <div className="progress-text text-xs text-slate-400 mt-1">
            {executionState.currentBrick} [{executionState.currentImplementation}]
          </div>
        </div>
      )}
      
      {/* Output handles */}
      {data.outputs?.map((output, i) => (
        <Handle
          key={output.id}
          type="source"
          position={Position.Right}
          id={output.id}
          style={{ top: 40 + i * 20 }}
          className={`handle handle-${output.dataType} w-3 h-3 border-2 border-white`}
        />
      ))}
    </div>
  );
});

AgentNode.displayName = 'AgentNode';

function calculateDistribution(
  bricks: BrickInfo[],
  overrides: Record<string, 'Deterministic' | 'Agentic'>
): { deterministic: number; agentic: number } {
  if (bricks.length === 0) return { deterministic: 50, agentic: 50 };
  
  const deterministic = bricks.filter(b => 
    (overrides[b.id] || b.defaultImplementation) === 'Deterministic'
  ).length;
  
  const agentic = bricks.length - deterministic;
  
  return {
    deterministic: (deterministic / bricks.length) * 100,
    agentic: (agentic / bricks.length) * 100,
  };
}
