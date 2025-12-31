// src/components/Panels/WorkflowSettings.tsx

/**
 * WorkflowSettings Component
 * 
 * Displays and allows editing of global workflow settings:
 * - Default autonomy level (conservative/balanced/aggressive)
 * - Conflict resolution strategy
 * - Global scaling multiplier
 * 
 * Shown in the properties panel when no specific item is selected.
 */

import type { AutonomyLevel, ConflictResolution } from '../../types/workflow';

interface WorkflowSettingsProps {
  defaultAutonomyLevel: AutonomyLevel;
  conflictResolution: ConflictResolution;
  globalScalingMultiplier: number;
  onUpdateSettings: (updates: {
    defaultAutonomyLevel?: AutonomyLevel;
    conflictResolution?: ConflictResolution;
    globalScalingMultiplier?: number;
  }) => void;
}

export default function WorkflowSettings({
  defaultAutonomyLevel,
  conflictResolution,
  globalScalingMultiplier,
  onUpdateSettings,
}: WorkflowSettingsProps) {
  return (
    <div className="w-80 bg-surface border-l border-slate-700 p-4 flex-shrink-0">
      <p className="text-slate-500 text-sm">Select a role, instance, or relationship to view properties</p>
      <div className="mt-6">
        <h3 className="text-sm font-semibold text-slate-300 mb-2">Workflow Settings</h3>
        <div className="space-y-3">
          <div>
            <label className="block text-xs text-slate-400 mb-1">Default Autonomy Level</label>
            <select
              value={defaultAutonomyLevel}
              onChange={(e) => onUpdateSettings({ defaultAutonomyLevel: e.target.value as AutonomyLevel })}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
            >
              <option value="conservative">Conservative</option>
              <option value="balanced">Balanced</option>
              <option value="aggressive">Aggressive</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-slate-400 mb-1">Conflict Resolution</label>
            <select
              value={conflictResolution}
              onChange={(e) => onUpdateSettings({ conflictResolution: e.target.value as ConflictResolution })}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
            >
              <option value="always-escalate">Always Escalate</option>
              <option value="negotiate-first">Negotiate First</option>
              <option value="decide-notify">Decide & Notify</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-slate-400 mb-1">Global Scaling Multiplier</label>
            <input
              type="number"
              min="0.1"
              max="5"
              step="0.1"
              value={globalScalingMultiplier}
              onChange={(e) => onUpdateSettings({ globalScalingMultiplier: parseFloat(e.target.value) })}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
            />
          </div>
        </div>
      </div>
    </div>
  );
}

