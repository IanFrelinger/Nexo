/**
 * PropertiesPanel Component
 * 
 * Right sidebar panel that displays properties and settings based on the current
 * selection. Acts as a dispatcher that renders different property views:
 * - WorkflowSettings: When nothing is selected (default)
 * - RoleProperties: When a role is selected
 * - InstanceProperties: When an instance is selected
 * - RelationshipProperties: When a relationship is selected
 */

import { useOrchestrationStore } from '../../stores/orchestrationStore';
import WorkflowSettings from './WorkflowSettings';
import RoleProperties from './RoleProperties';
import InstanceProperties from './InstanceProperties';
import RelationshipProperties from './RelationshipProperties';

/**
 * PropertiesPanel - Context-aware properties panel
 * @returns JSX element
 */
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
      <WorkflowSettings
        defaultAutonomyLevel={settings.defaultAutonomyLevel}
        conflictResolution={settings.conflictResolution}
        globalScalingMultiplier={settings.globalScalingMultiplier}
        onUpdateSettings={updateSettings}
      />
    );
  }

  return (
    <div className="w-80 bg-surface border-l border-slate-700 flex flex-col h-full flex-shrink-0">
      {selectedRole && (
        <RoleProperties
          role={selectedRole}
          roleInstances={getInstancesForRole(selectedRole.id)}
          onUpdate={(updates) => updateRole(selectedRole.id, updates)}
          onDelete={() => removeRole(selectedRole.id)}
        />
      )}
      {selectedInstance && (
        <InstanceProperties
          instance={selectedInstance}
          role={roles.find(r => r.id === selectedInstance.roleId)}
        />
      )}
      {selectedRelationship && (
        <RelationshipProperties
          relationship={selectedRelationship}
          sourceRole={roles.find(r => r.id === selectedRelationship.sourceRoleId)}
          targetRole={roles.find(r => r.id === selectedRelationship.targetRoleId)}
          onDelete={() => removeRelationship(selectedRelationship.id)}
        />
      )}
    </div>
  );
}
