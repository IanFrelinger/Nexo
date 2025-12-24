/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        surface: {
          DEFAULT: '#1E293B',
          dark: '#0F172A',
          light: '#334155',
        },
        agent: {
          architect: '#6366F1',
          combat: '#EF4444',
          economy: '#F59E0B',
          ai: '#8B5CF6',
          asset: '#EC4899',
          build: '#10B981',
          playtest: '#06B6D4',
          analysis: '#14B8A6',
        },
        status: {
          idle: '#6B7280',
          pending: '#FBBF24',
          running: '#3B82F6',
          completed: '#22C55E',
          failed: '#EF4444',
          conflict: '#F97316',
        },
      },
      animation: {
        'pulse-slow': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'flow': 'flow 1s linear infinite',
      },
      keyframes: {
        flow: {
          '0%': { strokeDashoffset: '24' },
          '100%': { strokeDashoffset: '0' },
        },
      },
    },
  },
  plugins: [],
};

