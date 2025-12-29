import dagre from 'dagre';
import type { Node, Edge } from 'reactflow';
import type { RoleDefinition } from '../types/workflow';

const NODE_WIDTH = 288;
const NODE_HEIGHT = 200;
const TIER_SPACING = 400; // Vertical space between tiers
const TIER_PADDING = 100; // Top padding for first tier
const HORIZONTAL_SPACING = 320; // Horizontal spacing between nodes in same tier
const VERTICAL_SPACING = 250; // Vertical spacing for hierarchy within tier
const TIER_BAND_HEIGHT = 300; // Fixed height of each tier band

export interface TierLayoutConfig {
  tier: 'strategic' | 'tactical' | 'execution';
  y: number;
  label: string;
  color: string;
  minY: number; // Minimum Y position for nodes in this tier
  maxY: number; // Maximum Y position for nodes in this tier
}

export const TIER_CONFIGS: TierLayoutConfig[] = [
  { 
    tier: 'strategic', 
    y: TIER_PADDING + TIER_BAND_HEIGHT / 2, // Center Y position for nodes in this tier
    label: 'Strategic', 
    color: 'rgba(139, 92, 246, 0.1)',
    minY: TIER_PADDING, // Top of tier band
    maxY: TIER_PADDING + TIER_BAND_HEIGHT, // Bottom of tier band
  },
  { 
    tier: 'tactical', 
    y: TIER_PADDING + TIER_SPACING + TIER_BAND_HEIGHT / 2, // Center Y position for nodes
    label: 'Tactical', 
    color: 'rgba(59, 130, 246, 0.1)',
    minY: TIER_PADDING + TIER_SPACING, // Top of tier band
    maxY: TIER_PADDING + TIER_SPACING + TIER_BAND_HEIGHT, // Bottom of tier band
  },
  { 
    tier: 'execution', 
    y: TIER_PADDING + TIER_SPACING * 2 + TIER_BAND_HEIGHT / 2, // Center Y position for nodes
    label: 'Execution', 
    color: 'rgba(34, 197, 94, 0.1)',
    minY: TIER_PADDING + TIER_SPACING * 2, // Top of tier band
    maxY: TIER_PADDING + TIER_SPACING * 2 + TIER_BAND_HEIGHT, // Bottom of tier band
  },
];

/**
 * Get tier boundaries for a given tier
 */
export function getTierBoundaries(tier: 'strategic' | 'tactical' | 'execution'): { minY: number; maxY: number } | null {
  const config = TIER_CONFIGS.find(t => t.tier === tier);
  if (!config) return null;
  return { minY: config.minY, maxY: config.maxY };
}

/**
 * Detect which tier a Y position belongs to
 * Returns the tier that contains the Y position, or null if outside all tiers
 */
export function getTierForPosition(y: number): 'strategic' | 'tactical' | 'execution' | null {
  for (const config of TIER_CONFIGS) {
    if (y >= config.minY && y <= config.maxY) {
      return config.tier;
    }
  }
  // If outside all tiers, find the closest tier
  let closestTier: 'strategic' | 'tactical' | 'execution' | null = null;
  let minDistance = Infinity;
  
  for (const config of TIER_CONFIGS) {
    const centerY = config.y;
    const distance = Math.abs(y - centerY);
    if (distance < minDistance) {
      minDistance = distance;
      closestTier = config.tier;
    }
  }
  
  return closestTier;
}

/**
 * Constrain a Y position to a tier's boundaries
 */
export function constrainToTier(tier: 'strategic' | 'tactical' | 'execution', y: number): number {
  const boundaries = getTierBoundaries(tier);
  if (!boundaries) return y;
  
  // Clamp Y position to tier boundaries
  return Math.max(boundaries.minY, Math.min(boundaries.maxY, y));
}

/**
 * Creates a hierarchical tier-based layout with proper top-down structure
 * Groups roles by tier (Strategic → Tactical → Execution)
 * Within each tier, arranges roles hierarchically based on reportsTo relationships
 */
export function hierarchicalTierLayout(
  roles: RoleDefinition[]
): RoleDefinition[] {
  if (roles.length === 0) return roles;

  // Group roles by tier
  const rolesByTier: Record<string, RoleDefinition[]> = {
    strategic: [],
    tactical: [],
    execution: [],
  };

  roles.forEach(role => {
    const tier = role.modelConfig.tier;
    if (tier) {
      rolesByTier[tier].push(role);
    }
  });

  // Build hierarchy map (who reports to whom)
  const reportsToMap = new Map<string, string | null>();
  const childrenMap = new Map<string, RoleDefinition[]>();
  
  roles.forEach(role => {
    reportsToMap.set(role.id, role.reportsTo);
    if (!childrenMap.has(role.id)) {
      childrenMap.set(role.id, []);
    }
  });

  // Build children map
  roles.forEach(role => {
    if (role.reportsTo) {
      const parent = childrenMap.get(role.reportsTo);
      if (parent) {
        parent.push(role);
      }
    }
  });

  // Layout each tier
  const layoutedRoles: RoleDefinition[] = [];
  const centerX = 600; // Center of canvas

  TIER_CONFIGS.forEach((tierConfig) => {
    const tierRoles = rolesByTier[tierConfig.tier];
    if (tierRoles.length === 0) return;

    // Find root roles in this tier (no reportsTo or reportsTo is in different tier)
    const rootRoles = tierRoles.filter(role => {
      if (!role.reportsTo) return true;
      const parent = roles.find(r => r.id === role.reportsTo);
      return !parent || parent.modelConfig.tier !== tierConfig.tier;
    });

    // Layout root roles horizontally
    const rootCount = rootRoles.length;
    const startX = centerX - ((rootCount - 1) * HORIZONTAL_SPACING) / 2;

    rootRoles.forEach((rootRole, rootIndex) => {
      const x = startX + rootIndex * HORIZONTAL_SPACING;
      const y = tierConfig.y; // Use tier center Y
      
      // Create new role with updated position - ensure we preserve all properties
      const layoutedRole: RoleDefinition = {
        ...rootRole,
        position: { x, y }, // Explicitly set position
      };
      
      layoutedRoles.push(layoutedRole);

      // Layout children hierarchically
      layoutChildren(rootRole, x, y, tierRoles, childrenMap, layoutedRoles, tierConfig.tier);
    });
  });

  // Update roles that weren't laid out (shouldn't happen, but safety check)
  roles.forEach(role => {
    if (!layoutedRoles.find(r => r.id === role.id)) {
      const tier = role.modelConfig.tier;
      const tierConfig = TIER_CONFIGS.find(t => t.tier === tier);
      const y = tierConfig?.y || TIER_PADDING;
      layoutedRoles.push({
        ...role,
        position: {
          x: centerX,
          y: y,
        },
      });
    }
  });

  // Verify all roles have positions set
  const rolesWithoutPositions = layoutedRoles.filter(r => !r.position || typeof r.position.x !== 'number' || typeof r.position.y !== 'number');
  if (rolesWithoutPositions.length > 0) {
    console.warn('Some roles missing positions after layout:', rolesWithoutPositions.map(r => r.id));
  }

  return layoutedRoles;
}

/**
 * Recursively layout children of a role
 */
function layoutChildren(
  parent: RoleDefinition,
  parentX: number,
  parentY: number,
  tierRoles: RoleDefinition[],
  childrenMap: Map<string, RoleDefinition[]>,
  layoutedRoles: RoleDefinition[],
  tier: string
): void {
  const children = childrenMap.get(parent.id)?.filter(child => 
    tierRoles.includes(child) && !layoutedRoles.find(r => r.id === child.id)
  ) || [];

  if (children.length === 0) return;

  // Layout children below parent
  const childY = parentY + VERTICAL_SPACING;
  const childCount = children.length;
  const startX = parentX - ((childCount - 1) * HORIZONTAL_SPACING) / 2;

  children.forEach((child, index) => {
    const x = startX + index * HORIZONTAL_SPACING;
    
    // Create new role with updated position - ensure we preserve all properties
    const layoutedChild: RoleDefinition = {
      ...child,
      position: { x, y: childY }, // Override position
    };
    
    layoutedRoles.push(layoutedChild);

    // Recursively layout grandchildren
    layoutChildren(child, x, childY, tierRoles, childrenMap, layoutedRoles, tier);
  });
}

/**
 * Legacy autoLayout function using dagre (kept for compatibility)
 */
export function autoLayout(
  nodes: Node[],
  edges: Edge[],
  direction: 'TB' | 'LR' = 'TB'
): Node[] {
  const dagreGraph = new dagre.graphlib.Graph();
  dagreGraph.setDefaultEdgeLabel(() => ({}));
  dagreGraph.setGraph({
    rankdir: direction,
    nodesep: 80,
    ranksep: 120,
    marginx: 50,
    marginy: 50,
  });

  nodes.forEach((node) => {
    dagreGraph.setNode(node.id, { width: NODE_WIDTH, height: NODE_HEIGHT });
  });

  // Only use hierarchical edges (delegates, reports-to) for layout
  const hierarchicalEdges = edges.filter(edge => {
    const edgeData = edge.data as any;
    const relType = edgeData?.relationship?.type;
    return relType === 'delegates' || relType === 'reports-to';
  });

  hierarchicalEdges.forEach((edge) => {
    dagreGraph.setEdge(edge.source, edge.target);
  });

  dagre.layout(dagreGraph);

  return nodes.map((node) => {
    const nodeWithPosition = dagreGraph.node(node.id);
    return {
      ...node,
      position: {
        x: nodeWithPosition.x - NODE_WIDTH / 2,
        y: nodeWithPosition.y - NODE_HEIGHT / 2,
      },
    };
  });
}

export function getLayoutedElements(
  nodes: Node[],
  edges: Edge[],
  direction: 'TB' | 'LR' = 'TB'
) {
  const layoutedNodes = autoLayout(nodes, edges, direction);
  return { nodes: layoutedNodes, edges };
}

