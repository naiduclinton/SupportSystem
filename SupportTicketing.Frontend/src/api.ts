import axios from 'axios'
import type {
  AuthResult, CreateTicketDto, PagedResult,
  TicketSummary, Ticket, TicketSearchParams,
  DashboardStats, AgentWorkload, User
} from './types'

// ── Axios instance ─────────────────────────────────────────────────────────
const api = axios.create({
  baseURL: (import.meta.env.VITE_API_BASE_URL || '') + '/api',
  headers: { 'Content-Type': 'application/json' }
})

// Attach JWT to every request
api.interceptors.request.use(cfg => {
  const token = localStorage.getItem('access_token')
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

// Refresh token on 401
api.interceptors.response.use(
  r => r,
  async err => {
    const original = err.config
    if (err.response?.status === 401 && !original._retry) {
      original._retry = true
      const refresh = localStorage.getItem('refresh_token')
      if (refresh) {
        try {
          const { data } = await axios.post<AuthResult>('/api/auth/refresh', refresh)
          localStorage.setItem('access_token', data.accessToken)
          original.headers.Authorization = `Bearer ${data.accessToken}`
          return api(original)
        } catch {
          localStorage.clear()
          window.location.href = '/login'
        }
      }
    }
    return Promise.reject(err)
  }
)

// ── Auth ───────────────────────────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResult>('/auth/login', { email, password }).then(r => r.data),
}

// ── Tickets ────────────────────────────────────────────────────────────────
export const ticketsApi = {
  list: (params: TicketSearchParams = {}) =>
    api.get<PagedResult<TicketSummary>>('/tickets', { params }).then(r => r.data),

  getById: (id: string) =>
    api.get<Ticket>(`/tickets/${id}`).then(r => r.data),

  create: (dto: CreateTicketDto) =>
    api.post<Ticket>('/tickets', dto).then(r => r.data),

  updateStatus: (id: string, status: string, actorUserId: string, note?: string) =>
    api.patch<Ticket>(`/tickets/${id}/status`, { status, actorUserId, note }).then(r => r.data),

  assign: (id: string, assigneeId?: string, teamId?: string, actorUserId?: string) =>
    api.patch<Ticket>(`/tickets/${id}/assign`, { assigneeId, teamId, actorUserId }).then(r => r.data),

  addComment: (ticketId: string, body: string, type: string, authorUserId?: string) =>
    api.post(`/tickets/${ticketId}/comments`, { body, commentType: type, authorUserId }).then(r => r.data),

  delete: (id: string) =>
    api.delete(`/tickets/${id}`),
}

// ── Reports ────────────────────────────────────────────────────────────────
export const reportsApi = {
  dashboard: () =>
    api.get<DashboardStats>('/reports/dashboard').then(r => r.data),
  agents: () =>
    api.get<AgentWorkload[]>('/reports/agents').then(r => r.data),
  sla: (from: string, to: string) =>
    api.get('/reports/sla', { params: { from, to } }).then(r => r.data),
}

// ── Users ──────────────────────────────────────────────────────────────────
export const usersApi = {
  list: () => api.get<User[]>('/users').then(r => r.data),
  getById: (id: string) => api.get<User>(`/users/${id}`).then(r => r.data),
}

// ── Notifications ──────────────────────────────────────────────────────────
export const notificationsApi = {
  unread: () => api.get('/notifications').then(r => r.data),
  markAllRead: () => api.post('/notifications/mark-all-read'),
}

export default api
