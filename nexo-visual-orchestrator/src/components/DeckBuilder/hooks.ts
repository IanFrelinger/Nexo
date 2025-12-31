// src/components/DeckBuilder/hooks.ts

/**
 * Custom hooks for the DeckBuilder component
 * 
 * Provides reusable hooks for managing agent data, filtering, and deck agent
 * transformations used throughout the deck builder interface.
 */

import { useMemo } from 'react';
import { useCustomAgentStore } from '../../stores/customAgentStore';
import { AGENT_REGISTRY } from '../../utils/agentRegistry';
import { ROLE_TEMPLATES } from '../../data/roleTemplates';
import type { AgentType } from '../../types/agents';

interface AgentItem {
  type: AgentType;
  definition: any;
  template: any;
  isCustom?: boolean;
  customAgent?: any;
  customId?: string;
}

/**
 * Hook to get available agents based on source type
 * @param agentSource - 'built-in' for system agents or 'custom' for user-created agents
 * @returns Array of agent items with definitions and templates
 */
export function useAvailableAgents(agentSource: 'built-in' | 'custom') {
  const { library: customAgentLibrary } = useCustomAgentStore();

  return useMemo(() => {
    if (agentSource === 'custom') {
      return customAgentLibrary.agents.map((customAgent) => {
        const baseDef = AGENT_REGISTRY[customAgent.baseAgentType];
        const template = ROLE_TEMPLATES[customAgent.baseAgentType];
        return {
          type: customAgent.baseAgentType as AgentType,
          definition: baseDef,
          template,
          customAgent,
          isCustom: true,
          customId: customAgent.id,
        };
      }).filter(item => item.template);
    } else {
      return Object.entries(AGENT_REGISTRY).map(([type, def]) => ({
        type: type as AgentType,
        definition: def,
        template: ROLE_TEMPLATES[type],
        isCustom: false,
      })).filter(item => item.template);
    }
  }, [agentSource, customAgentLibrary.agents]);
}

/**
 * Hook to filter agents based on search query
 * @param availableAgents - Array of available agents to filter
 * @param searchQuery - Search string to match against agent names, descriptions, types, and tags
 * @returns Filtered array of agents matching the search query
 */
export function useFilteredAgents(availableAgents: AgentItem[], searchQuery: string) {
  return useMemo(() => {
    if (!searchQuery) return availableAgents;
    const query = searchQuery.toLowerCase();
    return availableAgents.filter((item) => {
      const name = item.isCustom && item.customAgent
        ? item.customAgent.name
        : item.definition.label;
      const description = item.isCustom && item.customAgent?.description
        ? item.customAgent.description
        : item.definition.description;
      
      return (
        name.toLowerCase().includes(query) ||
        description.toLowerCase().includes(query) ||
        item.type.toLowerCase().includes(query) ||
        (item.customAgent?.tags || []).some((tag: string) => tag.toLowerCase().includes(query))
      );
    });
  }, [availableAgents, searchQuery]);
}

/**
 * Hook to transform deck agents into display format with definitions
 * @param currentDeck - The currently selected deck
 * @returns Array of deck agents enriched with agent definitions and templates
 */
export function useDeckAgents(currentDeck: any) {
  return useMemo(() => {
    if (!currentDeck) return [];
    return currentDeck.agents.map((deckAgent: any) => {
      const agentDef = AGENT_REGISTRY[deckAgent.agentType];
      const template = ROLE_TEMPLATES[deckAgent.roleId];
      return {
        ...deckAgent,
        definition: agentDef,
        template,
      };
    });
  }, [currentDeck]);
}

