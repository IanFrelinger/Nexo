import { useOrchestrationStore } from '../../stores/orchestrationStore';
import type { RoleDefinition, AgentInstance, Relationship, AutonomyLevel, ConflictResolution } from '../../types/workflow';
import * as HiIcons from 'react-icons/hi';

export default function PropertiesPanel() {
  const {
    roles,
    instances,
    relationships,
    selectedRoleId,
    selectedInstanceId,
    selectedRelationshipId,
    updateRole,
    removeRole,
    updateSettings,
    settings,
    removeRelationship,
    getInstancesForRole,
  } = useOrchestrationStore();

  const selectedRole = roles.find((r) => r.id === selectedRoleId);
  const selectedInstance = instances.find((i) => i.id === selectedInstanceId);
  const selectedRelationship = relationships.find((r) => r.id === selectedRelationshipId);

  if (!selectedRole && !selectedInstance && !selectedRelationship) {
    return (
      <div className="w-80 bg-surface border-l border-slate-700 p-4 flex-shrink-0">
        <p className="text-slate-500 text-sm">Select a role, instance, or relationship to view properties</p>
        <div className="mt-6">
          <h3 className="text-sm font-semibold text-slate-300 mb-2">Workflow Settings</h3>
          <div className="space-y-3">
            <div>
              <label className="block text-xs text-slate-400 mb-1">Default Autonomy Level</label>
              <select
                value={settings.defaultAutonomyLevel}
                onChange={(e) => updateSettings({ defaultAutonomyLevel: e.target.value as AutonomyLevel })}
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
                value={settings.conflictResolution}
                onChange={(e) => updateSettings({ conflictResolution: e.target.value as ConflictResolution })}
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
                value={settings.globalScalingMultiplier}
                onChange={(e) => updateSettings({ globalScalingMultiplier: parseFloat(e.target.value) })}
                className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>
        </div>
      </div>
    );
  }

  const handleRoleUpdate = (updates: Partial<RoleDefinition>) => {
    if (selectedRole) {
      updateRole(selectedRole.id, updates);
    }
  };

  const renderRoleProperties = (role: RoleDefinition) => {
    const IconComponent = (HiIcons as any)[role.icon] || HiIcons.HiCube;
    const roleInstances = getInstancesForRole(role.id);

    return (
      <>
        {/* Header */}
        <div className="p-3 border-b border-slate-700">
          <div className="flex items-center gap-2">
            <IconComponent className={`w-5 h-5 text-${role.color}-400`} />
            <input
              type="text"
              value={role.name}
              onChange={(e) => handleRoleUpdate({ name: e.target.value })}
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
              onChange={(e) => handleRoleUpdate({ description: e.target.value })}
              rows={3}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none text-slate-200"
            />
          </div>

          {/* Model Config */}
          <div>
            <label className="block text-xs text-slate-400 mb-1">Model Tier</label>
            <select
              value={role.modelConfig.tier}
              onChange={(e) => handleRoleUpdate({ 
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
                  onChange={(e) => handleRoleUpdate({
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
                  onChange={(e) => handleRoleUpdate({
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
                  onChange={(e) => handleRoleUpdate({
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
              onChange={(e) => handleRoleUpdate({ autonomyLevel: e.target.value as AutonomyLevel })}
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
              onChange={(e) => handleRoleUpdate({ owns: e.target.value.split(',').map(s => s.trim()).filter(Boolean) })}
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
            onClick={() => removeRole(role.id)}
            className="w-full px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-sm hover:bg-red-500/30 transition-colors"
          >
            Delete Role
          </button>
        </div>
      </>
    );
  };

  const renderInstanceProperties = (instance: AgentInstance) => {
    const role = roles.find(r => r.id === instance.roleId);
    
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
  };

  const renderRelationshipProperties = (relationship: Relationship) => {
    const sourceRole = roles.find(r => r.id === relationship.sourceRoleId);
    const targetRole = roles.find(r => r.id === relationship.targetRoleId);
    
    return (
      <>
        <div className="p-3 border-b border-slate-700">
          <h3 className="font-semibold text-sm text-white">Relationship</h3>
          <p className="text-xs text-slate-500 mt-1">
            {sourceRole?.name || relationship.sourceRoleId} --({relationship.type})--&gt; {targetRole?.name || relationship.targetRoleId}
          </p>
        </div>
        <div className="flex-1 overflow-y-auto p-3 space-y-4 custom-scrollbar">
          <div>
            <label className="block text-xs text-slate-400 mb-1">Type</label>
            <select
              value={relationship.type}
              onChange={() => {
                // Simplified: delete and re-add for type change
                removeRelationship(relationship.id);
              }}
              className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 text-slate-200"
            >
              <option value="delegates">Delegates</option>
              <option value="reports-to">Reports To</option>
              <option value="negotiates">Negotiates</option>
              <option value="observes">Observes</option>
            </select>
          </div>
          {relationship.type === 'negotiates' && relationship.metadata?.topics && (
            <div>
              <label className="block text-xs text-slate-400 mb-1">Topics (comma-separated)</label>
              <textarea
                value={relationship.metadata.topics.join(', ')}
                onChange={() => {
                  // This would need a proper update method
                }}
                rows={2}
                className="w-full px-2 py-1 bg-surface-dark border border-slate-600 rounded text-sm focus:outline-none focus:border-blue-500 resize-none text-slate-200"
              />
            </div>
          )}
        </div>
        <div className="p-3 border-t border-slate-700">
          <button
            onClick={() => removeRelationship(relationship.id)}
            className="w-full px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-sm hover:bg-red-500/30 transition-colors"
          >
            Delete Relationship
          </button>
        </div>
      </>
    );
  };

  return (
    <div className="w-80 bg-surface border-l border-slate-700 flex flex-col h-full flex-shrink-0">
      {selectedRole && renderRoleProperties(selectedRole)}
      {selectedInstance && renderInstanceProperties(selectedInstance)}
      {selectedRelationship && renderRelationshipProperties(selectedRelationship)}
    </div>
  );
}
