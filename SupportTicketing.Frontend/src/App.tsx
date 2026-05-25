import React, { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from 'react-query'
import Sidebar from './components/Sidebar'
import TicketsPage from './pages/TicketsPage'
import DashboardPage from './pages/DashboardPage'
import AccountDashboard from './pages/AccountDashboard'
import LoginPage from './pages/LoginPage'
import { ReportsPage, AgentsPage, KbPage, SettingsPage } from './pages/OtherPages'
import TicketReport from './pages/TicketReport'
import ChangePasswordPage from './pages/ChangePasswordPage'
import { ToastContainer } from './components/ui'
import { useAuth, useTheme } from './store'

const qc = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: true, staleTime: 10_000 }
  }
})

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { accessToken, mustChangePassword } = useAuth()
  if (!accessToken) return <Navigate to="/login" replace />
  if (mustChangePassword) return <Navigate to="/change-password" replace />
  return <>{children}</>
}

function AppShell() {
  return (
    <div className="app-shell">
      <Sidebar />
      <main className="flex flex-col overflow-y-auto" style={{ background: 'var(--surface-1)' }}>
        <Routes>
          <Route path="/tickets"   element={<TicketsPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/accounts" element={<AccountDashboard />} />
          <Route path="/reports"   element={<ReportsPage />} />
          <Route path="/report"    element={<TicketReport />} />
          <Route path="/agents"    element={<AgentsPage />} />
          <Route path="/kb"        element={<KbPage />} />
          <Route path="/settings"  element={<SettingsPage />} />
          <Route path="*"          element={<Navigate to="/tickets" replace />} />
        </Routes>
      </main>
    </div>
  )
}

export default function App() {
  const { dark } = useTheme()

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark)
  }, [dark])

  return (
    <QueryClientProvider client={qc}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/change-password" element={<ChangePasswordPage />} />
          <Route path="/*" element={
            <RequireAuth>
              <AppShell />
            </RequireAuth>
          } />
        </Routes>
        <ToastContainer />
      </BrowserRouter>
    </QueryClientProvider>
  )
}
