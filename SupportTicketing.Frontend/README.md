# SupportDesk — React Frontend

React 18 + TypeScript + Vite frontend for the support ticketing system.

**Stack:** React 18 · TypeScript · Vite · Tailwind CSS · React Query · Zustand · Recharts · React Hook Form · Axios

---

## Project structure

```
src/
├── api.ts              # Axios client + all API calls
├── store.ts            # Zustand stores: auth, theme, toasts, drawer
├── types.ts            # All TypeScript types/interfaces
├── App.tsx             # Router, QueryClient, theme init
├── index.css           # Design tokens (CSS vars), global styles, utility classes
│
├── components/
│   ├── ui.tsx          # Reusable atoms: StatusBadge, PriorityBadge, Avatar, SlaBar, Toast, ...
│   ├── Sidebar.tsx     # Navigation sidebar with theme toggle + user profile
│   ├── TicketDrawer.tsx # Slide-in ticket detail panel with thread + status updates
│   └── NewTicketModal.tsx # Create ticket modal (React Hook Form)
│
└── pages/
    ├── LoginPage.tsx   # JWT login form
    ├── TicketsPage.tsx # Main ticket list: search, filter, sort, pagination
    ├── DashboardPage.tsx # Stats cards + Recharts volume/category charts
    └── OtherPages.tsx  # Reports, Agents, Knowledge Base, Settings
```

---

## Quick start

```bash
npm install
npm run dev       # http://localhost:3000
```

The dev server proxies `/api/*` to `http://localhost:5000` (the C# API).

---

## Key features

### Authentication
- JWT with automatic refresh via Axios interceptor
- Route guard (`RequireAuth`) redirects unauthenticated users to `/login`
- Auth state persisted via Zustand + localStorage

### Ticket list
- Server-side search, filter (status, priority, SLA breach), sort, pagination
- Debounced search input (400ms)
- Click row → slide-in detail drawer

### Ticket drawer
- Thread tab: comments, internal notes, reply box
- Details tab: metadata, SLA info, status update buttons
- Optimistic-friendly with React Query mutations

### New ticket modal
- React Hook Form with validation
- Priority, channel, category, assignee fields
- Submits to POST /api/tickets

### Dashboard
- Real-time stat cards wired to /api/reports/dashboard
- Area chart (ticket volume trend) via Recharts
- Horizontal bar chart (tickets by category)
- Agent workload table

### Theme
- Light/dark mode via CSS variables + Tailwind `dark:` classes
- Persisted to localStorage via Zustand
- Toggle in sidebar

### Design system
- Fonts: Syne (display/headings) + DM Sans (body) + JetBrains Mono (numbers/IDs)
- CSS variable tokens: `--surface-*`, `--ink-*`, `--accent-*`, `--border`
- Status/priority badges, SLA progress bars, avatar initials with consistent color mapping

---

## Environment

Create `.env.local` for custom API URL:

```
VITE_API_BASE_URL=https://your-api.domain.com
```

Then update `api.ts` `baseURL` to use `import.meta.env.VITE_API_BASE_URL`.
