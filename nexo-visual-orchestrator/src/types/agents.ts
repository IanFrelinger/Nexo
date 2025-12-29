export type AgentCategory =
  | 'architect'
  | 'domain'
  | 'asset'
  | 'build'
  | 'playtest'
  | 'analysis';

export type AgentType =
  | 'architect'
  | 'combat'
  | 'economy'
  | 'ai-behavior'
  | 'level-design'
  | 'narrative'
  | 'image-gen'
  | 'audio-gen'
  | 'model-3d-gen'
  | 'unity-build'
  | 'ai-player'
  | 'balance-analyzer'
  | 'feedback-synthesizer';

export interface PortDefinition {
  id: string;
  label: string;
  dataType: 'json' | 'binary' | 'text' | 'any';
  required: boolean;
  multiple: boolean;
}

export interface ConfigProperty {
  type: 'string' | 'number' | 'boolean' | 'select' | 'multiselect' | 'textarea';
  label: string;
  description?: string;
  default?: unknown;
  options?: { value: string; label: string }[];
  min?: number;
  max?: number;
  step?: number;
}

export interface ConfigSchema {
  properties: Record<string, ConfigProperty>;
  required: string[];
}

export interface Command {
  id: string;
  label: string;
  description?: string;
  icon?: string;
}

export interface Behavior {
  id: string;
  label: string;
  description?: string;
  icon?: string;
  commands: Command[];
}

export interface AgentDefinition {
  type: AgentType;
  category: AgentCategory;
  label: string;
  description: string;
  icon: string;
  color: string;
  inputs: PortDefinition[];
  outputs: PortDefinition[];
  configSchema: ConfigSchema;
  defaultConfig: Record<string, unknown>;
  behaviors?: Behavior[];
}

