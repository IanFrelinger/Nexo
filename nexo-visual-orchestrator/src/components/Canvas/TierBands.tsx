// src/components/Canvas/TierBands.tsx

import { useMemo } from 'react';
import { useReactFlow } from 'reactflow';
import { TIER_CONFIGS } from '../../utils/layoutEngine';
import type { RoleDefinition } from '../../types/workflow';

interface TierBandsProps {
  roles: RoleDefinition[];
  collapsedTiers: Set<string>;
}

export default function TierBands({ roles, collapsedTiers }: TierBandsProps) {
  const { getViewport } = useReactFlow();

  // Get viewport to apply transform for zoom/pan
  const viewport = getViewport();
  
  // Calculate tier band positions and dimensions
  // Use fixed positions that match tier boundaries exactly
  const tierBands = useMemo(() => {
    return TIER_CONFIGS.map(tierConfig => {
      const tierRoles = roles.filter(r => r.modelConfig.tier === tierConfig.tier);
      if (tierRoles.length === 0 || collapsedTiers.has(tierConfig.tier)) {
        return null;
      }

      // Use fixed tier boundaries - bands should match tier boundaries exactly
      // Bands are rendered in ReactFlow's coordinate system
      const bandY = tierConfig.minY; // Start at minY (top of tier band)
      const bandHeight = tierConfig.maxY - tierConfig.minY; // Height spans from minY to maxY
      
      // Band should span a large width to cover the canvas
      const bandWidth = 5000; // Large enough to span the canvas
      const bandX = -1000; // Start left of origin to ensure full coverage

      return {
        tier: tierConfig.tier,
        label: tierConfig.label,
        color: tierConfig.color,
        x: bandX,
        y: bandY,
        width: bandWidth,
        height: bandHeight,
        minY: tierConfig.minY,
        maxY: tierConfig.maxY,
        centerY: tierConfig.y, // The center Y where nodes should be positioned
      };
    }).filter((band): band is NonNullable<typeof band> => band !== null);
  }, [roles, collapsedTiers]);

  // Calculate transform string for viewport (pan and zoom)
  const transform = useMemo(() => {
    return `translate(${viewport.x}, ${viewport.y}) scale(${viewport.zoom})`;
  }, [viewport.x, viewport.y, viewport.zoom]);

  if (tierBands.length === 0) return null;

  return (
    <div className="absolute inset-0 pointer-events-none" style={{ zIndex: 0 }}>
      {/* Use ReactFlow's coordinate system - render bands at exact tier positions */}
      {/* Apply viewport transform so bands scale and pan with the canvas */}
      <svg 
        className="w-full h-full" 
        style={{ 
          position: 'absolute', 
          top: 0, 
          left: 0,
          width: '100%',
          height: '100%',
          overflow: 'visible',
        }}
      >
        <g transform={transform}>
          {tierBands.map(band => (
            <g key={band.tier}>
              {/* Background band - positioned at exact tier boundaries */}
              <rect
                x={band.x}
                y={band.y}
                width={band.width}
                height={band.height}
                fill={band.color}
                opacity={0.3}
                rx={8}
              />
              {/* Top boundary line */}
              <line
                x1={band.x}
                y1={band.y}
                x2={band.x + band.width}
                y2={band.y}
                stroke="rgba(148, 163, 184, 0.5)" // slate-400
                strokeWidth={2 / viewport.zoom} // Scale stroke width with zoom
                strokeDasharray={`${4 / viewport.zoom},${4 / viewport.zoom}`} // Scale dash array with zoom
              />
              {/* Bottom boundary line */}
              <line
                x1={band.x}
                y1={band.y + band.height}
                x2={band.x + band.width}
                y2={band.y + band.height}
                stroke="rgba(148, 163, 184, 0.5)" // slate-400
                strokeWidth={2 / viewport.zoom} // Scale stroke width with zoom
                strokeDasharray={`${4 / viewport.zoom},${4 / viewport.zoom}`} // Scale dash array with zoom
              />
              {/* Label */}
              <text
                x={band.x + 20}
                y={band.y + 30}
                fill="currentColor"
                className="text-slate-300 font-semibold"
                style={{ 
                  fontSize: `${14 / viewport.zoom}px`, // Scale font size with zoom
                  fontWeight: 600,
                }}
              >
                {band.label}
              </text>
            </g>
          ))}
        </g>
      </svg>
    </div>
  );
}

