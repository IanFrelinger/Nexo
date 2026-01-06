import { useCallback, useEffect, useRef, useState } from 'react';
import { ExecutionSocket, ExecutionEventHandler } from '../api/executionSocket';

interface ExecutionState {
  status: 'idle' | 'running' | 'complete' | 'error';
  currentStep?: string;
  completedSteps: string[];
  errors: { stepId: string; message: string }[];
  metrics: {
    totalLatencyMs: number;
    cacheHits: number;
    deterministicSteps: number;
    agenticSteps: number;
  };
}

export function useExecution() {
  const socketRef = useRef<ExecutionSocket | null>(null);
  const [state, setState] = useState<ExecutionState>({
    status: 'idle',
    completedSteps: [],
    errors: [],
    metrics: { totalLatencyMs: 0, cacheHits: 0, deterministicSteps: 0, agenticSteps: 0 },
  });
  const [isOffline, setIsOffline] = useState(false);
  const [provider, setProvider] = useState('openai');
  
  useEffect(() => {
    const socket = new ExecutionSocket();
    socketRef.current = socket;
    
    const handlers: ExecutionEventHandler = {
      onBehaviorStarted: () => {
        setState(s => ({ ...s, status: 'running', completedSteps: [], errors: [] }));
      },
      onStepStarted: (evt) => {
        setState(s => ({ ...s, currentStep: evt.stepId }));
      },
      onStepCompleted: (evt) => {
        setState(s => ({
          ...s,
          completedSteps: [...s.completedSteps, evt.stepId],
          currentStep: undefined,
          metrics: {
            ...s.metrics,
            totalLatencyMs: s.metrics.totalLatencyMs + evt.latencyMs,
            deterministicSteps: s.metrics.deterministicSteps + (evt.implementation === 'deterministic' ? 1 : 0),
            agenticSteps: s.metrics.agenticSteps + (evt.implementation === 'agentic' ? 1 : 0),
          },
        }));
      },
      onStepError: (evt) => {
        setState(s => ({
          ...s,
          errors: [...s.errors, { stepId: evt.stepId, message: evt.error }],
          status: 'error',
        }));
      },
      onCacheHit: () => {
        setState(s => ({
          ...s,
          metrics: { ...s.metrics, cacheHits: s.metrics.cacheHits + 1 },
        }));
      },
      onBehaviorCompleted: () => {
        setState(s => ({ ...s, status: 'complete', currentStep: undefined }));
      },
      onOfflineModeActivated: () => {
        setIsOffline(true);
      },
    };
    
    socket.setHandlers(handlers);
    socket.connect();
    
    return () => {
      socket.disconnect();
    };
  }, []);
  
  const execute = useCallback(async (agentId: string, behaviorId: string, params: Record<string, unknown>) => {
    if (!socketRef.current) return;
    
    setState({
      status: 'idle',
      completedSteps: [],
      errors: [],
      metrics: { totalLatencyMs: 0, cacheHits: 0, deterministicSteps: 0, agenticSteps: 0 },
    });
    
    await socketRef.current.executeBehavior({
      agentId,
      behaviorId,
      parameters: params,
      isOffline: isOffline,
      provider,
    });
  }, [isOffline, provider]);
  
  const toggleOffline = useCallback(async () => {
    if (!socketRef.current) return;
    await socketRef.current.setOfflineMode(!isOffline);
    setIsOffline(!isOffline);
  }, [isOffline]);
  
  const switchProvider = useCallback(async (newProvider: string) => {
    if (!socketRef.current) return;
    await socketRef.current.switchProvider(newProvider);
    setProvider(newProvider);
  }, []);
  
  const setBrickImplementation = useCallback(async (brickId: string, impl: 'deterministic' | 'agentic') => {
    if (!socketRef.current) return;
    await socketRef.current.setBrickImplementation(brickId, impl);
  }, []);
  
  return {
    state,
    isOffline,
    provider,
    execute,
    toggleOffline,
    switchProvider,
    setBrickImplementation,
  };
}

