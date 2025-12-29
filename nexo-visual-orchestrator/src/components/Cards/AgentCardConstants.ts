// src/components/Cards/AgentCardConstants.ts

import type { InstanceStatus } from '../../types/workflow';

export const STATUS_STYLES: Record<InstanceStatus, { bg: string; pulse: boolean; label: string }> = {
  'initializing': { bg: 'bg-blue-500', pulse: true, label: 'Initializing' },
  'idle': { bg: 'bg-slate-500', pulse: false, label: 'Idle' },
  'busy': { bg: 'bg-green-500', pulse: true, label: 'Busy' },
  'negotiating': { bg: 'bg-yellow-500', pulse: true, label: 'Negotiating' },
  'waiting-approval': { bg: 'bg-orange-500', pulse: true, label: 'Waiting Approval' },
  'terminating': { bg: 'bg-red-500', pulse: true, label: 'Terminating' },
  'error': { bg: 'bg-red-600', pulse: false, label: 'Error' },
};

export const COLOR_CLASSES: Record<string, { bg: string; text: string; border: string; accent: string }> = {
  purple: { bg: 'bg-purple-500/10', text: 'text-purple-400', border: 'border-purple-500/30', accent: 'bg-purple-500/20' },
  red: { bg: 'bg-red-500/10', text: 'text-red-400', border: 'border-red-500/30', accent: 'bg-red-500/20' },
  yellow: { bg: 'bg-yellow-500/10', text: 'text-yellow-400', border: 'border-yellow-500/30', accent: 'bg-yellow-500/20' },
  cyan: { bg: 'bg-cyan-500/10', text: 'text-cyan-400', border: 'border-cyan-500/30', accent: 'bg-cyan-500/20' },
  green: { bg: 'bg-green-500/10', text: 'text-green-400', border: 'border-green-500/30', accent: 'bg-green-500/20' },
  indigo: { bg: 'bg-indigo-500/10', text: 'text-indigo-400', border: 'border-indigo-500/30', accent: 'bg-indigo-500/20' },
  pink: { bg: 'bg-pink-500/10', text: 'text-pink-400', border: 'border-pink-500/30', accent: 'bg-pink-500/20' },
  orange: { bg: 'bg-orange-500/10', text: 'text-orange-400', border: 'border-orange-500/30', accent: 'bg-orange-500/20' },
  slate: { bg: 'bg-slate-500/10', text: 'text-slate-400', border: 'border-slate-500/30', accent: 'bg-slate-500/20' },
  teal: { bg: 'bg-teal-500/10', text: 'text-teal-400', border: 'border-teal-500/30', accent: 'bg-teal-500/20' },
  amber: { bg: 'bg-amber-500/10', text: 'text-amber-400', border: 'border-amber-500/30', accent: 'bg-amber-500/20' },
  emerald: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/30', accent: 'bg-emerald-500/20' },
};

