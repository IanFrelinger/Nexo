// src/stores/orchestrationStore.ts

import { create } from 'zustand';
import { nanoid } from 'nanoid';
import { constrainToTier, TIER_CONFIGS } from '../utils/layoutEngine';
import type { 
  RoleDefinition, 
  Relationship, 
  WorkflowSettings,
  AgentInstance,
  InstanceStatus,
  TaskAssignment,
} from '../types/workflow';

interface OrchestrationState {
  // Data
  roles: RoleDefinition[];
  instances: AgentInstance[];
  relationships: Relationship[];
  settings: WorkflowSettings;
  
  // UI State
  expandedRoles: Set<string>;              // Which roles are expanded in the UI
  collapsedTiers: Set<string>;            // Which tiers are collapsed (strategic, tactical, execution)
  visibleRelationshipTypes: Set<string>;   // Which relationship types are visible (delegates, reports-to, negotiates, observes)
  highlightedPath: string[] | null;        // Path from selected role to architect (array of role IDs)
  selectedRoleId: string | null;
  selectedInstanceId: string | null;
  selectedRelationshipId: string | null;
  
  // Actions - Roles
  loadWorkflow: (roles: RoleDefinition[], relationships: Relationship[]) => void;
  addRole: (role: RoleDefinition) => void;
  updateRole: (id: string, updates: Partial<RoleDefinition>) => void;
  removeRole: (id: string) => void;
  toggleRoleExpand: (roleId: string) => void;
  
  // Actions - Instances
  spawnInstance: (roleId: string) => AgentInstance;
  terminateInstance: (instanceId: string) => void;
  updateInstance: (instanceId: string, updates: Partial<AgentInstance>) => void;
  assignTask: (instanceId: string, task: TaskAssignment) => void;
  
  // Actions - Relationships
  addRelationship: (relationship: Relationship) => void;
  removeRelationship: (id: string) => void;
  
  // Actions - Selection
  setSelectedRole: (id: string | null) => void;
  setSelectedInstance: (id: string | null) => void;
  setSelectedRelationship: (id: string | null) => void;
  
  // Actions - UI Controls
  toggleTierCollapse: (tier: string) => void;
  toggleRelationshipTypeVisibility: (type: string) => void;
  highlightPathToArchitect: (roleId: string) => void;
  clearHighlightedPath: () => void;
  
  // Actions - Settings
  updateSettings: (settings: Partial<WorkflowSettings>) => void;
  clearWorkflow: () => void;
  
  // Getters
  getInstancesForRole: (roleId: string) => AgentInstance[];
  getActiveInstances: () => AgentInstance[];
}

const defaultSettings: WorkflowSettings = {
  name: 'Untitled Workflow',
  defaultAutonomyLevel: 'balanced',
  conflictResolution: 'negotiate-first',
  requireApprovalFor: ['New features', 'Architecture changes'],
  globalScalingMultiplier: 1.0,
};

export const useOrchestrationStore = create<OrchestrationState>((set, get) => ({
  roles: [],
  instances: [],
  relationships: [],
  settings: defaultSettings,
  expandedRoles: new Set(),
  collapsedTiers: new Set(),
  visibleRelationshipTypes: new Set(['delegates', 'reports-to', 'negotiates', 'observes']), // All visible by default
  highlightedPath: null,
  selectedRoleId: null,
  selectedInstanceId: null,
  selectedRelationshipId: null,
  
  // Expose store to window for testing
  ...(typeof window !== 'undefined' ? { __store: { getState: get } } : {}),
  
  loadWorkflow: (roles, relationships) => {
    // Ensure all roles have positions
    // If a role already has a position (e.g., from layout), preserve it
    // Only calculate default position if missing
    const rolesWithPositions = roles.map((role, index) => {
      // Check if position exists and is valid
      // Layout function sets positions, so we should preserve them
      const hasPosition = role.position && 
                          role.position.x !== undefined && 
                          role.position.y !== undefined &&
                          typeof role.position.x === 'number' && 
                          typeof role.position.y === 'number' &&
                          !isNaN(role.position.x) && 
                          !isNaN(role.position.y);
      
      if (hasPosition) {
        // Position exists from layout - preserve it exactly
        // The layout function should have set positions to tier centers (250, 650, 1050)
        // But if layout didn't apply, constrain to tier boundaries
        const constrainedY = constrainToTier(role.modelConfig.tier, role.position.y);
        // Always use constrained position to ensure it's within tier bounds
        return {
          ...role,
          position: {
            x: role.position.x,
            y: constrainedY, // Use constrained Y to ensure it's within tier bounds
          },
        };
      }
      // Calculate position if missing - use tier center as default
      const tierConfig = TIER_CONFIGS.find(t => t.tier === role.modelConfig.tier);
      const defaultY = tierConfig?.y || 100;
      const constrainedY = constrainToTier(role.modelConfig.tier, defaultY);
      
      const nodeWidth = 288;
      const spacing = 50;
      const startX = 100;
      const nodesPerRow = 4;
      const col = index % nodesPerRow;
      
      return {
        ...role,
        position: {
          x: startX + col * (nodeWidth + spacing),
          y: constrainedY,
        },
      };
    });
    
    // Initialize with one instance per role (at minInstances)
    const instances: AgentInstance[] = [];
    rolesWithPositions.forEach(role => {
      for (let i = 0; i < role.scalingConfig.minInstances; i++) {
        instances.push(createInitialInstance(role.id, i + 1));
      }
    });
    
    set({
      roles: rolesWithPositions,
      relationships,
      instances,
      selectedRoleId: null,
      selectedInstanceId: null,
      selectedRelationshipId: null,
      expandedRoles: new Set(),
    });
  },
  
  addRole: (role) => set((state) => {
    // Spawn initial instances up to minInstances
    const newInstances: AgentInstance[] = [];
    for (let i = 0; i < role.scalingConfig.minInstances; i++) {
      newInstances.push(createInitialInstance(role.id, i + 1));
    }
    
    // Ensure role has a position - if not, assign default based on tier
    let roleWithPosition = role;
    if (!role.position || role.position.x === undefined || role.position.y === undefined) {
      const tierConfig = TIER_CONFIGS.find(t => t.tier === role.modelConfig.tier);
      const defaultY = tierConfig?.y || 100;
      const constrainedY = constrainToTier(role.modelConfig.tier, defaultY);
      
      // Calculate default X position
      const existingRoles = state.roles;
      const nodeWidth = 288;
      const spacing = 50;
      const startX = 100;
      const col = existingRoles.length % 4;
      
      roleWithPosition = {
        ...role,
        position: {
          x: startX + col * (nodeWidth + spacing),
          y: constrainedY,
        },
      };
    }
    
    return {
      roles: [...state.roles, roleWithPosition],
      instances: [...state.instances, ...newInstances],
    };
  }),
  
  updateRole: (id, updates) => set((state) => {
    const role = state.roles.find(r => r.id === id);
    if (!role) return state;
    
    // Determine the tier to use for constraints
    // If tier is being updated, use the new tier; otherwise use current tier
    const targetTier = updates.modelConfig?.tier || role.modelConfig.tier;
    
    // If position is being updated, constrain it to tier boundaries
    let constrainedUpdates = { ...updates };
    if (updates.position && updates.position.y !== undefined) {
      // Constrain Y position to the target tier's boundaries
      const constrainedY = constrainToTier(targetTier, updates.position.y);
      constrainedUpdates = {
        ...updates,
        position: {
          x: updates.position.x,
          y: constrainedY,
        },
      };
    }
    
    // If tier is being updated, ensure modelConfig is properly merged
    if (updates.modelConfig) {
      constrainedUpdates = {
        ...constrainedUpdates,
        modelConfig: {
          ...role.modelConfig,
          ...updates.modelConfig,
        },
      };
    }
    
    return {
      roles: state.roles.map((r) => 
        r.id === id ? { ...r, ...constrainedUpdates } : r
      ),
    };
  }),
  
  removeRole: (id) => set((state) => ({
    roles: state.roles.filter((r) => r.id !== id),
    instances: state.instances.filter((i) => i.roleId !== id),
    relationships: state.relationships.filter(
      (r) => r.sourceRoleId !== id && r.targetRoleId !== id
    ),
    selectedRoleId: state.selectedRoleId === id ? null : state.selectedRoleId,
    expandedRoles: new Set([...state.expandedRoles].filter(r => r !== id)),
  })),
  
  toggleRoleExpand: (roleId) => set((state) => {
    const newExpanded = new Set(state.expandedRoles);
    if (newExpanded.has(roleId)) {
      newExpanded.delete(roleId);
    } else {
      newExpanded.add(roleId);
    }
    return { expandedRoles: newExpanded };
  }),
  
  spawnInstance: (roleId) => {
    const role = get().roles.find(r => r.id === roleId);
    if (!role) {
      throw new Error(`Role not found: ${roleId}`);
    }
    
    const existingInstances = get().getInstancesForRole(roleId);
    const activeCount = existingInstances.filter(i => i.status !== 'terminating').length;
    
    if (activeCount >= role.scalingConfig.maxInstances) {
      throw new Error(`Cannot spawn: max instances (${role.scalingConfig.maxInstances}) reached`);
    }
    
    const instanceNumber = existingInstances.length + 1;
    const newInstance = createInitialInstance(roleId, instanceNumber);
    
    set((state) => ({
      instances: [...state.instances, newInstance],
    }));
    
    return newInstance;
  },
  
  terminateInstance: (instanceId) => set((state) => {
    const instance = state.instances.find(i => i.id === instanceId);
    if (!instance) return state;
    
    const role = state.roles.find(r => r.id === instance.roleId);
    if (!role) return state;
    
    const activeInstances = state.instances.filter(
      i => i.roleId === instance.roleId && i.status !== 'terminating'
    );
    
    // Don't terminate if we'd go below minInstances
    if (activeInstances.length <= role.scalingConfig.minInstances) {
      return state;
    }
    
    // Mark as terminating
    return {
      instances: state.instances.map(i => 
        i.id === instanceId ? { ...i, status: 'terminating' as InstanceStatus } : i
      ),
      selectedInstanceId: state.selectedInstanceId === instanceId ? null : state.selectedInstanceId,
    };
  }),
  
  updateInstance: (instanceId, updates) => set((state) => ({
    instances: state.instances.map((i) => 
      i.id === instanceId ? { ...i, ...updates } : i
    ),
  })),
  
  assignTask: (instanceId, task) => set((state) => ({
    instances: state.instances.map((i) => {
      if (i.id === instanceId) {
        return {
          ...i,
          currentTask: task,
          status: 'busy' as InstanceStatus,
          taskQueue: [...i.taskQueue, task],
        };
      }
      return i;
    }),
  })),
  
  addRelationship: (relationship) => set((state) => ({
    relationships: [...state.relationships, relationship],
  })),
  
  removeRelationship: (id) => set((state) => ({
    relationships: state.relationships.filter((r) => r.id !== id),
    selectedRelationshipId: state.selectedRelationshipId === id ? null : state.selectedRelationshipId,
  })),
  
  setSelectedRole: (id) => set({ 
    selectedRoleId: id, 
    selectedInstanceId: null,
    selectedRelationshipId: null,
  }),
  
  setSelectedInstance: (id) => set({ 
    selectedInstanceId: id,
    selectedRoleId: null,
    selectedRelationshipId: null,
  }),
  
  setSelectedRelationship: (id) => set({ 
    selectedRelationshipId: id,
    selectedRoleId: null,
    selectedInstanceId: null,
  }),
  
  toggleTierCollapse: (tier) => set((state) => {
    const newCollapsed = new Set(state.collapsedTiers);
    if (newCollapsed.has(tier)) {
      newCollapsed.delete(tier);
    } else {
      newCollapsed.add(tier);
    }
    return { collapsedTiers: newCollapsed };
  }),
  
  toggleRelationshipTypeVisibility: (type) => set((state) => {
    const newVisible = new Set(state.visibleRelationshipTypes);
    if (newVisible.has(type)) {
      newVisible.delete(type);
    } else {
      newVisible.add(type);
    }
    return { visibleRelationshipTypes: newVisible };
  }),
  
  highlightPathToArchitect: (roleId) => set((state) => {
    // Find path from role to architect using reportsTo relationships
    const path: string[] = [];
    let currentRoleId: string | null = roleId;
    const visited = new Set<string>();
    
    while (currentRoleId && !visited.has(currentRoleId)) {
      visited.add(currentRoleId);
      path.push(currentRoleId);
      
      const role = state.roles.find(r => r.id === currentRoleId);
      if (!role || !role.reportsTo) {
        break;
      }
      
      // Check if we've reached an architect (or top-level role)
      if (role.modelConfig.tier === 'strategic' && role.role.toLowerCase().includes('architect')) {
        path.push(role.reportsTo);
        break;
      }
      
      currentRoleId = role.reportsTo;
    }
    
    return { highlightedPath: path.length > 1 ? path : null };
  }),
  
  clearHighlightedPath: () => set({ highlightedPath: null }),
  
  updateSettings: (updates) => set((state) => ({
    settings: { ...state.settings, ...updates },
  })),
  
  clearWorkflow: () => set({
    roles: [],
    instances: [],
    relationships: [],
    selectedRoleId: null,
    selectedInstanceId: null,
    selectedRelationshipId: null,
    expandedRoles: new Set(),
    collapsedTiers: new Set(),
    visibleRelationshipTypes: new Set(['delegates', 'reports-to', 'negotiates', 'observes']),
    highlightedPath: null,
  }),
  
  getInstancesForRole: (roleId) => {
    return get().instances.filter(i => i.roleId === roleId);
  },
  
  getActiveInstances: () => {
    return get().instances.filter(i => i.status !== 'terminating');
  },
}));

// Helper to create initial instance
function createInitialInstance(roleId: string, instanceNumber: number): AgentInstance {
  return {
    id: `${roleId}-${nanoid(6)}`,
    roleId,
    instanceNumber,
    status: 'idle',
    spawnedAt: new Date().toISOString(),
    lastActiveAt: new Date().toISOString(),
    currentTask: null,
    taskQueue: [],
    metrics: {
      tasksCompleted: 0,
      tasksEscalated: 0,
      avgLatencyMs: 0,
      errorCount: 0,
      negotiationsParticipated: 0,
    },
    shortTermMemory: [],
    activeNegotiations: [],
  };
}
