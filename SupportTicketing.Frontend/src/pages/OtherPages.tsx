import React, { useState } from 'react'
import { Avatar } from '../components/ui'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, PieChart, Pie, Cell, Legend,
  BarChart, Bar
} from 'recharts'

// ── Reports ───────────────────────────────────────────────────────────────
const WEEKLY = [
  { week: 'W1', compliance: 88 }, { week: 'W2', compliance: 91 },
  { week: 'W3', compliance: 87 }, { week: 'W4', compliance: 96 },
]
const PIE_DATA = [
  { name: 'Within SLA', value: 141 }, { name: 'Breached', value: 4 },
]
const PIE_COLORS = ['#10B981', '#EF4444']

export function ReportsPage() {
  return (
    <div className="flex-1 overflow-y-auto p-6">
      <h1 className="font-display font-semibold text-lg mb-6" style={{ color: 'var(--ink)' }}>Reports</h1>
      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: 'First response SLA', value: '96%',  sub: 'Target: 90%', color: '#10B981' },
          { label: 'Resolution SLA',     value: '91%',  sub: 'Target: 85%', color: '#10B981' },
          { label: 'SLA breaches',       value: '4',    sub: '2 critical',   color: '#EF4444' },
          { label: 'Escalations',        value: '7',    sub: 'This week',    color: '#F59E0B' },
        ].map(s => (
          <div key={s.label} className="stat-card">
            <p className="text-xs uppercase tracking-wide mb-2" style={{ color: 'var(--ink-3)' }}>{s.label}</p>
            <p className="text-3xl font-display font-semibold" style={{ color: s.color }}>{s.value}</p>
            <p className="text-xs mt-1" style={{ color: 'var(--ink-3)' }}>{s.sub}</p>
          </div>
        ))}
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="card p-5">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>SLA compliance trend</h2>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={WEEKLY} margin={{ left: -20 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
              <XAxis dataKey="week" tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
              <YAxis domain={[80, 100]} tick={{ fontSize: 11, fill: 'var(--ink-3)' }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} formatter={(v: number) => [`${v}%`, 'Compliance']} />
              <Line type="monotone" dataKey="compliance" stroke="#3B82F6" strokeWidth={2} dot={{ r: 4, fill: '#3B82F6' }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
        <div className="card p-5 flex flex-col">
          <h2 className="text-sm font-medium mb-4" style={{ color: 'var(--ink)' }}>SLA outcome breakdown</h2>
          <div className="flex-1 flex items-center justify-center">
            <PieChart width={240} height={180}>
              <Pie data={PIE_DATA} cx={120} cy={80} innerRadius={50} outerRadius={75} dataKey="value" paddingAngle={3}>
                {PIE_DATA.map((_, i) => <Cell key={i} fill={PIE_COLORS[i]} />)}
              </Pie>
              <Tooltip contentStyle={{ background: 'var(--surface-0)', border: '1px solid var(--border)', borderRadius: 8, fontSize: 12 }} />
              <Legend wrapperStyle={{ fontSize: 12, color: 'var(--ink-2)' }} />
            </PieChart>
          </div>
        </div>
      </div>
    </div>
  )
}

// ── Agents ────────────────────────────────────────────────────────────────
const AGENTS = [
  { id: '1', fullName: 'Sarah Reynolds', email: 'sarah@adapt.co', role: 'Senior Agent', team: 'Support',   status: 'online',  csat: 98, resolved: 32, avgHours: 1.2 },
  { id: '2', fullName: 'Mike Khumalo',   email: 'mike@adapt.co',  role: 'Agent',        team: 'Technical', status: 'online',  csat: 95, resolved: 28, avgHours: 2.1 },
  { id: '3', fullName: 'Jamie Louw',     email: 'jamie@adapt.co', role: 'Agent',        team: 'Billing',   status: 'away',    csat: 88, resolved: 21, avgHours: 3.4 },
  { id: '4', fullName: 'Tanya Petersen', email: 'tanya@adapt.co', role: 'Agent',        team: 'Support',   status: 'offline', csat: 92, resolved: 19, avgHours: 2.8 },
]

const STATUS_DOT: Record<string, string> = {
  online: 'bg-emerald-400', away: 'bg-amber-400', offline: 'bg-gray-300'
}

export function AgentsPage() {
  const [showInvite, setShowInvite] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display font-semibold text-lg" style={{ color: 'var(--ink)' }}>Agents</h1>
        <button className="btn btn-primary text-sm" onClick={() => setShowInvite(true)}>
          <i className="fa-solid fa-plus text-xs" /> Invite agent
        </button>
      </div>

      {showInvite && (
        <div className="modal-backdrop" onClick={() => setShowInvite(false)}>
          <div className="modal p-6" onClick={e => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display font-semibold text-base">Invite agent</h2>
              <button className="btn-ghost btn p-2" onClick={() => setShowInvite(false)}><i className="fa-solid fa-xmark" /></button>
            </div>
            <div className="mb-4">
              <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Email address</label>
              <input className="input-base" type="email" placeholder="agent@company.com" value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} />
            </div>
            <div className="mb-4">
              <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Role</label>
              <select className="input-base"><option>Agent</option><option>Admin</option><option>Viewer</option></select>
            </div>
            <div className="flex gap-2 justify-end">
              <button className="btn" onClick={() => setShowInvite(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={() => { alert(`Invitation sent to ${inviteEmail}`); setShowInvite(false); setInviteEmail('') }}>Send invite</button>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        {AGENTS.map(a => (
          <div key={a.id} className="card p-5">
            <div className="flex items-start gap-3">
              <div className="relative">
                <Avatar name={a.fullName} size={40} />
                <span className={`absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-white dark:border-neutral-900 ${STATUS_DOT[a.status]}`} />
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm">{a.fullName}</p>
                <p className="text-xs mt-0.5" style={{ color: 'var(--ink-3)' }}>{a.role} · {a.team}</p>
                <p className="text-xs mt-0.5" style={{ color: 'var(--ink-3)' }}>{a.email}</p>
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3 mt-4 pt-4" style={{ borderTop: '1px solid var(--border)' }}>
              <div className="text-center"><p className="font-mono font-medium text-emerald-500">{a.csat}%</p><p className="text-[10px] mt-0.5" style={{ color: 'var(--ink-3)' }}>CSAT</p></div>
              <div className="text-center"><p className="font-mono font-medium">{a.resolved}</p><p className="text-[10px] mt-0.5" style={{ color: 'var(--ink-3)' }}>Resolved</p></div>
              <div className="text-center"><p className="font-mono font-medium">{a.avgHours}h</p><p className="text-[10px] mt-0.5" style={{ color: 'var(--ink-3)' }}>Avg resolve</p></div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// ── Knowledge Base ─────────────────────────────────────────────────────────
const ARTICLES = [
  { id: '1', title: 'Getting started guide',        category: 'Onboarding', views: 1204, updated: '2 days ago' },
  { id: '2', title: 'Billing & payments FAQ',        category: 'Billing',    views: 892,  updated: '5 days ago' },
  { id: '3', title: 'API integration guide',         category: 'Technical',  views: 741,  updated: '1 week ago' },
  { id: '4', title: 'Troubleshooting common errors', category: 'Technical',  views: 603,  updated: '3 days ago' },
  { id: '5', title: 'Account & access management',   category: 'Account',    views: 521,  updated: '4 days ago' },
  { id: '6', title: 'Data export and reporting',     category: 'General',    views: 388,  updated: '6 days ago' },
]

export function KbPage() {
  const [search, setSearch] = useState('')
  const filtered = ARTICLES.filter(a =>
    !search ||
    a.title.toLowerCase().includes(search.toLowerCase()) ||
    a.category.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display font-semibold text-lg" style={{ color: 'var(--ink)' }}>Knowledge Base</h1>
        <button className="btn btn-primary text-sm"><i className="fa-solid fa-plus text-xs" /> New article</button>
      </div>
      <div className="relative mb-5" style={{ maxWidth: 360 }}>
        <i className="fa-solid fa-magnifying-glass absolute left-3 top-1/2 -translate-y-1/2 text-xs" style={{ color: 'var(--ink-3)' }} />
        <input className="input-base pl-8" placeholder="Search articles…" value={search} onChange={e => setSearch(e.target.value)} />
      </div>
      <div className="card overflow-hidden">
        <table className="data-table">
          <thead>
            <tr><th>Title</th><th>Category</th><th>Views</th><th>Last updated</th><th></th></tr>
          </thead>
          <tbody>
            {filtered.map(a => (
              <tr key={a.id}>
                <td>
                  <div className="flex items-center gap-2">
                    <i className="fa-regular fa-file-lines text-xs" style={{ color: 'var(--ink-3)' }} />
                    <span className="text-sm font-medium">{a.title}</span>
                  </div>
                </td>
                <td><span className="badge bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">{a.category}</span></td>
                <td><span className="font-mono text-sm" style={{ color: 'var(--ink-2)' }}>{a.views.toLocaleString()}</span></td>
                <td><span className="text-sm" style={{ color: 'var(--ink-3)' }}>{a.updated}</span></td>
                <td><button className="btn btn-ghost p-1.5 text-xs"><i className="fa-solid fa-pen-to-square" style={{ color: 'var(--ink-3)' }} /></button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── Settings ──────────────────────────────────────────────────────────────
const DEFAULT_SLA = [
  { priority: 'Critical', first: '1 hour',   resolve: '4 hours',  firstMin: 60,   resolveMin: 240 },
  { priority: 'High',     first: '4 hours',  resolve: '24 hours', firstMin: 240,  resolveMin: 1440 },
  { priority: 'Medium',   first: '8 hours',  resolve: '72 hours', firstMin: 480,  resolveMin: 4320 },
  { priority: 'Low',      first: '24 hours', resolve: '7 days',   firstMin: 1440, resolveMin: 10080 },
]

const DEFAULT_RULES = [
  { name: 'Auto-assign by category',    active: true  },
  { name: 'Escalate on SLA breach',     active: true  },
  { name: 'Auto-close after 7 days idle', active: false },
  { name: 'Send CSAT after resolution', active: true  },
]

export function SettingsPage() {
  const [rules, setRules] = useState(DEFAULT_RULES)

  const toggleRule = (idx: number) => {
    setRules(r => r.map((rule, i) => i === idx ? { ...rule, active: !rule.active } : rule))
  }

  return (
    <div className="flex-1 overflow-y-auto p-6">
      <h1 className="font-display font-semibold text-lg mb-6" style={{ color: 'var(--ink)' }}>Settings</h1>
      <div className="max-w-xl flex flex-col gap-4">

        {/* SLA */}
        <div className="card p-5">
          <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--ink)' }}>SLA policies</h2>
          <div className="flex flex-col gap-3">
            {DEFAULT_SLA.map(p => (
              <div key={p.priority} className="flex items-center justify-between py-2" style={{ borderBottom: '1px solid var(--border)' }}>
                <span className="text-sm font-medium">{p.priority}</span>
                <div className="flex gap-6 text-sm" style={{ color: 'var(--ink-2)' }}>
                  <span>First response: <strong style={{ color: 'var(--ink)' }}>{p.first}</strong></span>
                  <span>Resolve: <strong style={{ color: 'var(--ink)' }}>{p.resolve}</strong></span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Automation */}
        <div className="card p-5">
          <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--ink)' }}>Automation rules</h2>
          <div className="flex flex-col gap-3">
            {rules.map((r, i) => (
              <div key={r.name} className="flex items-center justify-between">
                <span className="text-sm">{r.name}</span>
                <button
                  onClick={() => toggleRule(i)}
                  className={`badge text-[11px] cursor-pointer ${r.active
                    ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
                    : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'
                  }`}
                >
                  {r.active ? 'Active' : 'Inactive'}
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Business hours */}
        <div className="card p-5">
          <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--ink)' }}>Business hours</h2>
          <p className="text-sm" style={{ color: 'var(--ink-2)' }}>
            Monday – Friday, 08:00 – 17:00 <span style={{ color: 'var(--ink-3)' }}>(Africa/Johannesburg)</span>
          </p>
        </div>
      </div>
    </div>
  )
}
