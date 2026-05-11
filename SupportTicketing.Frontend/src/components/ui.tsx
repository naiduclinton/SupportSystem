import React from 'react'
import { clsx } from 'clsx'
import type { TicketPriority, TicketStatus } from '../types'
import { useToasts } from '../store'

// ── Status badge ──────────────────────────────────────────────────────────
const STATUS_CONFIG: Record<string, { label: string; color: string }> = {
  open:        { label: 'Open',        color: 'bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400' },
  Open:        { label: 'Open',        color: 'bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400' },
  in_progress: { label: 'In Progress', color: 'bg-amber-50 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400' },
  InProgress:  { label: 'In Progress', color: 'bg-amber-50 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400' },
  pending:     { label: 'Pending',     color: 'bg-violet-50 text-violet-600 dark:bg-violet-900/30 dark:text-violet-400' },
  Pending:     { label: 'Pending',     color: 'bg-violet-50 text-violet-600 dark:bg-violet-900/30 dark:text-violet-400' },
  resolved:    { label: 'Resolved',    color: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400' },
  Resolved:    { label: 'Resolved',    color: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400' },
  closed:      { label: 'Closed',      color: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400' },
  Closed:      { label: 'Closed',      color: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400' },
}

export function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? STATUS_CONFIG['open']
  return <span className={clsx('badge', cfg.color)}>{cfg.label}</span>
}

// ── Priority badge ────────────────────────────────────────────────────────
const PRIORITY_CONFIG: Record<string, { label: string; color: string; dot: string }> = {
  critical: { label: 'Critical', color: 'bg-red-50 text-red-600 dark:bg-red-900/30 dark:text-red-400',       dot: 'bg-red-500' },
  Critical: { label: 'Critical', color: 'bg-red-50 text-red-600 dark:bg-red-900/30 dark:text-red-400',       dot: 'bg-red-500' },
  high:     { label: 'High',     color: 'bg-orange-50 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400', dot: 'bg-orange-500' },
  High:     { label: 'High',     color: 'bg-orange-50 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400', dot: 'bg-orange-500' },
  medium:   { label: 'Medium',   color: 'bg-yellow-50 text-yellow-600 dark:bg-yellow-900/30 dark:text-yellow-400', dot: 'bg-yellow-500' },
  Medium:   { label: 'Medium',   color: 'bg-yellow-50 text-yellow-600 dark:bg-yellow-900/30 dark:text-yellow-400', dot: 'bg-yellow-500' },
  low:      { label: 'Low',      color: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',     dot: 'bg-gray-400' },
  Low:      { label: 'Low',      color: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',     dot: 'bg-gray-400' },
}

export function PriorityBadge({ priority }: { priority: string }) {
  const cfg = PRIORITY_CONFIG[priority] ?? PRIORITY_CONFIG['low']
  return (
    <span className={clsx('badge', cfg.color)}>
      <span className={clsx('w-1.5 h-1.5 rounded-full', cfg.dot)} />
      {cfg.label}
    </span>
  )
}

// ── SLA bar ───────────────────────────────────────────────────────────────
export function SlaBar({ pct, breached }: { pct?: number; breached: boolean }) {
  if (pct === undefined) return <span className="text-xs" style={{ color: 'var(--ink-3)' }}>—</span>
  const color = breached ? '#EF4444' : pct > 70 ? '#10B981' : pct > 40 ? '#F59E0B' : '#EF4444'
  return (
    <div>
      <div className="font-mono text-xs font-medium mb-1" style={{ color }}>{Math.round(pct)}%</div>
      <div className="sla-track">
        <div className="sla-fill" style={{ width: `${Math.min(pct, 100)}%`, background: color }} />
      </div>
    </div>
  )
}

// ── Avatar ────────────────────────────────────────────────────────────────
const AVATAR_COLORS = [
  ['#dbeafe','#1d4ed8'], ['#d1fae5','#065f46'], ['#fce7f3','#9d174d'],
  ['#fef3c7','#92400e'], ['#ede9fe','#4c1d95'], ['#fee2e2','#991b1b'],
]

export function Avatar({ name, size = 28 }: { name: string; size?: number }) {
  const idx = name.charCodeAt(0) % AVATAR_COLORS.length
  const [bg, fg] = AVATAR_COLORS[idx]
  const initials = name.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase()
  return (
    <div
      className="avatar"
      style={{ width: size, height: size, background: bg, color: fg, fontSize: size * 0.38 }}
      title={name}
    >
      {initials}
    </div>
  )
}

// ── Spinner ───────────────────────────────────────────────────────────────
export function Spinner({ size = 18 }: { size?: number }) {
  return (
    <svg
      className="animate-spin"
      width={size} height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2.5}
    >
      <circle cx="12" cy="12" r="10" strokeOpacity={0.2} />
      <path d="M12 2a10 10 0 0 1 10 10" strokeLinecap="round" />
    </svg>
  )
}

// ── Empty state ───────────────────────────────────────────────────────────
export function EmptyState({ message = 'No results found' }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 gap-3">
      <div className="text-4xl" style={{ color: 'var(--ink-3)' }}>
        <i className="fa-regular fa-folder-open" />
      </div>
      <p className="text-sm" style={{ color: 'var(--ink-3)' }}>{message}</p>
    </div>
  )
}

// ── Toast container ───────────────────────────────────────────────────────
export function ToastContainer() {
  const { toasts, remove } = useToasts()
  const icons: Record<string, string> = {
    success: 'fa-circle-check text-emerald-500',
    error:   'fa-circle-xmark text-red-500',
    info:    'fa-circle-info text-blue-500',
  }
  return (
    <div className="toast-container">
      {toasts.map(t => (
        <div key={t.id} className="toast">
          <i className={clsx('fa-solid text-base', icons[t.type])} />
          <span className="flex-1">{t.message}</span>
          <button className="btn-ghost btn p-1" onClick={() => remove(t.id)}>
            <i className="fa-solid fa-xmark text-xs" style={{ color: 'var(--ink-3)' }} />
          </button>
        </div>
      ))}
    </div>
  )
}

// ── Confirm dialog ────────────────────────────────────────────────────────
export function Confirm({
  message, onConfirm, onCancel
}: { message: string; onConfirm: () => void; onCancel: () => void }) {
  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <div className="modal p-6 max-w-sm w-full" onClick={e => e.stopPropagation()}>
        <p className="text-sm mb-5" style={{ color: 'var(--ink)' }}>{message}</p>
        <div className="flex gap-2 justify-end">
          <button className="btn" onClick={onCancel}>Cancel</button>
          <button className="btn btn-danger" onClick={onConfirm}>Confirm</button>
        </div>
      </div>
    </div>
  )
}

// ── Select ────────────────────────────────────────────────────────────────
export function Select({ value, onChange, options, className }: {
  value: string
  onChange: (v: string) => void
  options: { value: string; label: string }[]
  className?: string
}) {
  return (
    <select
      value={value}
      onChange={e => onChange(e.target.value)}
      className={clsx('input-base', className)}
    >
      {options.map(o => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  )
}
