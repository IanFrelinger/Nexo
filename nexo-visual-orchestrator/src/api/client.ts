const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface Brick {
  id: string;
  name: string;
  version: string;
  icon: string;
  category: string;
  description: string;
  domainKnowledge: {
    standards: string[];
    rules: { id: string; description: string; severity?: string }[];
    learnedPatterns: { pattern: string; confidence: number }[];
    executionCount: number;
  };
  interface: {
    inputs: { name: string; type: string; required: boolean }[];
    outputs: { name: string; type: string }[];
  };
  implementations: {
    deterministic?: { id: string; name: string; characteristics: { latency: string } };
    agentic?: { id: string; name: string; characteristics: { latency: string } };
  };
  defaultImplementation: 'deterministic' | 'agentic';
}

export interface Behavior {
  id: string;
  name: string;
  version: string;
  description: string;
  steps: {
    id: string;
    brickId: string;
    implementation: 'deterministic' | 'agentic' | 'auto';
  }[];
  inputs: { name: string; type: string; required: boolean }[];
  outputs: { name: string; type: string }[];
}

export interface Agent {
  id: string;
  name: string;
  version: string;
  icon: string;
  domain: string;
  description: string;
  platforms: string[];
  behaviors: string[];
  memory: { semanticCache: boolean; patternLibrary: boolean };
}

export const api = {
  async getBricks(): Promise<Brick[]> {
    const res = await fetch(`${API_BASE}/api/catalog/bricks`);
    return res.json();
  },
  
  async getBrick(id: string): Promise<Brick> {
    const res = await fetch(`${API_BASE}/api/catalog/bricks/${id}`);
    return res.json();
  },
  
  async getBehaviors(): Promise<Behavior[]> {
    const res = await fetch(`${API_BASE}/api/catalog/behaviors`);
    return res.json();
  },
  
  async getAgents(): Promise<Agent[]> {
    const res = await fetch(`${API_BASE}/api/catalog/agents`);
    return res.json();
  },
  
  async getClusters(tag?: string, scope?: string): Promise<Cluster[]> {
    const params = new URLSearchParams();
    if (tag) params.append('tag', tag);
    if (scope) params.append('scope', scope);
    const query = params.toString();
    const res = await fetch(`${API_BASE}/api/clusters${query ? `?${query}` : ''}`);
    return res.json();
  },
  
  async getCluster(id: string): Promise<ClusterDetail> {
    const res = await fetch(`${API_BASE}/api/clusters/${id}`);
    return res.json();
  },
  
  async createCluster(request: CreateClusterRequest): Promise<ClusterDetail> {
    const res = await fetch(`${API_BASE}/api/clusters`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    return res.json();
  },
  
  async exportWorkflow(request: ExportRequest): Promise<ExportResult> {
    const res = await fetch(`${API_BASE}/api/export`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    return res.json();
  },
  
  async downloadExport(request: ExportRequest): Promise<Blob> {
    const res = await fetch(`${API_BASE}/api/export/download`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    return res.blob();
  },
};

export interface Cluster {
  id: string;
  name: string;
  icon: string;
  description: string;
  brickCount: number;
  tags: string[];
  author: string;
  scope: string;
  usageCount: number;
}

export interface ClusterDetail extends Cluster {
  version: string;
  bricks: ClusterBrick[];
  connections: ClusterConnection[];
  interface: ClusterInterface;
  parameters: ClusterParameter[];
  scaling: ScalingConfig;
  metadata: ClusterMetadata;
}

export interface ClusterBrick {
  brickId: string;
  localId: string;
  x: number;
  y: number;
  defaultImplementation: string;
  allowImplementationOverride: boolean;
  allowParameterOverride: boolean;
}

export interface ClusterConnection {
  id: string;
  fromBrickId: string;
  fromOutput: string;
  toBrickId: string;
  toInput: string;
  condition?: string;
  type: string;
}

export interface ClusterInterface {
  inputs: ClusterPort[];
  outputs: ClusterPort[];
  failurePolicy: string;
}

export interface ClusterPort {
  name: string;
  type: string;
  description: string;
  required: boolean;
  default?: unknown;
  internalMapping: string;
}

export interface ClusterParameter {
  name: string;
  displayName: string;
  description: string;
  type: string;
  default?: unknown;
  min?: unknown;
  max?: unknown;
  allowedValues?: string[];
}

export interface ScalingConfig {
  mode: string;
  fixedCount: number;
  dynamicExpression?: string;
  maxInstances: number;
  minInstances: number;
}

export interface ClusterMetadata {
  author: string;
  createdAt: string;
  modifiedAt: string;
  scope: string;
  stats: ClusterStats;
}

export interface ClusterStats {
  usageCount: number;
  favoriteCount: number;
  averageRating: number;
}

export interface CreateClusterRequest {
  name: string;
  description: string;
  icon?: string;
  bricks: CreateClusterBrickRequest[];
  connections: CreateClusterConnectionRequest[];
  inputs: CreateClusterPortRequest[];
  outputs: CreateClusterPortRequest[];
  parameters: CreateClusterParameterRequest[];
  scalingMode: string;
  fixedCount?: number;
  maxInstances?: number;
  tags: string[];
  scope?: string;
}

export interface CreateClusterBrickRequest {
  brickId: string;
  localId: string;
  x: number;
  y: number;
  defaultImplementation: string;
  allowImplementationOverride: boolean;
}

export interface CreateClusterConnectionRequest {
  fromBrickId: string;
  fromOutput: string;
  toBrickId: string;
  toInput: string;
  condition?: string;
  type: string;
}

export interface CreateClusterPortRequest {
  name: string;
  type: string;
  description: string;
  required: boolean;
  internalMapping: string;
}

export interface CreateClusterParameterRequest {
  name: string;
  displayName: string;
  type: string;
  default?: unknown;
  min?: unknown;
  max?: unknown;
}

export interface ExportRequest {
  workflow: ExportWorkflowRequest;
  mode: string;
  target: string;
  generationConfig?: ExportGenerationConfig;
  includeFallbacks: boolean;
  outputFormat: string;
  namespace?: string;
}

export interface ExportWorkflowRequest {
  id?: string;
  name: string;
  description: string;
  instances: ExportInstanceRequest[];
  connections: ExportConnectionRequest[];
}

export interface ExportInstanceRequest {
  clusterId: string;
  instanceId: string;
  name?: string;
  parameters: Record<string, unknown>;
  implementationOverrides: Record<string, string>;
}

export interface ExportConnectionRequest {
  id: string;
  fromInstanceId: string;
  fromOutput: string;
  toInstanceId: string;
  toInput: string;
}

export interface ExportGenerationConfig {
  variationsPerItem: number;
  provider: string;
  model: string;
  requireReview: boolean;
}

export interface ExportResult {
  success: boolean;
  mode: string;
  target: string;
  files: ExportedFile[];
  generationSummary?: GenerationSummary;
  runtimeRequirements: string[];
  messages: string[];
}

export interface ExportedFile {
  path: string;
  content: string;
  language: string;
}

export interface GenerationSummary {
  itemsGenerated: number;
  variationsCreated: number;
}

