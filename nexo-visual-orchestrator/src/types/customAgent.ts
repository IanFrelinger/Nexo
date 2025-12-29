// src/types/customAgent.ts

import type { AgentType, AgentDefinition } from './agents';
import type { RoleDefinition } from './workflow';

export interface CustomAgentConfig {
  id: string;
  name: string; // User-friendly name
  description?: string;
  baseAgentType: AgentType; // Which built-in agent this is based on
  configuration: Record<string, unknown>; // Custom configuration overrides
  customBehaviors?: string[]; // Custom behavior IDs if any
  tags: string[];
  createdAt: string;
  updatedAt: string;
  projectIds?: string[]; // Which projects use this agent
  isShared?: boolean; // Can be used across all projects
  metadata?: {
    author?: string;
    version?: string;
    notes?: string;
  };
}

export interface CustomAgentLibrary {
  agents: CustomAgentConfig[];
  favorites: Set<string>; // Favorite agent IDs
  recent: string[]; // Recently used agent IDs (ordered)
}

// Convert custom agent config to role definition for use in workflows
export function customAgentToRole(
  customAgent: CustomAgentConfig,
  baseAgentDef: AgentDefinition,
  roleTemplate: any
): RoleDefinition {
  // Start with the role template
  const role: RoleDefinition = {
    ...roleTemplate,
    id: `custom-${customAgent.id}`,
    name: customAgent.name,
    description: customAgent.description || roleTemplate.description,
  };

  // Apply custom configuration overrides
  if (customAgent.configuration) {
    // Merge configuration into role properties
    Object.assign(role, customAgent.configuration);
  }

  return role;
}

