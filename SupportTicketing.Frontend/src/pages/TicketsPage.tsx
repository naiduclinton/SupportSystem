import React, { useState, useCallback } from 'react'
import { useQuery } from 'react-query'
import { format } from 'date-fns'
import { clsx } from 'clsx'
import { ticketsApi } from '../api'
import type { TicketSearchParams, TicketStatus, TicketPriority } from '../types'
import { StatusBadge, PriorityBadge, SlaBar, Avatar, Spinner, EmptyState } from '../components/ui'
import { useDrawer, useToasts } from '../store'
import NewTicketModal from '../components/NewTicketModal'
import TicketDrawer from '../components/TicketDrawer'

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: '',           label: 'All' },
  { value: 'Open',       label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Pending',    label: 'Pending' },
  { value: 'Resolved',   label: 'Resolved' },
  { value: 'Closed',     label: 'Closed' },
]

const PRIORITY_FILTERS: { value: string; label: string }[] = [
  { value: '',         label: 'All priorities' },
  { value: 'critical', label: 'Critical' },
  { value: 'high',     label: 'High' },
  { value: 'medium',   label: 'Medium' },
  { value: 'low',      label: 'Low' },
]

export default function TicketsPage() {
  const { open: openDrawer, ticketId } = useDrawer()
  const { add: toast } = useToasts()

  const [showNew, setShowNew] = useState(false)
  const [params, setParams] = useState<TicketSearchParams>({
    page: 1, pageSize: 25, sortBy: 'created_at', sortDesc: true
  })
  const [search, setSearch] = useState('')

  const setParam = useCallback(<K extends keyof TicketSearchParams>(key: K, val: TicketSearchParams[K]) => {
    setParams(p => ({ ...p, [key]: val, page: 1 }))
  }, [])

  // Debounced search
  const [searchTimer, setSearchTimer] = useState<ReturnType<typeof setTimeout>>()
  const handleSearch = (v: string) => {
    setSearch(v)
    clearTimeout(searchTimer)
    setSearchTimer(setTimeout(() => setParam('search', v || undefined), 400))
  }

  const { data, isLoading, refetch } = useQuery(
    ['tickets', params],
    () => ticketsApi.list(params),
    { keepPreviousData: true, staleTime: 30_000 }
  )

  const tickets = data?.items ?? []
  const total   = data?.totalCount ?? 0
  const pages   = data?.totalPages ?? 1

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Top bar */}
      <div
        className="flex items-center gap-3 px-6 py-4"
        style={{ borderBottom: '1px solid var(--border)', background: 'var(--surface-0)' }}
      >
        <h1 className="font-display font-semibold text-lg flex-1" style={{ color: 'var(--ink)' }}>
          Tickets
        </h1>

        {/* Search */}
        <div className="relative">
          <i
            className="fa-solid fa-magnifying-glass absolute left-3 top-1/2 -translate-y-1/2 text-xs"
            style={{ color: 'var(--ink-3)' }}
          />
          <input
            className="input-base pl-8 pr-3 py-2 text-sm"
            style={{ width: 220 }}
            placeholder="Search tickets…"
            value={search}
            onChange={e => handleSearch(e.target.value)}
          />
        </div>

        <button className="btn btn-primary text-sm" onClick={() => setShowNew(true)}>
          <i className="fa-solid fa-plus text-xs" />
          New ticket
        </button>
      </div>

      {/* Filter bar */}
      <div
        className="flex items-center gap-2 px-6 py-3 overflow-x-auto"
        style={{ borderBottom: '1px solid var(--border)', background: 'var(--surface-0)' }}
      >
        {STATUS_FILTERS.map(f => (
          <button
            key={f.value}
            className={clsx(
              'text-xs px-3 py-1.5 rounded-full border transition-colors whitespace-nowrap',
              params.status === f.value || (!params.status && f.value === '')
                ? 'bg-blue-50 border-blue-200 text-blue-600 font-medium dark:bg-blue-900/30 dark:border-blue-700 dark:text-blue-400'
                : 'border-transparent text-[color:var(--ink-2)] hover:bg-[var(--surface-2)]'
            )}
            onClick={() => setParam('status', (f.value || undefined) as TicketStatus | undefined)}
          >
            {f.label}
          </button>
        ))}

        <div className="w-px h-4 mx-1" style={{ background: 'var(--border)' }} />

        <select
          className="input-base text-xs py-1.5 px-2"
          style={{ width: 140 }}
          value={params.priority ?? ''}
          onChange={e => setParam('priority', (e.target.value || undefined) as TicketPriority | undefined)}
        >
          {PRIORITY_FILTERS.map(f => (
            <option key={f.value} value={f.value}>{f.label}</option>
          ))}
        </select>

        <label className="flex items-center gap-1.5 text-xs ml-1 cursor-pointer" style={{ color: 'var(--ink-2)' }}>
          <input
            type="checkbox"
            checked={params.slaBreached ?? false}
            onChange={e => setParam('slaBreached', e.target.checked || undefined)}
            className="rounded"
          />
          SLA breached
        </label>

        <div className="ml-auto flex items-center gap-2">
          <span className="text-xs" style={{ color: 'var(--ink-3)' }}>
            {total} ticket{total !== 1 ? 's' : ''}
          </span>
          <button className="btn btn-ghost p-2" onClick={() => refetch()} title="Refresh">
            <i className="fa-solid fa-rotate text-xs" style={{ color: 'var(--ink-2)' }} />
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto px-6 py-4">
        <div className="card overflow-hidden">
          {isLoading ? (
            <div className="flex items-center justify-center py-16 gap-2" style={{ color: 'var(--ink-3)' }}>
              <Spinner /> <span className="text-sm">Loading tickets…</span>
            </div>
          ) : tickets.length === 0 ? (
            <EmptyState message="No tickets match your filters" />
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Subject</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>Customer</th>
                  <th>Assignee</th>
                  <th>SLA</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {tickets.map(t => (
                  <tr key={t.id} onClick={() => openDrawer(t.id)}>
                    <td>
                      <span className="font-mono text-xs" style={{ color: 'var(--ink-3)' }}>
                        #{t.ticketNumber}
                      </span>
                    </td>
                    <td style={{ maxWidth: 300 }}>
                      <div className="truncate-1 font-medium text-sm">{t.subject}</div>
                      {t.categoryName && (
                        <div className="text-xs mt-0.5" style={{ color: 'var(--ink-3)' }}>{t.categoryName}</div>
                      )}
                    </td>
                    <td><StatusBadge status={t.status} /></td>
                    <td><PriorityBadge priority={t.priority} /></td>
                    <td>
                      <div className="text-sm truncate-1" style={{ maxWidth: 140 }}>{t.customerName || t.customerEmail}</div>
                      <div className="text-xs" style={{ color: 'var(--ink-3)' }}>{t.customerEmail}</div>
                    </td>
                    <td>
                      {t.assigneeName
                        ? <div className="flex items-center gap-2"><Avatar name={t.assigneeName} size={22} /><span className="text-sm">{t.assigneeName}</span></div>
                        : <span className="text-xs" style={{ color: 'var(--ink-3)' }}>Unassigned</span>
                      }
                    </td>
                    <td>
                      <SlaBar pct={t.slaCompliancePct ?? undefined} breached={t.slaBreached} />
                    </td>
                    <td>
                      <span className="text-xs" style={{ color: 'var(--ink-3)' }}>
                        {format(new Date(t.createdAt), 'MMM d, HH:mm')}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {pages > 1 && (
          <div className="flex items-center justify-between mt-4 px-1">
            <span className="text-xs" style={{ color: 'var(--ink-3)' }}>
              Page {params.page} of {pages}
            </span>
            <div className="flex gap-2">
              <button
                className="btn text-xs py-1.5 px-3"
                disabled={params.page === 1}
                onClick={() => setParam('page', (params.page ?? 1) - 1)}
              >
                <i className="fa-solid fa-chevron-left text-xs" /> Prev
              </button>
              <button
                className="btn text-xs py-1.5 px-3"
                disabled={params.page === pages}
                onClick={() => setParam('page', (params.page ?? 1) + 1)}
              >
                Next <i className="fa-solid fa-chevron-right text-xs" />
              </button>
            </div>
          </div>
        )}
      </div>

      {showNew && <NewTicketModal onClose={() => setShowNew(false)} onCreated={() => { setShowNew(false); refetch(); toast('Ticket created', 'success') }} />}
      {ticketId && <TicketDrawer onUpdated={refetch} />}
    </div>
  )
}
