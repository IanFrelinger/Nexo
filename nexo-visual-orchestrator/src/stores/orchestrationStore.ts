import { create } from 'zustand';
import {
  applyNodeChanges,
  applyEdgeChanges,
  addEdge,
} from 'reactflow';
import type {
  OnNodesChange,
  OnEdgesChange,
  OnConnect,
  Connection,
  XYPosition,
} from 'reactflow';
import type { AgentNode, DependencyEdge } from '../types/workflow';
import type { AgentType } from '../types/agents';
import type { ExecutionStatus } from '../types/execution';
import { AGENT_REGISTRY } from '../utils/agentRegistry';
import { nanoid } from 'nanoid';

interface OrchestrationState {
  nodes: AgentNode[];
  edges: DependencyEdge[];
  selectedNodeId: string | null;

  // Actions
  addNode: (type: AgentType, position: XYPosition) => string;
  updateNodeConfig: (nodeId: string, config: Record<string, unknown>) => void;
  updateNodeLabel: (nodeId: string, label: string) => void;
  removeNode: (nodeId: string) => void;
  setNodeStatus: (nodeId: string, status: ExecutionStatus, progress?: number) => void;
  setNodeOutput: (nodeId: string, output: unknown) => void;
  setNodeError: (nodeId: string, error: string) => void;
  selectNode: (nodeId: string | null) => void;
  resetAllStatus: () => void;
  clearWorkflow: () => void;
  loadWorkflow: (nodes: AgentNode[], edges: DependencyEdge[]) => void;

  // React Flow handlers
  onNodesChange: OnNodesChange;
  onEdgesChange: OnEdgesChange;
  onConnect: OnConnect;
}

export const useOrchestrationStore = create<OrchestrationState>((set) => ({
  nodes: [],
  edges: [],
  selectedNodeId: null,

  addNode: (type: AgentType, position: XYPosition) => {
    const agentDef = AGENT_REGISTRY[type];
    const nodeId = `${type}_${nanoid(6)}`;

    const newNode: AgentNode = {
      id: nodeId,
      type: 'agentNode',
      position,
      data: {
        agentType: type,
        label: agentDef.label,
        config: { ...agentDef.defaultConfig },
        status: 'idle',
      },
    };

    set((state) => ({
      nodes: [...state.nodes, newNode],
    }));

    return nodeId;
  },

  updateNodeConfig: (nodeId: string, config: Record<string, unknown>) => {
    set((state) => ({
      nodes: state.nodes.map((node) =>
        node.id === nodeId
          ? { ...node, data: { ...node.data, config: { ...node.data.config, ...config } } }
          : node
      ),
    }));
  },

  updateNodeLabel: (nodeId: string, label: string) => {
    set((state) => ({
      nodes: state.nodes.map((node) =>
        node.id === nodeId ? { ...node, data: { ...node.data, label } } : node
      ),
    }));
  },

  removeNode: (nodeId: string) => {
    set((state) => ({
      nodes: state.nodes.filter((node) => node.id !== nodeId),
      edges: state.edges.filter((edge) => edge.source !== nodeId && edge.target !== nodeId),
      selectedNodeId: state.selectedNodeId === nodeId ? null : state.selectedNodeId,
    }));
  },

  setNodeStatus: (nodeId: string, status: ExecutionStatus, progress?: number) => {
    set((state) => ({
      nodes: state.nodes.map((node) =>
        node.id === nodeId
          ? {
              ...node,
              data: {
                ...node.data,
                status,
                progress: progress ?? node.data.progress,
                startTime: status === 'running' ? Date.now() : node.data.startTime,
                endTime: status === 'completed' || status === 'failed' ? Date.now() : undefined,
              },
            }
          : node
      ),
    }));
  },

  setNodeOutput: (nodeId: string, output: unknown) => {
    set((state) => ({
      nodes: state.nodes.map((node) =>
        node.id === nodeId ? { ...node, data: { ...node.data, output } } : node
      ),
    }));
  },

  setNodeError: (nodeId: string, error: string) => {
    set((state) => ({
      nodes: state.nodes.map((node) =>
        node.id === nodeId ? { ...node, data: { ...node.data, error } } : node
      ),
    }));
  },

  selectNode: (nodeId: string | null) => {
    set({ selectedNodeId: nodeId });
  },

  resetAllStatus: () => {
    set((state) => ({
      nodes: state.nodes.map((node) => ({
        ...node,
        data: {
          ...node.data,
          status: 'idle' as ExecutionStatus,
          progress: undefined,
          output: undefined,
          error: undefined,
          startTime: undefined,
          endTime: undefined,
        },
      })),
    }));
  },

  clearWorkflow: () => {
    set({ nodes: [], edges: [], selectedNodeId: null });
  },

  loadWorkflow: (nodes: AgentNode[], edges: DependencyEdge[]) => {
    set({ nodes, edges, selectedNodeId: null });
  },

  onNodesChange: (changes) => {
    set((state) => ({
      nodes: applyNodeChanges(changes, state.nodes) as AgentNode[],
    }));
  },

  onEdgesChange: (changes) => {
    set((state) => ({
      edges: applyEdgeChanges(changes, state.edges) as DependencyEdge[],
    }));
  },

  onConnect: (connection: Connection) => {
    const newEdge: DependencyEdge = {
      ...connection,
      id: `edge_${nanoid(6)}`,
      type: 'dataFlowEdge',
      data: {
        sourcePort: connection.sourceHandle || 'output',
        targetPort: connection.targetHandle || 'input',
      },
    } as DependencyEdge;

    set((state) => ({
      edges: addEdge(newEdge, state.edges) as DependencyEdge[],
    }));
  },
}));

