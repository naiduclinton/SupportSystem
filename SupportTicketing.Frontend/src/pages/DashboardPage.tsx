import React, { useState } from 'react'
import { useQuery } from 'react-query'
import {
  AreaChart, Area, BarChart, Bar, XAxis, YAxis,
  Tooltip, ResponsiveContainer, CartesianGrid, Cell
} from 'recharts'
import { reportsApi } from '../api'
import { Avatar, Spinner, StatusBadge, PriorityBadge } from '../components/ui'
import { useDrawer } from '../store'
import { format } from 'date-fns'

// ── Types ─────────────────────────────────────────────────────────────────
interface VolumePoint    { day: string; date: string; created: number; resolved: number }
interface CategoryItem   { category: string; count: number; percentage: number }
interface DrillDownData  { volumeByDay: VolumePoint[]; byCategory: CategoryItem[]; recentTickets: any[] }

// ── Stat card ─────────────────────────────────────────────────────────────
function StatCard({ label, value, sub, color, onClick }: {
  label: string; value: string | number; sub?: string; color?: string; onClick?: () => void
}) {
  return (
    <div
      className={`stat-card ${onClick ? 'cursor-pointer hover:opacity-90 transition-opacity' : ''}`}
      onClick={onClick}
      title={onClick ? `Click to view ${label} tickets` : undefined}
    >
      <p className="text-xs uppercase tracking-wide mb-2" style={{ color: 'var(--ink-3)' }}>{label}</p>
      <p className="text-3xl font-display font-semibold" style={{ color: color ?? 'var(--ink)' }}>{value}</p>
      {sub && <p className="text-xs mt-1" style={{ color: 'var(--ink-3)' }}>{sub}</p>}
      {onClick && <p className="text-xs mt-2" style={{ color: 'var(--accent)' }}>Click to drill down →</p>}
    </div>
  )
}

// ── Drill-down modal ───────────────────────────────────────────────────────
function DrillDownModal({ title, children, onClose }: {
  title: string; children: React.ReactNode; onClose: () => void
}) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="modal"
        style={{ width: 720, maxHeight: '85vh' }}
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 pt-5 pb-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="font-display font-semibold text-base">{title}</h2>
          <button className="btn-ghost btn p-2" onClick={onClose}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>
        <div className="overflow-y-auto" style={{ maxHeight: 'calc(85vh - 60px)' }}>
          {children}
        </div>
      </div>
    </div>
  )
}

// ── Ticket mini-table ──────────────────────────────────────────────────────
function TicketMiniTable({ tickets, onTicketClick }: { tickets: any[]; onTicketClick: (id: string) => void }) {
  if (!tickets?.length) return <p className="text-sm p-6 text-center" style={{ color: 'var(--ink-3)' }}>No tickets found.</p>
  return (
    <table className="data-table">
      <thead>
        <tr>
          <th>#</th><th>Subject</th><th>Status</th><th>Priority</th><th>Customer</th><th>Created</th>
        </tr>
      </thead>
      <tbody>
        {tickets.map((t: any) => (
          <tr key={t.id} onClick={() => onTicketClick(t.id)} className="cursor-pointer">
            <td><span className="font-mono text-xs" style={{ color: 'var(--ink-3)' }}>#{t.ticketNumber}</span></td>
            <td><span className="text-sm font-medium">{t.subject}</span></td>
            <td><StatusBadge status={t.status} /></td>
            <td><PriorityBadge priority={t.priority} /></td>
            <td><span className="text-sm" style={{ color: 'var(--ink-2)' }}>{t.customerName || t.customerEmail}</span></td>
            <td><span className="text-xs" style={{ color: 'var(--ink-3)' }}>{format(new Date(t.createdAt), 'MMM d, HH:mm')}</span></td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

// ── Main Dashboard ─────────────────────────────────────────────────────────
export default function DashboardPage() {
  const { open: openTicket } = useDrawer()
  const [modal, setModal] = useState<string | null>(null)
  const [filterStatus, setFilterStatus] = useState<string | null>(null)

  const { data: stats, isLoading: statsLoading } = useQuery(
    'dashboard-stats',
    reportsApi.dashboard,
    { staleTime: 30_000 }
  )

  const { data: drillDown, isLoading: ddLoading } = useQuery(
    'dashboard-drilldown',
    reportsApi.drillDown,
    { staleTime: 30_000 }
  )

  const { data: ticketList } = useQuery(
    ['tickets-modal', filterStatus],
    () => import('../api').then(m => m.ticketsApi.list({
      status: filterStatus as any,
      pageSize: 50,
      sortBy: 'created_at',
      sortDesc: true
    })),
    { enabled: !!filterStatus && !!modal, staleTime: 10_000 }
  )

  const openModal = (key: string, status?: string) => {
    setFilterStatus(status ?? null)
    setModal(key)
  }

  const BAR_COLORS = ['#6366F1', '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6']

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display font-semibold text-lg" style={{ color: 'var(--ink)' }}>Dashboard</h1>
        <span className="text-xs" style={{ color: 'var(--ink-3)' }}>Live data · Click stats to drill down</span>
      </div>

      {/* Stat cards */}
      {statsLoading ? (
        <div className="flex items-center gap-2 mb-6" style={{ color: 'var(--ink-3)' }}><Spinner size={14} /> Loading stats…</div>
      ) : (
        <div className="grid grid-cols-4 gap-4 mb-6">
          <StatCard label="Open"            value={stats?.openCount ?? 0}          sub="+today"          color="#3B82F6"  onClick={() => openModal('status-open', 'open')} />
          <StatCard label="In Progress"     value={stats?.inProgressCount ?? 0}    sub="Being worked on" color="#F59E0B"  onClick={() => openModal('status-progress', 'in_progress')} />
          <StatCard label="Pending"         value={stats?.pendingCount ?? 0}        sub="Awaiting reply"  color="#8B5CF6"  onClick={() => openModal('status-pending', 'pending')} />
          <StatCard label="Resolved today"  value={stats?.resolvedTodayCount ?? 0}  sub="Closed today"    color="#10B981"  onClick={() => openModal('status-resolved', 'resolved')} />
          <StatCard label="Avg 1st response" value={stats?.avgFirstResponseHours ? `${stats.avgFirstResponseHours}h` : '—'} sub="SLA target: 4h" />
          <StatCard label="Avg resolution"  value={stats?.avgResolutionHours ? `${stats.avgResolutionHours}h` : '—'}       sub="SLA target: 8h" />
          <StatCard label="SLA breaches"    value={stats?.slaBreachCount ?? 0}     sub="Active breaches" color="#EF4444"  onClick={() => openModal('sla-breach')} />
          <StatCard label="CSAT score"      value={stats?.csatScore ? `${stats.csatScore}%` : '—'}       sub="Last 30 days" color="#10B981" />
        </div>
      )}

      {/* Charts */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        {/* Volume trend */}
        <div className="card p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>Ticket volume — last 7 days</h2>
            <button className="btn btn-ghost text-xs py-1 px-2" onClick={() => openModal('volume')}>
              <i className="fa-solid fa-expand text-xs mr-1" /> Drill down
            </button>
          </div>
          {ddLoading ? <div className="flex items-center justify-center h-40"><Spinner /></div> : (
            <ResponsiveContainer width="100%" height={180}>
              <AreaChart data={drillDown?.volumeByDay ?? []} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="gCreated" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%"  stopColor="#3B82F6" stopOpacity={0.2} />
                    <stop offset="95%" stopColor="#3B82F6" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="gResolved" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%"  stopColor="#10B981" stopOpacity={0.2} />
                    <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="day" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} labelStyle={{ color: 'var(--ink)' }} />
                <Area type="monotone" dataKey="created"  stroke="#3B82F6" fill="url(#gCreated)"  strokeWidth={2} dot={false} name="Created" />
                <Area type="monotone" dataKey="resolved" stroke="#10B981" fill="url(#gResolved)" strokeWidth={2} dot={false} name="Resolved" />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* Category breakdown */}
        <div className="card p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>Tickets by category</h2>
            <button className="btn btn-ghost text-xs py-1 px-2" onClick={() => openModal('category')}>
              <i className="fa-solid fa-expand text-xs mr-1" /> Drill down
            </button>
          </div>
          {ddLoading ? <div className="flex items-center justify-center h-40"><Spinner /></div> : (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={drillDown?.byCategory ?? []} margin={{ top: 0, right: 0, left: 0, bottom: 0 }} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" horizontal={false} />
                <XAxis type="number" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} allowDecimals={false} />
                <YAxis dataKey="category" type="category" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} width={90} />
                <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} formatter={(v, n, p) => [`${v} (${p.payload.percentage}%)`, 'Tickets']} />
                <Bar dataKey="count" radius={[0, 4, 4, 0]} name="Tickets" onClick={(d) => openModal('category-detail', d.category)}>
                  {(drillDown?.byCategory ?? []).map((_: any, i: number) => (
                    <Cell key={i} fill={BAR_COLORS[i % BAR_COLORS.length]} cursor="pointer" />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Agent workload */}
      <div className="card overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>Agent workload</h2>
          <button className="btn btn-ghost text-xs py-1 px-2" onClick={() => openModal('agents')}>
            <i className="fa-solid fa-expand text-xs mr-1" /> Drill down
          </button>
        </div>
        <table className="data-table">
          <thead>
            <tr><th>Agent</th><th>Team</th><th>Open</th><th>In Progress</th><th>Pending</th><th>SLA Breaches</th><th>Avg Resolution</th></tr>
          </thead>
          <tbody>
            {ddLoading ? (
              <tr><td colSpan={7} className="text-center py-8"><Spinner /></td></tr>
            ) : (drillDown?.recentTickets?.length === 0 ? (
              <tr><td colSpan={7} className="text-center py-8 text-sm" style={{ color: 'var(--ink-3)' }}>No agent data</td></tr>
            ) : null)}
          </tbody>
        </table>
        {/* Agent workload from dedicated endpoint */}
        <AgentWorkloadTable onDrillDown={(agentId) => openModal('agent-tickets-' + agentId)} />
      </div>

      {/* Recent tickets */}
      <div className="card overflow-hidden mt-4">
        <div className="flex items-center justify-between px-5 py-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>Recent tickets</h2>
          <button className="btn btn-ghost text-xs py-1 px-2" onClick={() => openModal('recent')}>
            <i className="fa-solid fa-expand text-xs mr-1" /> View all
          </button>
        </div>
        {ddLoading ? (
          <div className="flex items-center justify-center py-8"><Spinner /></div>
        ) : (
          <TicketMiniTable tickets={drillDown?.recentTickets ?? []} onTicketClick={openTicket} />
        )}
      </div>

      {/* ── Drill-down modals ── */}

      {/* Status drill-downs */}
      {modal?.startsWith('status-') && (
        <DrillDownModal
          title={`${modal === 'status-open' ? 'Open' : modal === 'status-progress' ? 'In Progress' : modal === 'status-pending' ? 'Pending' : 'Resolved'} Tickets`}
          onClose={() => setModal(null)}
        >
          {ticketList ? (
            <TicketMiniTable tickets={ticketList.items} onTicketClick={(id) => { openTicket(id); setModal(null) }} />
          ) : <div className="flex items-center justify-center py-8"><Spinner /></div>}
        </DrillDownModal>
      )}

      {/* SLA breach drill-down */}
      {modal === 'sla-breach' && (
        <DrillDownModal title="SLA Breached Tickets" onClose={() => setModal(null)}>
          {ticketList ? (
            <TicketMiniTable tickets={ticketList.items.filter((t: any) => t.slaBreached)} onTicketClick={(id) => { openTicket(id); setModal(null) }} />
          ) : <div className="flex items-center justify-center py-8"><Spinner /></div>}
        </DrillDownModal>
      )}

      {/* Volume drill-down */}
      {modal === 'volume' && (
        <DrillDownModal title="Ticket volume — last 7 days" onClose={() => setModal(null)}>
          <div className="p-6">
            <ResponsiveContainer width="100%" height={260}>
              <AreaChart data={drillDown?.volumeByDay ?? []} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="gC2" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#3B82F6" stopOpacity={0.2} /><stop offset="95%" stopColor="#3B82F6" stopOpacity={0} /></linearGradient>
                  <linearGradient id="gR2" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#10B981" stopOpacity={0.2} /><stop offset="95%" stopColor="#10B981" stopOpacity={0} /></linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} />
                <Area type="monotone" dataKey="created" stroke="#3B82F6" fill="url(#gC2)" strokeWidth={2} name="Created" />
                <Area type="monotone" dataKey="resolved" stroke="#10B981" fill="url(#gR2)" strokeWidth={2} name="Resolved" />
              </AreaChart>
            </ResponsiveContainer>
            <div className="mt-4">
              <table className="data-table">
                <thead><tr><th>Date</th><th>Day</th><th>Created</th><th>Resolved</th><th>Net</th></tr></thead>
                <tbody>
                  {(drillDown?.volumeByDay ?? []).map((v: VolumePoint) => (
                    <tr key={v.date}>
                      <td className="font-mono text-xs">{v.date}</td>
                      <td className="text-sm">{v.day}</td>
                      <td><span className="font-mono font-medium text-blue-500">{v.created}</span></td>
                      <td><span className="font-mono font-medium text-emerald-500">{v.resolved}</span></td>
                      <td><span className={`font-mono font-medium ${v.created - v.resolved > 0 ? 'text-red-500' : 'text-emerald-500'}`}>{v.created - v.resolved > 0 ? '+' : ''}{v.created - v.resolved}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </DrillDownModal>
      )}

      {/* Category drill-down */}
      {modal === 'category' && (
        <DrillDownModal title="Tickets by category" onClose={() => setModal(null)}>
          <div className="p-6">
            <div className="flex flex-col gap-3 mb-6">
              {(drillDown?.byCategory ?? []).map((c: CategoryItem, i: number) => (
                <div key={c.category}>
                  <div className="flex justify-between text-sm mb-1">
                    <button
                      className="font-medium hover:underline text-left"
                      style={{ color: 'var(--ink)', cursor: 'pointer' }}
                      onClick={() => openModal('category-detail', c.category)}
                    >{c.category}</button>
                    <span style={{ color: 'var(--ink-2)' }}>{c.count} tickets · {c.percentage}%</span>
                  </div>
                  <div className="sla-track" style={{ height: 8 }}>
                    <div className="sla-fill" style={{ width: `${c.percentage}%`, background: BAR_COLORS[i % BAR_COLORS.length] }} />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </DrillDownModal>
      )}

      {/* Recent tickets drill-down */}
      {modal === 'recent' && (
        <DrillDownModal title="Recent tickets" onClose={() => setModal(null)}>
          <TicketMiniTable tickets={drillDown?.recentTickets ?? []} onTicketClick={(id) => { openTicket(id); setModal(null) }} />
        </DrillDownModal>
      )}

      {/* Agents drill-down */}
      {modal === 'agents' && (
        <DrillDownModal title="Agent workload detail" onClose={() => setModal(null)}>
          <AgentWorkloadTable onDrillDown={(id) => openModal('agent-tickets-' + id)} expanded />
        </DrillDownModal>
      )}
    </div>
  )
}

// ── Agent workload sub-component ──────────────────────────────────────────
function AgentWorkloadTable({ onDrillDown, expanded }: { onDrillDown: (id: string) => void; expanded?: boolean }) {
  const { data: workloads, isLoading } = useQuery('agent-workloads', () =>
    import('../api').then(m => m.reportsApi.agents()), { staleTime: 30_000 })

  if (isLoading) return <div className="flex items-center justify-center py-6"><Spinner /></div>
  if (!workloads?.length) return <p className="text-sm text-center py-6" style={{ color: 'var(--ink-3)' }}>No agents found.</p>

  const rows = expanded ? workloads : workloads.slice(0, 5)

  return (
    <table className="data-table">
      {!expanded && <thead><tr><th>Agent</th><th>Team</th><th>Open</th><th>In Progress</th><th>Pending</th><th>SLA Breaches</th><th>Avg Resolution</th></tr></thead>}
      <tbody>
        {rows.map((a: any) => (
          <tr key={a.userId} onClick={() => onDrillDown(a.userId)} className="cursor-pointer">
            <td>
              <div className="flex items-center gap-2">
                <Avatar name={a.fullName} size={26} />
                <span className="text-sm font-medium">{a.fullName}</span>
              </div>
            </td>
            <td><span className="text-sm" style={{ color: 'var(--ink-2)' }}>{a.teamName ?? '—'}</span></td>
            <td><span className="font-mono text-sm text-blue-500 font-medium">{a.openCount}</span></td>
            <td><span className="font-mono text-sm text-amber-500 font-medium">{a.inProgressCount}</span></td>
            <td><span className="font-mono text-sm text-violet-500 font-medium">{a.pendingCount}</span></td>
            <td><span className={`font-mono text-sm font-medium ${a.slaBreachCount > 0 ? 'text-red-500' : 'text-emerald-500'}`}>{a.slaBreachCount}</span></td>
            <td><span className="font-mono text-sm" style={{ color: 'var(--ink-2)' }}>{a.avgResolutionHours ? `${Number(a.avgResolutionHours).toFixed(1)}h` : '—'}</span></td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
