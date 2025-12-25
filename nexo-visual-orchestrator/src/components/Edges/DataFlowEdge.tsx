import { memo } from 'react';
import { getBezierPath, EdgeLabelRenderer } from 'reactflow';
import type { EdgeProps } from 'reactflow';
import type { DependencyEdgeData } from '../../types/workflow';
import { HiExclamationCircle } from 'react-icons/hi';

function DataFlowEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  selected,
}: EdgeProps<DependencyEdgeData>) {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  });

  const isAnimated = data?.animated;
  const hasConflict = data?.hasConflict;

  return (
    <>
      <path
        id={id}
        className={`react-flow__edge-path ${isAnimated ? 'animated-edge' : ''}`}
        d={edgePath}
        strokeWidth={2}
        stroke={hasConflict ? '#F97316' : selected ? '#3B82F6' : '#64748B'}
        fill="none"
        strokeDasharray={isAnimated ? '5' : undefined}
      />
      {hasConflict && (
        <EdgeLabelRenderer>
          <div
            className="absolute bg-orange-500 text-white text-xs px-1 rounded pointer-events-none flex items-center gap-0.5"
            style={{
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
            }}
          >
            <HiExclamationCircle className="w-3 h-3" />
            <span>CONFLICT</span>
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}

export default memo(DataFlowEdge);

