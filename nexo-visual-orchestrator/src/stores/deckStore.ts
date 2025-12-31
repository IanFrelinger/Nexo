// src/stores/deckStore.ts

/**
 * Deck Store
 * 
 * Zustand store with persistence for managing agent decks. Decks are collections
 * of agents that can be saved, loaded, and reused across projects.
 * 
 * Features:
 * - Create, update, delete, and duplicate decks
 * - Add/remove agents from decks with configurable counts
 * - Associate decks with projects
 * - Share decks across projects
 * - Search and filter decks by tags
 * - Persist to localStorage for cross-session persistence
 * 
 * Used by the DeckBuilder component and for loading decks onto the canvas.
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { nanoid } from 'nanoid';
import type { AgentDeck, DeckAgent } from '../types/deck';
import type { AgentType } from '../types/agents';

/**
 * Deck store state interface
 */
interface DeckStoreState {
  decks: AgentDeck[];
  currentDeck: AgentDeck | null;
  selectedDeckId: string | null;
  
  // Actions
  createDeck: (name: string, description?: string, tags?: string[]) => AgentDeck;
  updateDeck: (deckId: string, updates: Partial<AgentDeck>) => void;
  deleteDeck: (deckId: string) => void;
  duplicateDeck: (deckId: string) => AgentDeck;
  
  // Deck agents
  addAgentToDeck: (deckId: string, agent: DeckAgent) => void;
  removeAgentFromDeck: (deckId: string, agentType: AgentType) => void;
  updateAgentInDeck: (deckId: string, agentType: AgentType, updates: Partial<DeckAgent>) => void;
  setAgentCount: (deckId: string, agentType: AgentType, count: number) => void;
  
  // Selection
  selectDeck: (deckId: string | null) => void;
  loadDeck: (deckId: string) => AgentDeck | null;
  
  // Project association
  associateDeckWithProject: (deckId: string, projectId: string) => void;
  removeDeckFromProject: (deckId: string, projectId: string) => void;
  getDecksForProject: (projectId: string) => AgentDeck[];
  
  // Sharing
  setDeckShared: (deckId: string, shared: boolean) => void;
  getSharedDecks: () => AgentDeck[];
  
  // Search and filter
  searchDecks: (query: string) => AgentDeck[];
  getDecksByTag: (tag: string) => AgentDeck[];
}

const STORAGE_KEY = 'nexo-deck-store';

export const useDeckStore = create<DeckStoreState>()(
  persist(
    (set, get) => ({
      decks: [],
      currentDeck: null,
      selectedDeckId: null,
      
      createDeck: (name, description, tags = []) => {
        const newDeck: AgentDeck = {
          id: nanoid(),
          name,
          description,
          tags,
          agents: [],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          isShared: false,
          metadata: {
            version: '1.0.0',
          },
        };
        
        set((state) => ({
          decks: [...state.decks, newDeck],
          currentDeck: newDeck,
          selectedDeckId: newDeck.id,
        }));
        
        return newDeck;
      },
      
      updateDeck: (deckId, updates) => {
        set((state) => ({
          decks: state.decks.map((deck) =>
            deck.id === deckId
              ? { ...deck, ...updates, updatedAt: new Date().toISOString() }
              : deck
          ),
          currentDeck:
            state.currentDeck?.id === deckId
              ? { ...state.currentDeck, ...updates, updatedAt: new Date().toISOString() }
              : state.currentDeck,
        }));
      },
      
      deleteDeck: (deckId) => {
        set((state) => ({
          decks: state.decks.filter((deck) => deck.id !== deckId),
          currentDeck: state.currentDeck?.id === deckId ? null : state.currentDeck,
          selectedDeckId: state.selectedDeckId === deckId ? null : state.selectedDeckId,
        }));
      },
      
      duplicateDeck: (deckId) => {
        const deck = get().decks.find((d) => d.id === deckId);
        if (!deck) {
          throw new Error(`Deck not found: ${deckId}`);
        }
        
        const duplicated: AgentDeck = {
          ...deck,
          id: nanoid(),
          name: `${deck.name} (Copy)`,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          projectIds: [], // Don't copy project associations
        };
        
        set((state) => ({
          decks: [...state.decks, duplicated],
        }));
        
        return duplicated;
      },
      
      addAgentToDeck: (deckId, agent) => {
        set((state) => ({
          decks: state.decks.map((deck) => {
            if (deck.id !== deckId) return deck;
            
            // Check if agent already exists
            const existingIndex = deck.agents.findIndex(
              (a) => a.agentType === agent.agentType
            );
            
            if (existingIndex >= 0) {
              // Update count if exists
              const updatedAgents = [...deck.agents];
              updatedAgents[existingIndex] = {
                ...updatedAgents[existingIndex],
                count: updatedAgents[existingIndex].count + (agent.count || 1),
              };
              return {
                ...deck,
                agents: updatedAgents,
                updatedAt: new Date().toISOString(),
              };
            }
            
            return {
              ...deck,
              agents: [...deck.agents, { ...agent, count: agent.count || 1 }],
              updatedAt: new Date().toISOString(),
            };
          }),
          currentDeck:
            state.currentDeck?.id === deckId
              ? {
                  ...state.currentDeck,
                  agents: state.currentDeck.agents.some(
                    (a) => a.agentType === agent.agentType
                  )
                    ? state.currentDeck.agents.map((a) =>
                        a.agentType === agent.agentType
                          ? { ...a, count: a.count + (agent.count || 1) }
                          : a
                      )
                    : [...state.currentDeck.agents, { ...agent, count: agent.count || 1 }],
                  updatedAt: new Date().toISOString(),
                }
              : state.currentDeck,
        }));
      },
      
      removeAgentFromDeck: (deckId, agentType) => {
        set((state) => ({
          decks: state.decks.map((deck) =>
            deck.id === deckId
              ? {
                  ...deck,
                  agents: deck.agents.filter((a) => a.agentType !== agentType),
                  updatedAt: new Date().toISOString(),
                }
              : deck
          ),
          currentDeck:
            state.currentDeck?.id === deckId
              ? {
                  ...state.currentDeck,
                  agents: state.currentDeck.agents.filter(
                    (a) => a.agentType !== agentType
                  ),
                  updatedAt: new Date().toISOString(),
                }
              : state.currentDeck,
        }));
      },
      
      updateAgentInDeck: (deckId, agentType, updates) => {
        set((state) => ({
          decks: state.decks.map((deck) =>
            deck.id === deckId
              ? {
                  ...deck,
                  agents: deck.agents.map((a) =>
                    a.agentType === agentType ? { ...a, ...updates } : a
                  ),
                  updatedAt: new Date().toISOString(),
                }
              : deck
          ),
          currentDeck:
            state.currentDeck?.id === deckId
              ? {
                  ...state.currentDeck,
                  agents: state.currentDeck.agents.map((a) =>
                    a.agentType === agentType ? { ...a, ...updates } : a
                  ),
                  updatedAt: new Date().toISOString(),
                }
              : state.currentDeck,
        }));
      },
      
      setAgentCount: (deckId, agentType, count) => {
        if (count < 1) {
          get().removeAgentFromDeck(deckId, agentType);
          return;
        }
        
        get().updateAgentInDeck(deckId, agentType, { count });
      },
      
      selectDeck: (deckId) => {
        const deck = get().decks.find((d) => d.id === deckId);
        set({
          selectedDeckId: deckId,
          currentDeck: deck || null,
        });
      },
      
      loadDeck: (deckId) => {
        const deck = get().decks.find((d) => d.id === deckId);
        if (deck) {
          set({
            currentDeck: deck,
            selectedDeckId: deckId,
          });
        }
        return deck || null;
      },
      
      associateDeckWithProject: (deckId, projectId) => {
        set((state) => ({
          decks: state.decks.map((deck) =>
            deck.id === deckId
              ? {
                  ...deck,
                  projectIds: [
                    ...(deck.projectIds || []),
                    projectId,
                  ].filter((id, index, arr) => arr.indexOf(id) === index), // Unique
                  updatedAt: new Date().toISOString(),
                }
              : deck
          ),
        }));
      },
      
      removeDeckFromProject: (deckId, projectId) => {
        set((state) => ({
          decks: state.decks.map((deck) =>
            deck.id === deckId
              ? {
                  ...deck,
                  projectIds: (deck.projectIds || []).filter(
                    (id) => id !== projectId
                  ),
                  updatedAt: new Date().toISOString(),
                }
              : deck
          ),
        }));
      },
      
      getDecksForProject: (projectId) => {
        return get().decks.filter(
          (deck) =>
            deck.isShared ||
            (deck.projectIds || []).includes(projectId)
        );
      },
      
      setDeckShared: (deckId, shared) => {
        get().updateDeck(deckId, { isShared: shared });
      },
      
      getSharedDecks: () => {
        return get().decks.filter((deck) => deck.isShared);
      },
      
      searchDecks: (query) => {
        const lowerQuery = query.toLowerCase();
        return get().decks.filter(
          (deck) =>
            deck.name.toLowerCase().includes(lowerQuery) ||
            deck.description?.toLowerCase().includes(lowerQuery) ||
            deck.tags.some((tag) => tag.toLowerCase().includes(lowerQuery))
        );
      },
      
      getDecksByTag: (tag) => {
        return get().decks.filter((deck) => deck.tags.includes(tag));
      },
    }),
    {
      name: STORAGE_KEY,
      version: 1,
      storage: createJSONStorage(() => localStorage),
    }
  )
);

