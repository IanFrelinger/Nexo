// src/components/Nodes/RoleCardConstants.ts

import type { InstanceStatus } from '../../types/workflow';

export const STATUS_STYLES: Record<InstanceStatus, { bg: string; pulse: boolean }> = {
  'initializing': { bg: 'bg-blue-500', pulse: true },
  'idle': { bg: 'bg-slate-500', pulse: false },
  'busy': { bg: 'bg-green-500', pulse: true },
  'negotiating': { bg: 'bg-yellow-500', pulse: true },
  'waiting-approval': { bg: 'bg-orange-500', pulse: true },
  'terminating': { bg: 'bg-red-500', pulse: true },
  'error': { bg: 'bg-red-600', pulse: false },
};

export const TIER_BORDERS: Record<string, string> = {
  'strategic': 'border-l-purple-500',
  'tactical': 'border-l-blue-500',
  'execution': 'border-l-slate-500',
};

export const COLOR_CLASSES: Record<string, { bg: string; text: string; border: string }> = {
  purple: { bg: 'bg-purple-500/20', text: 'text-purple-400', border: 'border-purple-500/50' },
  red: { bg: 'bg-red-500/20', text: 'text-red-400', border: 'border-red-500/50' },
  yellow: { bg: 'bg-yellow-500/20', text: 'text-yellow-400', border: 'border-yellow-500/50' },
  cyan: { bg: 'bg-cyan-500/20', text: 'text-cyan-400', border: 'border-cyan-500/50' },
  green: { bg: 'bg-green-500/20', text: 'text-green-400', border: 'border-green-500/50' },
  indigo: { bg: 'bg-indigo-500/20', text: 'text-indigo-400', border: 'border-indigo-500/50' },
  pink: { bg: 'bg-pink-500/20', text: 'text-pink-400', border: 'border-pink-500/50' },
  orange: { bg: 'bg-orange-500/20', text: 'text-orange-400', border: 'border-orange-500/50' },
  slate: { bg: 'bg-slate-500/20', text: 'text-slate-400', border: 'border-slate-500/50' },
  teal: { bg: 'bg-teal-500/20', text: 'text-teal-400', border: 'border-teal-500/50' },
  amber: { bg: 'bg-amber-500/20', text: 'text-amber-400', border: 'border-amber-500/50' },
  emerald: { bg: 'bg-emerald-500/20', text: 'text-emerald-400', border: 'border-emerald-500/50' },
};

