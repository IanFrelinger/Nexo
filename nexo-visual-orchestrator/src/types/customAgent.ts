// src/types/customAgent.ts

/**
 * Custom Agent Type Definitions
 * 
 * Type definitions for user-created custom agents. Custom agents are based
 * on built-in agent types but with user-defined configurations, names, and tags.
 * 
 * Includes:
 * - CustomAgentConfig: Individual custom agent configuration
 * - CustomAgentLibrary: Library structure with favorites and recent tracking
 * - customAgentToRole: Conversion function to transform custom agents into roles
 */

import type { AgentType, AgentDefinition } from './agents';
import type { RoleDefinition } from './workflow';

/**
 * User-created custom agent configuration
 */
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

/**
 * Custom agent library structure with favorites and recent tracking
 */
export interface CustomAgentLibrary {
  agents: CustomAgentConfig[];
  favorites: Set<string>; // Favorite agent IDs
  recent: string[]; // Recently used agent IDs (ordered)
}

/**
 * Converts a custom agent configuration into a role definition for use in workflows
 * @param customAgent - The custom agent configuration
 * @param baseAgentDef - The base agent definition
 * @param roleTemplate - The role template to use as a base
 * @returns Role definition ready for use in workflows
 */
export function customAgentToRole(
  customAgent: CustomAgentConfig,
  _baseAgentDef: AgentDefinition,
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

