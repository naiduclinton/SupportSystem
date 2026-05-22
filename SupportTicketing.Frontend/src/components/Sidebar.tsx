import React from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { clsx } from 'clsx'
import { useAuth, useTheme } from '../store'
import { Avatar } from './ui'

const NAV = [
  { to: '/tickets',   icon: 'fa-ticket',         label: 'Tickets'      },
  { to: '/dashboard', icon: 'fa-chart-pie',       label: 'Dashboard'    },
  { to: '/accounts',  icon: 'fa-building',        label: 'Accounts'     },
  { to: '/reports',   icon: 'fa-chart-bar',       label: 'Reports'      },
  { to: '/report',    icon: 'fa-table-list',      label: 'Ticket report' },
  { to: '/agents',    icon: 'fa-users',           label: 'Agents'       },
  { to: '/kb',        icon: 'fa-book-open',       label: 'Knowledge Base'},
]

const SETTINGS_NAV = [
  { to: '/settings',  icon: 'fa-gear',            label: 'Settings'     },
]

export default function Sidebar() {
  const { fullName, role, logout } = useAuth()
  const { dark, toggle } = useTheme()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <aside
      style={{ background: 'var(--surface-0)', borderRight: '1px solid var(--border)' }}
      className="flex flex-col h-full overflow-hidden"
    >
      {/* Logo */}
      <div className="px-4 py-5 mb-1">
        <div className="flex items-center gap-2.5">
          <div
            className="w-7 h-7 rounded-lg flex items-center justify-center text-white text-xs font-bold"
            style={{ background: 'var(--accent)' }}
          >
            <i className="fa-solid fa-headset" />
          </div>
          <span className="font-display font-semibold text-base tracking-tight" style={{ color: 'var(--ink)' }}>
            SupportDesk
          </span>
        </div>
      </div>

      {/* Main nav */}
      <nav className="flex-1 px-2 overflow-y-auto">
        <div className="mb-1">
          <p className="px-3 mb-1 text-[10px] uppercase tracking-widest font-medium" style={{ color: 'var(--ink-3)' }}>
            Main
          </p>
          {NAV.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => clsx('nav-link', isActive && 'active')}
            >
              <i className={clsx('fa-solid text-sm w-4 text-center', item.icon)} aria-hidden="true" />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </div>

        <div className="mt-4">
          <p className="px-3 mb-1 text-[10px] uppercase tracking-widest font-medium" style={{ color: 'var(--ink-3)' }}>
            Config
          </p>
          {SETTINGS_NAV.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => clsx('nav-link', isActive && 'active')}
            >
              <i className={clsx('fa-solid text-sm w-4 text-center', item.icon)} aria-hidden="true" />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </div>
      </nav>

      {/* Bottom: theme toggle + user */}
      <div
        className="px-2 pt-2 pb-3"
        style={{ borderTop: '1px solid var(--border)' }}
      >
        <button
          onClick={toggle}
          className="nav-link w-full mb-1"
          title="Toggle theme"
        >
          <i className={clsx('fa-solid text-sm w-4 text-center', dark ? 'fa-sun' : 'fa-moon')} aria-hidden="true" />
          <span className="text-sm">{dark ? 'Light mode' : 'Dark mode'}</span>
        </button>

        <div className="nav-link cursor-default">
          {fullName && <Avatar name={fullName} size={26} />}
          <div className="flex-1 min-w-0">
            <p className="truncate-1 text-xs font-medium" style={{ color: 'var(--ink)' }}>{fullName}</p>
            <p className="truncate-1 text-[11px] capitalize" style={{ color: 'var(--ink-3)' }}>{role}</p>
          </div>
          <button
            onClick={handleLogout}
            className="btn-ghost btn p-1"
            title="Log out"
          >
            <i className="fa-solid fa-right-from-bracket text-xs" style={{ color: 'var(--ink-3)' }} />
          </button>
        </div>
      </div>
    </aside>
  )
}
