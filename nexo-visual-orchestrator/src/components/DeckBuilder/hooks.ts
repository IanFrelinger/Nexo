// src/components/DeckBuilder/hooks.ts

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

