// src/components/Canvas/OrchestrationCanvasEdgeHandlers.ts

import { useCallback } from 'react';
import type { Connection } from 'reactflow';
import { nanoid } from 'nanoid';
import { useOrchestrationStore } from '../../stores/orchestrationStore';
import type { Relationship } from '../../types/workflow';

export function useEdgeHandlers(
  addRelationship: (relationship: Relationship) => void,
) {
  const onEdgesChange = useCallback((changes: any) => {
    changes.forEach((change: any) => {
      if (change.type === 'remove') {
        useOrchestrationStore.getState().removeRelationship(change.id);
      }
    });
  }, []);

  const onConnect = useCallback((connection: Connection) => {
    const isNegotiation = connection.sourceHandle === 'negotiate-out' || connection.targetHandle === 'negotiate-in';
    const type: Relationship['type'] = isNegotiation ? 'negotiates' : 'delegates';

    const newRelationship: Relationship = {
      id: `rel-${nanoid(6)}`,
      sourceRoleId: connection.source!,
      targetRoleId: connection.target!,
      type,
      metadata: isNegotiation ? { topics: [] } : undefined,
    };

    addRelationship(newRelationship);
  }, [addRelationship]);

  return { onEdgesChange, onConnect };
}

