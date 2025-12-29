// src/stores/customAgentStore.ts

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { nanoid } from 'nanoid';
import type { CustomAgentConfig, CustomAgentLibrary } from '../types/customAgent';
import type { AgentType } from '../types/agents';

interface CustomAgentStoreState {
  library: CustomAgentLibrary;
  
  // Actions
  createCustomAgent: (
    name: string,
    baseAgentType: AgentType,
    configuration: Record<string, unknown>,
    description?: string,
    tags?: string[]
  ) => CustomAgentConfig;
  
  updateCustomAgent: (agentId: string, updates: Partial<CustomAgentConfig>) => void;
  deleteCustomAgent: (agentId: string) => void;
  duplicateCustomAgent: (agentId: string) => CustomAgentConfig;
  
  // Favorites
  toggleFavorite: (agentId: string) => void;
  getFavorites: () => CustomAgentConfig[];
  
  // Recent
  markAsRecent: (agentId: string) => void;
  getRecent: (limit?: number) => CustomAgentConfig[];
  
  // Project association
  associateAgentWithProject: (agentId: string, projectId: string) => void;
  removeAgentFromProject: (agentId: string, projectId: string) => void;
  getAgentsForProject: (projectId: string) => CustomAgentConfig[];
  
  // Sharing
  setAgentShared: (agentId: string, shared: boolean) => void;
  getSharedAgents: () => CustomAgentConfig[];
  
  // Search and filter
  searchAgents: (query: string) => CustomAgentConfig[];
  getAgentsByTag: (tag: string) => CustomAgentConfig[];
  getAgentsByBaseType: (baseType: AgentType) => CustomAgentConfig[];
  
  // Get all agents (custom + built-in)
  getAllAvailableAgents: () => CustomAgentConfig[];
}

const STORAGE_KEY = 'nexo-custom-agent-library';

export const useCustomAgentStore = create<CustomAgentStoreState>()(
  persist(
    (set, get) => ({
      library: {
        agents: [],
        favorites: new Set(),
        recent: [],
      },
      
      createCustomAgent: (name, baseAgentType, configuration, description, tags = []) => {
        const newAgent: CustomAgentConfig = {
          id: nanoid(),
          name,
          description,
          baseAgentType,
          configuration,
          tags,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          isShared: false,
          metadata: {
            version: '1.0.0',
          },
        };
        
        set((state) => ({
          library: {
            ...state.library,
            agents: [...state.library.agents, newAgent],
          },
        }));
        
        return newAgent;
      },
      
      updateCustomAgent: (agentId, updates) => {
        set((state) => ({
          library: {
            ...state.library,
            agents: state.library.agents.map((agent) =>
              agent.id === agentId
                ? { ...agent, ...updates, updatedAt: new Date().toISOString() }
                : agent
            ),
          },
        }));
      },
      
      deleteCustomAgent: (agentId) => {
        set((state) => ({
          library: {
            ...state.library,
            agents: state.library.agents.filter((agent) => agent.id !== agentId),
            favorites: new Set([...state.library.favorites].filter((id) => id !== agentId)),
            recent: state.library.recent.filter((id) => id !== agentId),
          },
        }));
      },
      
      duplicateCustomAgent: (agentId) => {
        const agent = get().library.agents.find((a) => a.id === agentId);
        if (!agent) {
          throw new Error(`Custom agent not found: ${agentId}`);
        }
        
        const duplicated: CustomAgentConfig = {
          ...agent,
          id: nanoid(),
          name: `${agent.name} (Copy)`,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          projectIds: [], // Don't copy project associations
        };
        
        set((state) => ({
          library: {
            ...state.library,
            agents: [...state.library.agents, duplicated],
          },
        }));
        
        return duplicated;
      },
      
      toggleFavorite: (agentId) => {
        set((state) => {
          const newFavorites = new Set(state.library.favorites);
          if (newFavorites.has(agentId)) {
            newFavorites.delete(agentId);
          } else {
            newFavorites.add(agentId);
          }
          
          return {
            library: {
              ...state.library,
              favorites: newFavorites,
            },
          };
        });
      },
      
      getFavorites: () => {
        const state = get();
        return state.library.agents.filter((agent) =>
          state.library.favorites.has(agent.id)
        );
      },
      
      markAsRecent: (agentId) => {
        set((state) => {
          const recent = [...state.library.recent];
          // Remove if already exists
          const index = recent.indexOf(agentId);
          if (index >= 0) {
            recent.splice(index, 1);
          }
          // Add to front
          recent.unshift(agentId);
          // Keep only last 20
          const limited = recent.slice(0, 20);
          
          return {
            library: {
              ...state.library,
              recent: limited,
            },
          };
        });
      },
      
      getRecent: (limit = 10) => {
        const state = get();
        return state.library.recent
          .slice(0, limit)
          .map((id) => state.library.agents.find((a) => a.id === id))
          .filter((agent): agent is CustomAgentConfig => agent !== undefined);
      },
      
      associateAgentWithProject: (agentId, projectId) => {
        set((state) => ({
          library: {
            ...state.library,
            agents: state.library.agents.map((agent) =>
              agent.id === agentId
                ? {
                    ...agent,
                    projectIds: [
                      ...(agent.projectIds || []),
                      projectId,
                    ].filter((id, index, arr) => arr.indexOf(id) === index), // Unique
                    updatedAt: new Date().toISOString(),
                  }
                : agent
            ),
          },
        }));
      },
      
      removeAgentFromProject: (agentId, projectId) => {
        set((state) => ({
          library: {
            ...state.library,
            agents: state.library.agents.map((agent) =>
              agent.id === agentId
                ? {
                    ...agent,
                    projectIds: (agent.projectIds || []).filter((id) => id !== projectId),
                    updatedAt: new Date().toISOString(),
                  }
                : agent
            ),
          },
        }));
      },
      
      getAgentsForProject: (projectId) => {
        return get().library.agents.filter(
          (agent) =>
            agent.isShared ||
            (agent.projectIds || []).includes(projectId)
        );
      },
      
      setAgentShared: (agentId, shared) => {
        get().updateCustomAgent(agentId, { isShared: shared });
      },
      
      getSharedAgents: () => {
        return get().library.agents.filter((agent) => agent.isShared);
      },
      
      searchAgents: (query) => {
        const lowerQuery = query.toLowerCase();
        return get().library.agents.filter(
          (agent) =>
            agent.name.toLowerCase().includes(lowerQuery) ||
            agent.description?.toLowerCase().includes(lowerQuery) ||
            agent.tags.some((tag) => tag.toLowerCase().includes(lowerQuery)) ||
            agent.baseAgentType.toLowerCase().includes(lowerQuery)
        );
      },
      
      getAgentsByTag: (tag) => {
        return get().library.agents.filter((agent) => agent.tags.includes(tag));
      },
      
      getAgentsByBaseType: (baseType) => {
        return get().library.agents.filter((agent) => agent.baseAgentType === baseType);
      },
      
      getAllAvailableAgents: () => {
        return get().library.agents;
      },
    }),
    {
      name: STORAGE_KEY,
      version: 1,
      storage: createJSONStorage(() => localStorage),
    }
  )
);

