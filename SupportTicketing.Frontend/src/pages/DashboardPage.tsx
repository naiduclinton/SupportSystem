import React from 'react'
import { useQuery } from 'react-query'
import {
  AreaChart, Area, BarChart, Bar, XAxis, YAxis,
  Tooltip, ResponsiveContainer, CartesianGrid
} from 'recharts'
import { reportsApi } from '../api'
import { Avatar, Spinner } from '../components/ui'

const MOCK_VOLUME = [
  { day: 'Mon', created: 12, resolved: 10 },
  { day: 'Tue', created: 18, resolved: 15 },
  { day: 'Wed', created: 9,  resolved: 11 },
  { day: 'Thu', created: 22, resolved: 18 },
  { day: 'Fri', created: 16, resolved: 20 },
  { day: 'Sat', created: 5,  resolved: 4  },
  { day: 'Sun', created: 3,  resolved: 5  },
]

const MOCK_CATEGORY = [
  { name: 'Billing',   count: 34 },
  { name: 'Technical', count: 28 },
  { name: 'Account',   count: 20 },
  { name: 'General',   count: 18 },
]

const MOCK_AGENTS = [
  { userId: '1', fullName: 'Sarah Reynolds', teamName: 'Support', openCount: 6, inProgressCount: 2, pendingCount: 1, slaBreachCount: 0, avgResolutionHours: 1.2 },
  { userId: '2', fullName: 'Mike Khumalo',   teamName: 'Technical', openCount: 4, inProgressCount: 3, pendingCount: 0, slaBreachCount: 1, avgResolutionHours: 2.1 },
  { userId: '3', fullName: 'Jamie Louw',     teamName: 'Billing',  openCount: 2, inProgressCount: 1, pendingCount: 2, slaBreachCount: 0, avgResolutionHours: 3.4 },
]

function StatCard({ label, value, sub, color }: { label: string; value: string | number; sub?: string; color?: string }) {
  return (
    <div className="stat-card">
      <p className="text-xs uppercase tracking-wide mb-2" style={{ color: 'var(--ink-3)' }}>{label}</p>
      <p className="text-3xl font-display font-semibold" style={{ color: color ?? 'var(--ink)' }}>{value}</p>
      {sub && <p className="text-xs mt-1" style={{ color: 'var(--ink-3)' }}>{sub}</p>}
    </div>
  )
}

export default function DashboardPage() {
  // const { data: stats } = useQuery('dashboard', reportsApi.dashboard)
  // Using mock data for demo — swap with API call above in production
  const stats = {
    openCount: 23, inProgressCount: 11, pendingCount: 8,
    resolvedTodayCount: 17, avgFirstResponseHours: 1.8,
    avgResolutionHours: 4.2, csatScore: 94, slaBreachCount: 4
  }

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <h1 className="font-display font-semibold text-lg mb-6" style={{ color: 'var(--ink)' }}>Dashboard</h1>

      {/* Stat grid */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <StatCard label="Open"            value={stats.openCount}            sub="+3 today"           color="#3B82F6" />
        <StatCard label="In Progress"     value={stats.inProgressCount}      sub="5 agents active"    color="#F59E0B" />
        <StatCard label="Resolved today"  value={stats.resolvedTodayCount}   sub="vs 14 yesterday"    color="#10B981" />
        <StatCard label="CSAT score"      value={`${stats.csatScore}%`}      sub="Last 30 days"       color="#10B981" />
        <StatCard label="Avg first response" value={`${stats.avgFirstResponseHours}h`} sub="SLA target: 4h" />
        <StatCard label="Avg resolution"  value={`${stats.avgResolutionHours}h`} sub="SLA target: 8h" />
        <StatCard label="Pending"         value={stats.pendingCount}         sub="Awaiting customer"  color="#8B5CF6" />
        <StatCard label="SLA breaches"    value={stats.slaBreachCount}       sub="This week"          color="#EF4444" />
      </div>

      <div className="grid grid-cols-2 gap-4 mb-4">
        {/* Volume trend */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>Ticket volume — last 7 days</h2>
          <ResponsiveContainer width="100%" height={180}>
            <AreaChart data={MOCK_VOLUME} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
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
              <YAxis tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
              <Tooltip
                contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }}
                labelStyle={{ color: 'var(--ink)' }}
              />
              <Area type="monotone" dataKey="created"  stroke="#3B82F6" fill="url(#gCreated)"  strokeWidth={2} dot={false} name="Created" />
              <Area type="monotone" dataKey="resolved" stroke="#10B981" fill="url(#gResolved)" strokeWidth={2} dot={false} name="Resolved" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Category breakdown */}
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>Tickets by category</h2>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={MOCK_CATEGORY} margin={{ top: 0, right: 0, left: -20, bottom: 0 }} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" horizontal={false} />
              <XAxis type="number" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
              <YAxis dataKey="name" type="category" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} width={80} />
              <Tooltip
                contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }}
              />
              <Bar dataKey="count" fill="#6366F1" radius={[0, 4, 4, 0]} name="Tickets" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Agent workload */}
      <div className="card overflow-hidden">
        <div className="px-5 py-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="text-sm font-medium" style={{ color: 'var(--ink)' }}>Agent workload</h2>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>Agent</th>
              <th>Team</th>
              <th>Open</th>
              <th>In Progress</th>
              <th>Pending</th>
              <th>SLA Breaches</th>
              <th>Avg Resolution</th>
            </tr>
          </thead>
          <tbody>
            {MOCK_AGENTS.map(a => (
              <tr key={a.userId}>
                <td>
                  <div className="flex items-center gap-2">
                    <Avatar name={a.fullName} size={26} />
                    <span className="text-sm font-medium">{a.fullName}</span>
                  </div>
                </td>
                <td><span className="text-sm" style={{ color: 'var(--ink-2)' }}>{a.teamName}</span></td>
                <td><span className="font-mono text-sm text-blue-500 font-medium">{a.openCount}</span></td>
                <td><span className="font-mono text-sm text-amber-500 font-medium">{a.inProgressCount}</span></td>
                <td><span className="font-mono text-sm text-violet-500 font-medium">{a.pendingCount}</span></td>
                <td>
                  <span className={`font-mono text-sm font-medium ${a.slaBreachCount > 0 ? 'text-red-500' : 'text-emerald-500'}`}>
                    {a.slaBreachCount}
                  </span>
                </td>
                <td>
                  <span className="font-mono text-sm" style={{ color: 'var(--ink-2)' }}>
                    {a.avgResolutionHours?.toFixed(1)}h
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
