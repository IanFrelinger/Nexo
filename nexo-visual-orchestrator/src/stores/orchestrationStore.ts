// src/stores/orchestrationStore.ts

import { create } from 'zustand';
import { nanoid } from 'nanoid';
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
  selectedRoleId: null,
  selectedInstanceId: null,
  selectedRelationshipId: null,
  
  // Expose store to window for testing
  ...(typeof window !== 'undefined' ? { __store: { getState: get } } : {}),
  
  loadWorkflow: (roles, relationships) => {
    // Ensure all roles have positions
    const rolesWithPositions = roles.map((role, index) => {
      if (role.position) {
        return role;
      }
      // Calculate position if missing
      const nodeWidth = 288;
      const nodeHeight = 200;
      const spacing = 50;
      const startX = 100;
      const startY = 100;
      const nodesPerRow = 4;
      const row = Math.floor(index / nodesPerRow);
      const col = index % nodesPerRow;
      return {
        ...role,
        position: {
          x: startX + col * (nodeWidth + spacing),
          y: startY + row * (nodeHeight + spacing),
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
    
    return {
      roles: [...state.roles, role],
      instances: [...state.instances, ...newInstances],
    };
  }),
  
  updateRole: (id, updates) => set((state) => ({
    roles: state.roles.map((r) => 
      r.id === id ? { ...r, ...updates } : r
    ),
  })),
  
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
