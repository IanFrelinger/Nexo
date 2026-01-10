// src/types/deck.ts

/**
 * Deck Type Definitions
 * 
 * Type definitions for the agent deck system. Decks are collections of agents
 * that can be saved, loaded, and reused across projects.
 * 
 * Includes:
 * - DeckAgent: Individual agent entry in a deck with count and configuration
 * - AgentDeck: Complete deck with metadata, tags, and sharing settings
 * - DeckTemplate: Predefined deck templates
 * - deckToWorkflow: Conversion function to transform decks into workflows
 */

import type { RoleDefinition, Relationship } from './workflow';
import type { AgentType } from './agents';

/**
 * Individual agent entry within a deck
 */
export interface DeckAgent {
  agentType: AgentType;
  roleId: string; // Reference to the role template
  count: number; // How many instances of this agent in the deck (default 1)
  configuration?: Record<string, unknown>; // Agent-specific configuration
}

/**
 * Complete agent deck with metadata and sharing settings
 */
export interface AgentDeck {
  id: string;
  name: string;
  description?: string;
  tags: string[];
  agents: DeckAgent[];
  createdAt: string;
  updatedAt: string;
  projectIds?: string[]; // Which projects are using this deck
  isShared?: boolean; // Can be used across all projects
  metadata?: {
    author?: string;
    version?: string;
    minAgents?: number;
    maxAgents?: number;
  };
}

/**
 * Predefined deck template for quick deck creation
 */
export interface DeckTemplate {
  id: string;
  name: string;
  description: string;
  category: 'starter' | 'specialized' | 'balanced' | 'custom';
  agents: DeckAgent[];
  recommendedFor?: string[];
}

/**
 * Converts an agent deck into workflow roles and relationships
 * @param deck - The agent deck to convert
 * @param roleTemplates - Template definitions for roles
 * @returns Object containing roles and relationships for the workflow
 */
export function deckToWorkflow(deck: AgentDeck, roleTemplates: Record<string, any>): {
  roles: RoleDefinition[];
  relationships: Relationship[];
} {
  const roles: RoleDefinition[] = [];
  const relationships: Relationship[] = [];
  
  deck.agents.forEach((deckAgent, index) => {
    const template = roleTemplates[deckAgent.roleId];
    if (!template) return;
    
    // Create role instances based on count
    for (let i = 0; i < deckAgent.count; i++) {
      const role: RoleDefinition = {
        ...template,
        id: `${deckAgent.roleId}-${deck.id}-${index}-${i}`,
        position: {
          x: 100 + (index * 320) + (i * 50),
          y: 100 + (i * 250),
        },
      };
      
      // Apply configuration if provided
      if (deckAgent.configuration) {
        // Merge configuration into role
        Object.assign(role, deckAgent.configuration);
      }
      
      roles.push(role);
    }
  });
  
  // Generate default relationships (architect delegates to others)
  const architect = roles.find(r => r.role.toLowerCase().includes('architect'));
  if (architect) {
    roles.forEach(role => {
      if (role.id !== architect.id && role.modelConfig.tier !== 'strategic') {
        relationships.push({
          id: `rel-${architect.id}-${role.id}`,
          sourceRoleId: architect.id,
          targetRoleId: role.id,
          type: 'delegates',
          metadata: { label: 'Delegation from architect' },
        });
      }
    });
  }
  
  return { roles, relationships };
}

