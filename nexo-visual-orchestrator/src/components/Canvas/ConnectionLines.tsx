// src/components/Canvas/ConnectionLines.tsx

/**
 * ConnectionLines Component
 * 
 * Renders SVG connection lines between agent cards on the canvas. Displays:
 * - Existing relationships as dashed indigo lines
 * - Connection preview (when creating a new relationship) as dashed green line
 * 
 * Uses absolute positioning to overlay lines on top of the canvas.
 */

import type { RoleDefinition, Relationship } from '../../types/workflow';

interface ConnectionLine {
  from: { x: number; y: number };
  to: { x: number; y: number };
  relationship: Relationship;
}

interface ConnectionLinesProps {
  connectionLines: ConnectionLine[];
  connectionPreview: { from: string; to: { x: number; y: number } } | null;
  roles: RoleDefinition[];
}

export default function ConnectionLines({
  connectionLines,
  connectionPreview,
  roles,
}: ConnectionLinesProps) {
  return (
    <svg className="absolute inset-0 pointer-events-none" style={{ zIndex: 1 }}>
      {connectionLines.map((line) => (
        <line
          key={`${line.relationship.sourceRoleId}-${line.relationship.targetRoleId}`}
          x1={line.from.x}
          y1={line.from.y}
          x2={line.to.x}
          y2={line.to.y}
          stroke="#6366F1"
          strokeWidth="2"
          strokeDasharray="5,5"
          opacity={0.5}
        />
      ))}
      
      {connectionPreview && (() => {
        const sourceRole = roles.find(r => r.id === connectionPreview.from);
        if (!sourceRole?.position) return null;
        
        return (
          <line
            x1={sourceRole.position.x + 160}
            y1={sourceRole.position.y + 100}
            x2={connectionPreview.to.x}
            y2={connectionPreview.to.y}
            stroke="#10B981"
            strokeWidth="2"
            strokeDasharray="3,3"
            opacity={0.7}
          />
        );
      })()}
    </svg>
  );
}

