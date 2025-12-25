// src/components/Edges/RelationshipEdge.tsx

import { memo } from 'react';
import { getBezierPath, EdgeLabelRenderer } from 'reactflow';
import type { EdgeProps } from 'reactflow';
import type { RelationshipEdgeData } from '../../types/workflow';
import { HiSwitchHorizontal } from 'react-icons/hi';

const EDGE_STYLES = {
  delegates: {
    stroke: '#64748b',           // slate-500
    strokeWidth: 2,
    strokeDasharray: 'none',
    markerEnd: 'url(#arrow-delegates)',
  },
  'reports-to': {
    stroke: '#8b5cf6',           // purple-500
    strokeWidth: 2,
    strokeDasharray: 'none',
    markerEnd: 'url(#arrow-reports)',
  },
  negotiates: {
    stroke: '#eab308',           // yellow-500
    strokeWidth: 2,
    strokeDasharray: '5,5',
    markerEnd: 'none',
  },
  observes: {
    stroke: '#64748b',           // slate-500
    strokeWidth: 1,
    strokeDasharray: '3,3',
    markerEnd: 'none',
  },
};

function RelationshipEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  style = {},
}: EdgeProps<RelationshipEdgeData>) {
  const relationship = data?.relationship;
  const type = relationship?.type || 'delegates';
  const edgeStyle = EDGE_STYLES[type];
  
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });
  
  const topics = relationship?.metadata?.topics;
  
  return (
    <>
      {/* SVG Definitions for markers */}
      <defs>
        <marker
          id="arrow-delegates"
          viewBox="0 0 10 10"
          refX="8"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill="#64748b" />
        </marker>
        <marker
          id="arrow-reports"
          viewBox="0 0 10 10"
          refX="8"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill="#8b5cf6" />
        </marker>
      </defs>
      
      <path
        id={id}
        className="react-flow__edge-path"
        d={edgePath}
        style={{
          ...style,
          stroke: edgeStyle.stroke,
          strokeWidth: edgeStyle.strokeWidth,
          strokeDasharray: edgeStyle.strokeDasharray,
        }}
        markerEnd={edgeStyle.markerEnd}
      />
      
      {/* Label for negotiation topics */}
      {type === 'negotiates' && topics && topics.length > 0 && (
        <EdgeLabelRenderer>
          <div
            className="absolute bg-yellow-500/20 text-yellow-300 text-xs px-2 py-1 rounded border border-yellow-500/30 pointer-events-none flex items-center gap-1"
            style={{
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
            }}
          >
            <HiSwitchHorizontal className="w-3 h-3" />
            <span>{topics[0]}</span>
            {topics.length > 1 && <span className="text-yellow-400/60">+{topics.length - 1}</span>}
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}

export default memo(RelationshipEdge);

