import * as signalR from '@microsoft/signalr';

export interface BehaviorStartedEvent {
  behaviorId: string;
  behaviorName: string;
  timestamp: string;
}

export interface StepStartedEvent {
  stepId: string;
  brickId: string;
  brickName: string;
  implementation: 'deterministic' | 'agentic';
  usedFallback: boolean;
  stepIndex: number;
  totalSteps: number;
}

export interface StepCompletedEvent {
  stepId: string;
  brickId: string;
  implementation: 'deterministic' | 'agentic';
  latencyMs: number;
  summary?: string;
}

export interface StepErrorEvent {
  stepId: string;
  error: string;
  latencyMs?: number;
}

export interface CacheHitEvent {
  stepId: string;
  cacheKey: string;
}

export interface BehaviorCompletedEvent {
  behaviorId: string;
  success: boolean;
  outputs: Record<string, unknown>;
}

export interface ProviderSwitchedEvent {
  fromProvider: string;
  toProvider: string;
}

export interface OfflineModeActivatedEvent {
}

export type ExecutionEventHandler = {
  onBehaviorStarted?: (evt: BehaviorStartedEvent) => void;
  onStepStarted?: (evt: StepStartedEvent) => void;
  onStepCompleted?: (evt: StepCompletedEvent) => void;
  onStepError?: (evt: StepErrorEvent) => void;
  onCacheHit?: (evt: CacheHitEvent) => void;
  onBehaviorCompleted?: (evt: BehaviorCompletedEvent) => void;
  onProviderSwitched?: (evt: ProviderSwitchedEvent) => void;
  onOfflineModeActivated?: (evt: OfflineModeActivatedEvent) => void;
};

export interface ExecuteBehaviorRequest {
  agentId: string;
  behaviorId: string;
  parameters: Record<string, unknown>;
  isOffline?: boolean;
  auditMode?: boolean;
  provider?: string;
}

export class ExecutionSocket {
  private connection: signalR.HubConnection;
  private handlers: ExecutionEventHandler = {};
  
  constructor(url: string = 'http://localhost:5000/hubs/execution') {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build();
    
    this.setupHandlers();
  }
  
  private setupHandlers() {
    this.connection.on('OnBehaviorStarted', (evt) => this.handlers.onBehaviorStarted?.(evt));
    this.connection.on('OnStepStarted', (evt) => this.handlers.onStepStarted?.(evt));
    this.connection.on('OnStepCompleted', (evt) => this.handlers.onStepCompleted?.(evt));
    this.connection.on('OnStepError', (evt) => this.handlers.onStepError?.(evt));
    this.connection.on('OnCacheHit', (evt) => this.handlers.onCacheHit?.(evt));
    this.connection.on('OnBehaviorCompleted', (evt) => this.handlers.onBehaviorCompleted?.(evt));
    this.connection.on('OnProviderSwitched', (evt) => this.handlers.onProviderSwitched?.(evt));
    this.connection.on('OnOfflineModeActivated', (evt) => this.handlers.onOfflineModeActivated?.(evt));
  }
  
  setHandlers(handlers: ExecutionEventHandler) {
    this.handlers = handlers;
  }
  
  async connect() {
    await this.connection.start();
  }
  
  async disconnect() {
    await this.connection.stop();
  }
  
  async executeBehavior(request: ExecuteBehaviorRequest) {
    await this.connection.invoke('ExecuteBehavior', request);
  }
  
  async switchProvider(provider: string) {
    await this.connection.invoke('SwitchProvider', provider);
  }
  
  async setOfflineMode(offline: boolean) {
    await this.connection.invoke('SetOfflineMode', offline);
  }
  
  async setBrickImplementation(brickId: string, implementation: 'deterministic' | 'agentic') {
    await this.connection.invoke('SetBrickImplementation', brickId, implementation);
  }
}

