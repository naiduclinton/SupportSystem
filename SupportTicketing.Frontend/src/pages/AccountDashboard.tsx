import React, { useState } from 'react'
import { useQuery } from 'react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, PieChart, Pie, Cell, Legend,
  LineChart, Line
} from 'recharts'
import { reportsApi, ticketsApi } from '../api'
import { Spinner, StatusBadge, PriorityBadge } from '../components/ui'
import { useDrawer } from '../store'
import { format } from 'date-fns'
import { createPortal } from 'react-dom'

const COLORS = { AdaptIT: '#3B82F6', ChannelPartner: '#6366F1', Unassigned: '#9CA3AF' }
const PIE_COLORS = ['#3B82F6', '#6366F1', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899']

function DrillModal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return createPortal(
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" style={{ width: 740, maxHeight: '85vh' }} onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between px-6 pt-5 pb-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="font-display font-semibold text-base">{title}</h2>
          <button className="btn-ghost btn p-2" onClick={onClose}><i className="fa-solid fa-xmark" /></button>
        </div>
        <div className="overflow-y-auto" style={{ maxHeight: 'calc(85vh - 60px)' }}>{children}</div>
      </div>
    </div>,
    document.body
  )
}

function TicketTable({ accountHolder, partnerName, onTicketClick }: {
  accountHolder?: string; partnerName?: string; onTicketClick: (id: string) => void
}) {
  const { data, isLoading } = useQuery(
    ['tickets-account', accountHolder, partnerName],
    () => ticketsApi.list({ pageSize: 100, sortBy: 'created_at', sortDesc: true }),
    { staleTime: 30_000 }
  )

  const tickets = (data?.items ?? []).filter((t: any) => {
    if (accountHolder && (t as any).accountHolder !== accountHolder) return false
    return true
  })

  if (isLoading) return <div className="flex items-center justify-center py-8"><Spinner /></div>
  if (!tickets.length) return <p className="text-sm text-center py-8" style={{ color: 'var(--ink-3)' }}>No tickets found.</p>

  return (
    <table className="data-table">
      <thead>
        <tr><th>#</th><th>Subject</th><th>Status</th><th>Priority</th><th>Customer</th><th>Product</th><th>Created</th></tr>
      </thead>
      <tbody>
        {tickets.map((t: any) => (
          <tr key={t.id} onClick={() => onTicketClick(t.id)} className="cursor-pointer">
            <td><span className="font-mono text-xs" style={{ color: 'var(--ink-3)' }}>#{t.ticketNumber}</span></td>
            <td><span className="text-sm font-medium">{t.subject}</span></td>
            <td><StatusBadge status={t.status} /></td>
            <td><PriorityBadge priority={t.priority} /></td>
            <td><span className="text-sm" style={{ color: 'var(--ink-2)' }}>{t.accountCustomer || t.customerName || '—'}</span></td>
            <td><span className="text-sm" style={{ color: 'var(--ink-2)' }}>{t.accountProduct || '—'}</span></td>
            <td><span className="text-xs" style={{ color: 'var(--ink-3)' }}>{format(new Date(t.createdAt), 'MMM d, HH:mm')}</span></td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default function AccountDashboard() {
  const { open: openTicket } = useDrawer()
  const [modal, setModal] = useState<{ title: string; accountHolder?: string; partnerName?: string } | null>(null)

  const { data, isLoading } = useQuery('account-holder-stats', reportsApi.accountHolder, { staleTime: 30_000 })

  if (isLoading) return (
    <div className="flex-1 flex items-center justify-center gap-2" style={{ color: 'var(--ink-3)' }}>
      <Spinner /> Loading account dashboard…
    </div>
  )

  const byAccountHolder: any[] = data?.byAccountHolder ?? []
  const byChannelPartner: any[] = data?.byChannelPartner ?? []
  const trend: any[] = data?.trend ?? []

  const adaptIT      = byAccountHolder.find(r => r.accountHolder === 'AdaptIT') ?? { total: 0, openCount: 0, slaBreachedCount: 0 }
  const channelTotal = byAccountHolder.find(r => r.accountHolder === 'ChannelPartner') ?? { total: 0, openCount: 0, slaBreachedCount: 0 }
  const grandTotal   = byAccountHolder.reduce((s, r) => s + r.total, 0)

  const pieData = byAccountHolder.map(r => ({
    name:  r.accountHolder === 'AdaptIT' ? 'Adapt IT' : r.accountHolder === 'ChannelPartner' ? 'Channel Partner' : 'Unassigned',
    value: r.total,
    key:   r.accountHolder,
  }))

  const statusData = byAccountHolder.map(r => ({
    name:       r.accountHolder === 'AdaptIT' ? 'Adapt IT' : r.accountHolder === 'ChannelPartner' ? 'Channel Partner' : 'Unassigned',
    Open:       r.openCount,
    'In Progress': r.inProgressCount,
    Pending:    r.pendingCount,
    Resolved:   r.resolvedCount,
    Closed:     r.closedCount,
  }))

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display font-semibold text-lg" style={{ color: 'var(--ink)' }}>Account holder dashboard</h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ink-3)' }}>Adapt IT vs Channel Partner ticket volume</p>
        </div>
        <span className="text-xs" style={{ color: 'var(--ink-3)' }}>Live data · Click to drill down</span>
      </div>

      {/* Summary stat cards */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Total tickets',         value: grandTotal,                 sub: 'All accounts',          color: 'var(--ink)' },
          { label: 'Adapt IT',              value: adaptIT.total,              sub: `${adaptIT.openCount} open`,       color: '#3B82F6',  key: 'AdaptIT' },
          { label: 'Channel partner',       value: channelTotal.total,         sub: `${channelTotal.openCount} open`,  color: '#6366F1',  key: 'ChannelPartner' },
          { label: 'Active SLA breaches',   value: byAccountHolder.reduce((s, r) => s + r.slaBreachedCount, 0), sub: 'Across all accounts', color: '#EF4444' },
        ].map(s => (
          <button
            key={s.label}
            type="button"
            className="stat-card text-left w-full hover:opacity-80 transition-opacity"
            style={{ cursor: (s as any).key ? 'pointer' : 'default' }}
            onClick={() => (s as any).key && setModal({ title: s.label + ' tickets', accountHolder: (s as any).key })}
          >
            <p className="text-xs uppercase tracking-wide mb-2" style={{ color: 'var(--ink-3)' }}>{s.label}</p>
            <p className="text-3xl font-display font-semibold" style={{ color: s.color }}>{s.value}</p>
            <p className="text-xs mt-1" style={{ color: 'var(--ink-3)' }}>{s.sub}</p>
            {(s as any).key && <p className="text-xs mt-2" style={{ color: 'var(--accent)' }}>Drill down →</p>}
          </button>
        ))}
      </div>

      {/* Charts row 1 */}
      <div className="grid grid-cols-2 gap-4 mb-4">

        {/* Pie — share of volume */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>Volume share by account holder</h2>
          {grandTotal === 0 ? (
            <p className="text-sm text-center py-10" style={{ color: 'var(--ink-3)' }}>No ticket data yet. Create tickets to see data here.</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%" cy="45%"
                  innerRadius={55} outerRadius={85}
                  dataKey="value" paddingAngle={3}
                  onClick={(d: any) => d.key && setModal({ title: d.name + ' tickets', accountHolder: d.key })}
                  style={{ cursor: 'pointer' }}
                >
                  {pieData.map((entry, i) => (
                    <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }}
                  formatter={(v: number, n: string) => [`${v} tickets (${grandTotal > 0 ? Math.round(v / grandTotal * 100) : 0}%)`, n]}
                />
                <Legend wrapperStyle={{ fontSize: 12, color: 'var(--ink-2)' }} />
              </PieChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* Bar — status breakdown per account holder */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>Status breakdown by account holder</h2>
          {grandTotal === 0 ? (
            <p className="text-sm text-center py-10" style={{ color: 'var(--ink-3)' }}>No ticket data yet.</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={statusData} margin={{ left: -20, right: 0, top: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="name" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} />
                <Legend wrapperStyle={{ fontSize: 11, color: 'var(--ink-2)' }} />
                <Bar dataKey="Open"        fill="#3B82F6" stackId="a" radius={[0,0,0,0]} />
                <Bar dataKey="In Progress" fill="#F59E0B" stackId="a" />
                <Bar dataKey="Pending"     fill="#8B5CF6" stackId="a" />
                <Bar dataKey="Resolved"    fill="#10B981" stackId="a" />
                <Bar dataKey="Closed"      fill="#6B7280" stackId="a" radius={[4,4,0,0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Charts row 2 */}
      <div className="grid grid-cols-2 gap-4 mb-4">

        {/* Line — 14-day trend */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>14-day ticket trend</h2>
          <ResponsiveContainer width="100%" height={180}>
            <LineChart data={trend} margin={{ left: -20, right: 0, top: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
              <XAxis dataKey="day" tick={{ fontSize: 10, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} allowDecimals={false} />
              <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} />
              <Legend wrapperStyle={{ fontSize: 11, color: 'var(--ink-2)' }} />
              <Line type="monotone" dataKey="adaptIt"        stroke="#3B82F6" strokeWidth={2} dot={false} name="Adapt IT" />
              <Line type="monotone" dataKey="channelPartner" stroke="#6366F1" strokeWidth={2} dot={false} name="Channel Partner" />
              <Line type="monotone" dataKey="unassigned"     stroke="#9CA3AF" strokeWidth={1} dot={false} name="Unassigned" strokeDasharray="4 4" />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Channel partner breakdown */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>Channel partner breakdown</h2>
          {byChannelPartner.length === 0 ? (
            <p className="text-sm text-center py-8" style={{ color: 'var(--ink-3)' }}>No channel partner tickets yet.</p>
          ) : (
            <div className="flex flex-col gap-3">
              {byChannelPartner.map((p: any, i: number) => (
                <button
                  key={p.partnerName}
                  type="button"
                  className="text-left w-full"
                  onClick={() => setModal({ title: p.partnerName + ' tickets', accountHolder: 'ChannelPartner', partnerName: p.partnerName })}
                >
                  <div className="flex justify-between text-sm mb-1">
                    <span className="font-medium hover:underline" style={{ color: 'var(--ink)' }}>{p.partnerName}</span>
                    <span style={{ color: 'var(--ink-2)' }}>{p.total} total · {p.active} active</span>
                  </div>
                  <div className="sla-track" style={{ height: 6 }}>
                    <div className="sla-fill" style={{
                      width: `${byChannelPartner[0].total > 0 ? Math.round(p.total / byChannelPartner[0].total * 100) : 0}%`,
                      background: PIE_COLORS[i % PIE_COLORS.length]
                    }} />
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Full ticket table */}
      <div className="card overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>All tickets by account</h2>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>Account holder</th>
              <th>Channel partner</th>
              <th>Customer</th>
              <th>Product</th>
              <th>Open</th>
              <th>In Progress</th>
              <th>Resolved</th>
              <th>SLA breaches</th>
            </tr>
          </thead>
          <tbody>
            {byAccountHolder.map((r: any) => (
              <tr
                key={r.accountHolder}
                onClick={() => setModal({ title: (r.accountHolder === 'AdaptIT' ? 'Adapt IT' : r.accountHolder === 'ChannelPartner' ? 'Channel Partner' : 'Unassigned') + ' tickets', accountHolder: r.accountHolder })}
                className="cursor-pointer"
              >
                <td>
                  <span className="font-medium text-sm">
                    {r.accountHolder === 'AdaptIT' ? 'Adapt IT' : r.accountHolder === 'ChannelPartner' ? 'Channel Partner' : 'Unassigned'}
                  </span>
                </td>
                <td><span className="text-sm" style={{ color: 'var(--ink-3)' }}>{r.accountHolder === 'ChannelPartner' ? byChannelPartner.length + ' partners' : '—'}</span></td>
                <td><span className="font-mono text-sm">{r.total}</span></td>
                <td><span className="font-mono text-sm">—</span></td>
                <td><span className="font-mono text-sm text-blue-500 font-medium">{r.openCount}</span></td>
                <td><span className="font-mono text-sm text-amber-500 font-medium">{r.inProgressCount}</span></td>
                <td><span className="font-mono text-sm text-emerald-500 font-medium">{r.resolvedCount}</span></td>
                <td><span className={`font-mono text-sm font-medium ${r.slaBreachedCount > 0 ? 'text-red-500' : 'text-emerald-500'}`}>{r.slaBreachedCount}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Drill-down modal */}
      {modal && (
        <DrillModal title={modal.title} onClose={() => setModal(null)}>
          <TicketTable
            accountHolder={modal.accountHolder}
            partnerName={modal.partnerName}
            onTicketClick={(id) => { openTicket(id); setModal(null) }}
          />
        </DrillModal>
      )}
    </div>
  )
}
