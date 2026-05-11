/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['DM Sans', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
        display: ['Syne', 'sans-serif'],
      },
      colors: {
        surface: {
          0: 'var(--surface-0)',
          1: 'var(--surface-1)',
          2: 'var(--surface-2)',
          3: 'var(--surface-3)',
        },
        border: 'var(--border)',
        accent: {
          DEFAULT: 'var(--accent)',
          muted: 'var(--accent-muted)',
          fg: 'var(--accent-fg)',
        },
        ink: {
          DEFAULT: 'var(--ink)',
          2: 'var(--ink-2)',
          3: 'var(--ink-3)',
        },
        status: {
          open:       '#3B82F6',
          progress:   '#F59E0B',
          pending:    '#8B5CF6',
          resolved:   '#10B981',
          closed:     '#6B7280',
          critical:   '#EF4444',
          high:       '#F97316',
          medium:     '#EAB308',
          low:        '#6B7280',
        }
      },
      borderRadius: {
        sm: '4px', DEFAULT: '8px', md: '8px', lg: '12px', xl: '16px'
      },
      boxShadow: {
        card:  '0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04)',
        panel: '0 4px 24px rgba(0,0,0,0.10)',
        glow:  '0 0 0 3px var(--accent-muted)',
      },
      animation: {
        'slide-in':  'slideIn 0.2s ease-out',
        'fade-in':   'fadeIn 0.15s ease-out',
        'spin-slow': 'spin 2s linear infinite',
      },
      keyframes: {
        slideIn: { from: { transform: 'translateX(100%)', opacity: '0' }, to: { transform: 'translateX(0)', opacity: '1' } },
        fadeIn:  { from: { opacity: '0', transform: 'translateY(4px)' }, to: { opacity: '1', transform: 'translateY(0)' } },
      }
    }
  },
  plugins: []
}
