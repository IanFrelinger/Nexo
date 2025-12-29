// src/components/Panels/InstanceProperties.tsx

import type { AgentInstance, RoleDefinition } from '../../types/workflow';

interface InstancePropertiesProps {
  instance: AgentInstance;
  role?: RoleDefinition;
}

export default function InstanceProperties({ instance, role }: InstancePropertiesProps) {
  return (
    <>
      <div className="p-3 border-b border-slate-700">
        <h3 className="font-semibold text-sm text-white">Instance #{instance.instanceNumber}</h3>
        <p className="text-xs text-slate-500 mt-1">{role?.name || 'Unknown Role'}</p>
      </div>
      <div className="flex-1 overflow-y-auto p-3 space-y-4 custom-scrollbar">
        <div>
          <label className="block text-xs text-slate-400 mb-1">Status</label>
          <div className="text-sm text-slate-300">{instance.status}</div>
        </div>
        {instance.currentTask && (
          <div>
            <label className="block text-xs text-slate-400 mb-1">Current Task</label>
            <div className="text-sm text-slate-300 bg-slate-800/50 p-2 rounded">
              {instance.currentTask.description}
            </div>
          </div>
        )}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Metrics</label>
          <div className="space-y-1 text-xs text-slate-300">
            <div>Tasks Completed: {instance.metrics.tasksCompleted}</div>
            <div>Tasks Escalated: {instance.metrics.tasksEscalated}</div>
            <div>Avg Latency: {instance.metrics.avgLatencyMs}ms</div>
            <div>Errors: {instance.metrics.errorCount}</div>
          </div>
        </div>
      </div>
    </>
  );
}

