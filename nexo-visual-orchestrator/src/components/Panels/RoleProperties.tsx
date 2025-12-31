// src/components/Panels/RoleProperties.tsx

/**
 * RoleProperties Component
 * 
 * Properties panel for editing a selected role definition. Allows editing:
 * - Role name and description
 * - Model tier (strategic/tactical/execution)
 * - Scaling configuration (min/max instances, scale to zero)
 * - Autonomy level
 * - Owned items (comma-separated list)
 * - View associated instances
 * 
 * Provides a delete button to remove the role from the workflow.
 */

import * as HiIcons from 'react-icons/hi';
import type { RoleDefinition, AgentInstance, AutonomyLevel } from '../../types/workflow';

interface RolePropertiesProps {
  role: RoleDefinition;
  roleInstances: AgentInstance[];
  onUpdate: (updates: Partial<RoleDefinition>) => void;
  onDelete: () => void;
}

export default function RoleProperties({
  role,
  roleInstances,
  onUpdate,
  onDelete,
}: RolePropertiesProps) {
  const IconComponent = (HiIcons as any)[role.icon] || HiIcons.HiCube;

  return (
    <>
      {/* Header */}
      <div className="p-3 border-b border-slate-700">
        <div className="flex items-center gap-2">
          <IconComponent className={`w-5 h-5 text-${role.color}-400`} />
          <input
            type="text"
            value={role.name}
            onChange={(e) => onUpdate({ name: e.target.value })}
            className="flex-1 bg-transparent font-semibold text-sm focus:outline-none border-b border-transparent focus:border-blue-500 text-white"
          />
        </div>
        <p className="text-xs text-slate-500 mt-1">{role.role} ({role.modelConfig.tier} tier)</p>
      </div>

      {/* Properties */}
      <div className="flex-1 overflow-y-auto p-3 space-y-4 custom-scrollbar">
        {/* Description */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Description</label>
          <textarea
            value={role.description}
            onChange={(e) => onUpdate({ description: e.target.value })}
            rows={3}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none text-slate-200"
          />
        </div>

        {/* Model Config */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Model Tier</label>
          <select
            value={role.modelConfig.tier}
            onChange={(e) => onUpdate({ 
              modelConfig: { ...role.modelConfig, tier: e.target.value as any }
            })}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 text-slate-200"
          >
            <option value="strategic">Strategic (Opus/GPT-4)</option>
            <option value="tactical">Tactical (Sonnet/GPT-4-mini)</option>
            <option value="execution">Execution (Haiku/GPT-3.5/Local)</option>
          </select>
        </div>

        {/* Scaling Config */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Scaling</label>
          <div className="space-y-2">
            <div className="flex gap-2">
              <input
                type="number"
                min="0"
                value={role.scalingConfig.minInstances}
                onChange={(e) => onUpdate({
                  scalingConfig: { ...role.scalingConfig, minInstances: parseInt(e.target.value) || 0 }
                })}
                className="w-20 px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm text-slate-200"
                placeholder="Min"
              />
              <span className="text-slate-500 self-center">-</span>
              <input
                type="number"
                min="1"
                value={role.scalingConfig.maxInstances}
                onChange={(e) => onUpdate({
                  scalingConfig: { ...role.scalingConfig, maxInstances: parseInt(e.target.value) || 1 }
                })}
                className="w-20 px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm text-slate-200"
                placeholder="Max"
              />
              <span className="text-xs text-slate-500 self-center">instances</span>
            </div>
            <label className="flex items-center gap-2 text-xs text-slate-400">
              <input
                type="checkbox"
                checked={role.scalingConfig.scaleToZero}
                onChange={(e) => onUpdate({
                  scalingConfig: { ...role.scalingConfig, scaleToZero: e.target.checked }
                })}
                className="rounded"
              />
              Can scale to zero
            </label>
          </div>
        </div>

        {/* Autonomy Level */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Autonomy Level</label>
          <select
            value={role.autonomyLevel}
            onChange={(e) => onUpdate({ autonomyLevel: e.target.value as AutonomyLevel })}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 text-slate-200"
          >
            <option value="conservative">Conservative</option>
            <option value="balanced">Balanced</option>
            <option value="aggressive">Aggressive</option>
          </select>
        </div>

        {/* Owns */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Owns (comma-separated)</label>
          <textarea
            value={role.owns.join(', ')}
            onChange={(e) => onUpdate({ owns: e.target.value.split(',').map(s => s.trim()).filter(Boolean) })}
            rows={2}
            className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none text-slate-200"
          />
        </div>

        {/* Instances */}
        <div>
          <label className="block text-xs text-slate-400 mb-1">Instances ({roleInstances.length})</label>
          <div className="space-y-1 max-h-32 overflow-y-auto">
            {roleInstances.map((instance) => (
              <div
                key={instance.id}
                className="flex items-center justify-between p-2 bg-slate-800/50 rounded text-xs"
              >
                <span className="text-slate-300">
                  #{instance.instanceNumber} - {instance.status}
                </span>
                {instance.currentTask && (
                  <span className="text-blue-400 truncate max-w-[100px]" title={instance.currentTask.description}>
                    {instance.currentTask.description.slice(0, 15)}...
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="p-3 border-t border-slate-700">
        <button
          onClick={onDelete}
          className="w-full px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-sm hover:bg-red-500/30 transition-colors"
        >
          Delete Role
        </button>
      </div>
    </>
  );
}

