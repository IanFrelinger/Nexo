import React from 'react';
import { BaseEdge, getBezierPath } from 'reactflow';
import type { EdgeProps } from 'reactflow';

interface AnimatedEdgeData {
  status?: 'inactive' | 'active' | 'complete';
}

export const AnimatedEdge: React.FC<EdgeProps<AnimatedEdgeData>> = ({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  style = {},
  markerEnd,
  animated,
  data,
}) => {
  const [edgePath] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const isActive = data?.status === 'active' || animated;
  const isComplete = data?.status === 'complete';
  const strokeColor = isComplete ? '#10b981' : isActive ? '#3b82f6' : '#6b7280';
  const strokeWidth = isActive ? 3 : 2;

    return (
      <>
        <BaseEdge
          id={id}
          path={edgePath}
          markerEnd={markerEnd}
          style={{
            ...style,
            strokeWidth,
            stroke: strokeColor,
            strokeDasharray: isActive ? '5' : '0',
            animation: isActive ? 'dashdraw 0.5s linear infinite' : undefined,
          }}
        />
      {/* Animated flow indicator */}
      {isActive && (
        <circle
          r={4}
          fill="#3b82f6"
          style={{
            offsetPath: `path('${edgePath}')`,
            animation: 'data-flow 1s linear infinite',
          }}
        />
      )}
      <style>{`
        @keyframes dashdraw {
          to {
            stroke-dashoffset: -10;
          }
        }
        @keyframes data-flow {
          0% {
            offset-distance: 0%;
          }
          100% {
            offset-distance: 100%;
          }
        }
      `}</style>
    </>
  );
};
