import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// ── Auth store ─────────────────────────────────────────────────────────────
interface AuthState {
  userId: string | null
  fullName: string | null
  role: string | null
  accessToken: string | null
  mustChangePassword: boolean
  login: (data: { userId: string; fullName: string; role: string; accessToken: string; refreshToken: string; mustChangePassword?: boolean }) => void
  clearMustChangePassword: () => void
  logout: () => void
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      userId: null,
      fullName: null,
      role: null,
      accessToken: null,
      mustChangePassword: false,
      login: ({ userId, fullName, role, accessToken, refreshToken, mustChangePassword }) => {
        localStorage.setItem('access_token', accessToken)
        localStorage.setItem('refresh_token', refreshToken)
        set({ userId, fullName, role, accessToken, mustChangePassword: mustChangePassword ?? false })
      },
      clearMustChangePassword: () => set({ mustChangePassword: false }),
      logout: () => {
        localStorage.removeItem('access_token')
        localStorage.removeItem('refresh_token')
        set({ userId: null, fullName: null, role: null, accessToken: null, mustChangePassword: false })
      }
    }),
    { name: 'auth-store', partialize: s => ({ userId: s.userId, fullName: s.fullName, role: s.role }) }
  )
)

// ── Theme store ────────────────────────────────────────────────────────────
interface ThemeState {
  dark: boolean
  toggle: () => void
}

export const useTheme = create<ThemeState>()(
  persist(
    (set, get) => ({
      dark: false,
      toggle: () => {
        const next = !get().dark
        document.documentElement.classList.toggle('dark', next)
        set({ dark: next })
      }
    }),
    { name: 'theme-store' }
  )
)

// ── Toast store ────────────────────────────────────────────────────────────
export type ToastType = 'success' | 'error' | 'info'

interface Toast {
  id: string
  message: string
  type: ToastType
}

interface ToastState {
  toasts: Toast[]
  add: (message: string, type?: ToastType) => void
  remove: (id: string) => void
}

export const useToasts = create<ToastState>((set) => ({
  toasts: [],
  add: (message, type = 'info') => {
    const id = Math.random().toString(36).slice(2)
    set(s => ({ toasts: [...s.toasts, { id, message, type }] }))
    setTimeout(() => set(s => ({ toasts: s.toasts.filter(t => t.id !== id) })), 3500)
  },
  remove: (id) => set(s => ({ toasts: s.toasts.filter(t => t.id !== id) }))
}))

// ── Ticket drawer store ────────────────────────────────────────────────────
interface DrawerState {
  ticketId: string | null
  open: (id: string) => void
  close: () => void
}

export const useDrawer = create<DrawerState>((set) => ({
  ticketId: null,
  open: (id) => set({ ticketId: id }),
  close: () => set({ ticketId: null })
}))
