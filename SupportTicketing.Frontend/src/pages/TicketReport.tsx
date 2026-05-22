import React, { useState, useCallback } from 'react'
import { createPortal } from 'react-dom'
import { useQuery } from 'react-query'
import { format, subDays, startOfQuarter } from 'date-fns'
import { ticketsApi, usersApi } from '../api'
import { StatusBadge, Spinner } from '../components/ui'
import { useDrawer } from '../store'

interface ReportParams {
  dateFrom: string
  dateTo: string
  assigneeId: string
  status: string
  accountHolder: string
  channelPartnerName: string
  accountCustomer: string
  accountProduct: string
}

const today = format(new Date(), 'yyyy-MM-dd')
const defaultParams: ReportParams = {
  dateFrom: format(subDays(new Date(), 30), 'yyyy-MM-dd'),
  dateTo: today,
  assigneeId: '',
  status: '',
  accountHolder: '',
  channelPartnerName: '',
  accountCustomer: '',
  accountProduct: '',
}

function exportCSV(tickets: any[], showCP: boolean) {
  const headers = [
    'Ticket #', 'Subject', 'Date', 'Assignee', 'Status',
    'Account Holder', ...(showCP ? ['Channel Partner Name'] : []),
    'Customer', 'Product'
  ]
  const rows = tickets.map((t: any) => [
    `#${t.ticketNumber}`,
    `"${(t.subject ?? '').replace(/"/g, '""')}"`,
    format(new Date(t.createdAt), 'yyyy-MM-dd'),
    t.assigneeName ?? '',
    t.status ?? '',
    t.accountHolder ?? '',
    ...(showCP ? [t.channelPartnerName ?? ''] : []),
    t.accountCustomer ?? '',
    t.accountProduct ?? '',
  ])
  const csv = [headers, ...rows].map(r => r.join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `ticket-report-${today}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

export default function TicketReport() {
  const { open: openTicket } = useDrawer()
  const [params, setParams] = useState<ReportParams>(defaultParams)
  const [hasRun, setHasRun] = useState(false)
  const [runKey, setRunKey] = useState(0)

  const isCP = params.accountHolder === 'ChannelPartner'

  const setP = (key: keyof ReportParams, val: string) =>
    setParams(p => ({ ...p, [key]: val }))

  const setQuick = (period: 'week' | 'month' | 'quarter') => {
    const now = new Date()
    const from = period === 'week'
      ? subDays(now, 7)
      : period === 'month'
      ? subDays(now, 30)
      : startOfQuarter(now)
    setParams(p => ({ ...p, dateFrom: format(from, 'yyyy-MM-dd'), dateTo: today }))
  }

  const clearParams = () => {
    setParams(defaultParams)
    setHasRun(false)
  }

  const runReport = () => {
    setHasRun(true)
    setRunKey(k => k + 1)
  }

  // Fetch all tickets with filters
  const { data, isLoading } = useQuery(
    ['ticket-report', runKey, params],
    () => ticketsApi.list({
      status: params.status ? params.status as any : undefined,
      assigneeId: params.assigneeId || undefined,
      pageSize: 500,
      sortBy: 'created_at',
      sortDesc: true,
    }),
    { enabled: hasRun, staleTime: 0 }
  )

  const { data: users } = useQuery('users-list', usersApi.list, { staleTime: 60_000 })

  // Client-side filter for fields not supported by API query params
  const tickets = (data?.items ?? []).filter((t: any) => {
    if (params.dateFrom && new Date(t.createdAt) < new Date(params.dateFrom)) return false
    if (params.dateTo && new Date(t.createdAt) > new Date(params.dateTo + 'T23:59:59')) return false
    if (params.accountHolder && t.accountHolder !== params.accountHolder) return false
    if (params.channelPartnerName && !(t.channelPartnerName ?? '').toLowerCase().includes(params.channelPartnerName.toLowerCase())) return false
    if (params.accountCustomer && !(t.accountCustomer ?? '').toLowerCase().includes(params.accountCustomer.toLowerCase())) return false
    if (params.accountProduct && !(t.accountProduct ?? '').toLowerCase().includes(params.accountProduct.toLowerCase())) return false
    return true
  })

  const showCP = tickets.some((t: any) => t.accountHolder === 'ChannelPartner')

  const activeFilters = [
    params.dateFrom && `From: ${params.dateFrom}`,
    params.dateTo && `To: ${params.dateTo}`,
    params.status && `Status: ${params.status}`,
    params.assigneeId && users && `Assignee: ${users.find((u: any) => u.id === params.assigneeId)?.fullName ?? ''}`,
    params.accountHolder && `Account: ${params.accountHolder === 'AdaptIT' ? 'Adapt IT' : 'Channel Partner'}`,
    params.channelPartnerName && `Partner: ${params.channelPartnerName}`,
    params.accountCustomer && `Customer: ${params.accountCustomer}`,
    params.accountProduct && `Product: ${params.accountProduct}`,
  ].filter(Boolean) as string[]

  return (
    <div className="flex-1 overflow-y-auto p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display font-semibold text-lg" style={{ color: 'var(--ink)' }}>Ticket report</h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ink-3)' }}>Configure parameters and run to generate report</p>
        </div>
      </div>

      {/* Parameters card */}
      <div className="card p-5 mb-5">
        <p className="text-xs font-medium uppercase tracking-wider mb-4" style={{ color: 'var(--ink-3)' }}>Report parameters</p>

        {/* Date range */}
        <div className="mb-4">
          <label className="block text-xs uppercase tracking-wide mb-2 font-medium" style={{ color: 'var(--ink-2)' }}>Date range</label>
          <div className="flex items-center gap-3 flex-wrap">
            <input
              type="date"
              className="input-base"
              style={{ width: 160 }}
              value={params.dateFrom}
              onChange={e => setP('dateFrom', e.target.value)}
            />
            <span className="text-sm" style={{ color: 'var(--ink-3)' }}>to</span>
            <input
              type="date"
              className="input-base"
              style={{ width: 160 }}
              value={params.dateTo}
              onChange={e => setP('dateTo', e.target.value)}
            />
            <div className="flex gap-2">
              {(['week', 'month', 'quarter'] as const).map(p => (
                <button key={p} className="btn text-xs py-1.5 px-3" onClick={() => setQuick(p)}>
                  {p === 'week' ? 'Last 7 days' : p === 'month' ? 'Last 30 days' : 'This quarter'}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Filter grid */}
        <div className="grid grid-cols-3 gap-3 mb-4">
          {/* Assignee */}
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>Assignee</label>
            <select className="input-base" value={params.assigneeId} onChange={e => setP('assigneeId', e.target.value)}>
              <option value="">All assignees</option>
              {(users ?? []).map((u: any) => (
                <option key={u.id} value={u.id}>{u.fullName}</option>
              ))}
            </select>
          </div>

          {/* Status */}
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>Status</label>
            <select className="input-base" value={params.status} onChange={e => setP('status', e.target.value)}>
              <option value="">All statuses</option>
              {['Open','InProgress','Pending','Resolved','Closed'].map(s => (
                <option key={s} value={s}>{s === 'InProgress' ? 'In Progress' : s}</option>
              ))}
            </select>
          </div>

          {/* Account Holder */}
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>Account holder</label>
            <select
              className="input-base"
              value={params.accountHolder}
              onChange={e => {
                setP('accountHolder', e.target.value)
                if (e.target.value !== 'ChannelPartner') setP('channelPartnerName', '')
              }}
            >
              <option value="">All</option>
              <option value="AdaptIT">Adapt IT</option>
              <option value="ChannelPartner">Channel Partner</option>
            </select>
          </div>

          {/* Channel Partner Name — conditional */}
          <div style={{ opacity: isCP ? 1 : 0.35, pointerEvents: isCP ? 'auto' : 'none', transition: 'opacity 0.2s' }}>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>
              Channel partner name
            </label>
            <input
              className="input-base"
              placeholder="Filter by partner name…"
              value={params.channelPartnerName}
              onChange={e => setP('channelPartnerName', e.target.value)}
            />
          </div>

          {/* Customer */}
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>Customer</label>
            <input
              className="input-base"
              placeholder="Filter by customer…"
              maxLength={15}
              value={params.accountCustomer}
              onChange={e => setP('accountCustomer', e.target.value)}
            />
          </div>

          {/* Product */}
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5 font-medium" style={{ color: 'var(--ink-2)' }}>Product</label>
            <input
              className="input-base"
              placeholder="Filter by product…"
              maxLength={10}
              value={params.accountProduct}
              onChange={e => setP('accountProduct', e.target.value)}
            />
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-end gap-2 pt-3" style={{ borderTop: '1px solid var(--border)' }}>
          <button className="btn text-sm" onClick={clearParams}>
            <i className="fa-solid fa-rotate-left text-xs" /> Clear
          </button>
          <button className="btn btn-primary text-sm" onClick={runReport}>
            <i className="fa-solid fa-play text-xs" /> Run report
          </button>
        </div>
      </div>

      {/* Loading */}
      {hasRun && isLoading && (
        <div className="card p-5 flex items-center gap-3" style={{ color: 'var(--ink-2)' }}>
          <Spinner size={16} /> Running report…
        </div>
      )}

      {/* Results */}
      {hasRun && !isLoading && (
        <>
          {/* Results header */}
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3 flex-wrap">
              <span className="text-sm font-medium" style={{ color: 'var(--ink)' }}>
                {tickets.length} ticket{tickets.length !== 1 ? 's' : ''} found
              </span>
              <div className="flex gap-1.5 flex-wrap">
                {activeFilters.map(f => (
                  <span
                    key={f}
                    className="text-xs px-2 py-0.5 rounded-full"
                    style={{
                      background: 'var(--surface-2)',
                      color: 'var(--ink-2)',
                      border: '0.5px solid var(--border)'
                    }}
                  >{f}</span>
                ))}
              </div>
            </div>
            {tickets.length > 0 && (
              <button
                className="btn text-sm"
                style={{
                  background: 'var(--color-background-success)',
                  borderColor: 'var(--color-border-success)',
                  color: 'var(--color-text-success)'
                }}
                onClick={() => exportCSV(tickets, showCP)}
              >
                <i className="fa-solid fa-download text-xs" /> Export CSV
              </button>
            )}
          </div>

          {tickets.length === 0 ? (
            <div className="card p-8 flex flex-col items-center gap-3" style={{ color: 'var(--ink-3)' }}>
              <i className="fa-regular fa-folder-open text-3xl" />
              <p className="text-sm">No tickets match the selected parameters. Try broadening your filters.</p>
            </div>
          ) : (
            <div className="card overflow-hidden">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Subject</th>
                    <th>Date</th>
                    <th>Assignee</th>
                    <th>Status</th>
                    <th>Account holder</th>
                    {showCP && <th>Channel partner</th>}
                    <th>Customer</th>
                    <th>Product</th>
                  </tr>
                </thead>
                <tbody>
                  {tickets.map((t: any) => (
                    <tr key={t.id} onClick={() => openTicket(t.id)} className="cursor-pointer">
                      <td>
                        <span className="font-mono text-xs" style={{ color: 'var(--ink-3)' }}>
                          #{t.ticketNumber}
                        </span>
                      </td>
                      <td style={{ maxWidth: 200 }}>
                        <span className="text-sm font-medium" style={{ display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {t.subject}
                        </span>
                      </td>
                      <td>
                        <span className="text-xs" style={{ color: 'var(--ink-2)', whiteSpace: 'nowrap' }}>
                          {format(new Date(t.createdAt), 'MMM d, yyyy')}
                        </span>
                      </td>
                      <td>
                        <span className="text-sm" style={{ color: 'var(--ink-2)', whiteSpace: 'nowrap' }}>
                          {t.assigneeName ?? <span style={{ color: 'var(--ink-3)' }}>Unassigned</span>}
                        </span>
                      </td>
                      <td><StatusBadge status={t.status} /></td>
                      <td>
                        {t.accountHolder ? (
                          <span
                            className="text-xs px-2 py-0.5 rounded-full"
                            style={{
                              background: t.accountHolder === 'AdaptIT' ? 'rgba(59,130,246,0.1)' : 'rgba(99,102,241,0.1)',
                              color: t.accountHolder === 'AdaptIT' ? '#3B82F6' : '#6366F1',
                              whiteSpace: 'nowrap'
                            }}
                          >
                            {t.accountHolder === 'AdaptIT' ? 'Adapt IT' : 'Channel Partner'}
                          </span>
                        ) : <span style={{ color: 'var(--ink-3)' }}>—</span>}
                      </td>
                      {showCP && (
                        <td>
                          <span className="text-sm" style={{ color: 'var(--ink-2)' }}>
                            {t.channelPartnerName ?? '—'}
                          </span>
                        </td>
                      )}
                      <td>
                        <span className="text-sm" style={{ color: 'var(--ink-2)' }}>
                          {t.accountCustomer ?? '—'}
                        </span>
                      </td>
                      <td>
                        <span className="text-sm" style={{ color: 'var(--ink-2)' }}>
                          {t.accountProduct ?? '—'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  )
}
