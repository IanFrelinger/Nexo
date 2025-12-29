// src/components/Canvas/OrchestrationCanvasEdges.ts

import { useMemo } from 'react';
import type { Edge } from 'reactflow';
import type { Relationship } from '../../types/workflow';
import type { RelationshipEdgeData } from '../../types/workflow';

export function useOrchestrationEdges(
  relationships: Relationship[],
  selectedRelationshipId: string | null,
  visibleRelationshipTypes: Set<string>,
  highlightedPath: string[] | null,
): Edge<RelationshipEdgeData>[] {
  return useMemo(() => {
    return relationships
      .filter(rel => visibleRelationshipTypes.has(rel.type))
      .map((rel) => {
        const isInPath = highlightedPath && (
          highlightedPath.includes(rel.sourceRoleId) && 
          highlightedPath.includes(rel.targetRoleId) &&
          Math.abs(highlightedPath.indexOf(rel.sourceRoleId) - highlightedPath.indexOf(rel.targetRoleId)) === 1
        );
        
        return {
          id: rel.id,
          source: rel.sourceRoleId,
          target: rel.targetRoleId,
          type: 'relationship',
          data: {
            relationship: rel,
            isActive: true,
            trafficVolume: 1,
            isHighlighted: isInPath || false,
          } as RelationshipEdgeData,
          sourceHandle: rel.type === 'negotiates' ? 'negotiate-out' : undefined,
          targetHandle: rel.type === 'negotiates' ? 'negotiate-in' : undefined,
          selected: rel.id === selectedRelationshipId,
          style: isInPath ? {
            strokeWidth: 4,
            stroke: '#fbbf24',
            opacity: 1,
          } : undefined,
        };
      });
  }, [relationships, selectedRelationshipId, visibleRelationshipTypes, highlightedPath]);
}

