import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../api'
import { useAuth, useTheme } from '../store'
import { Spinner } from '../components/ui'

export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const data = await authApi.login(email, password)
      login({
        userId:             data.userId,
        fullName:           data.fullName,
        role:               data.role,
        accessToken:        data.accessToken,
        refreshToken:       data.refreshToken,
        mustChangePassword: data.mustChangePassword,
      })
      navigate(data.mustChangePassword ? '/change-password' : '/tickets')
    } catch {
      setError('Invalid email or password.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center px-4"
      style={{ background: 'var(--surface-1)' }}
    >
      <div style={{ width: 400 }}>
        {/* Logo mark */}
        <div className="flex flex-col items-center mb-8">
          <div
            className="w-12 h-12 rounded-2xl flex items-center justify-center text-white text-xl mb-4"
            style={{ background: 'var(--accent)' }}
          >
            <i className="fa-solid fa-headset" />
          </div>
          <h1 className="font-display font-semibold text-2xl tracking-tight" style={{ color: 'var(--ink)' }}>
            SupportDesk
          </h1>
          <p className="text-sm mt-1" style={{ color: 'var(--ink-3)' }}>Sign in to your account</p>
        </div>

        <div className="card p-8">
          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            <div>
              <label className="block text-sm font-medium mb-1.5" style={{ color: 'var(--ink-2)' }}>
                Email address
              </label>
              <input
                className="input-base"
                type="email"
                autoComplete="email"
                placeholder="you@company.com"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1.5" style={{ color: 'var(--ink-2)' }}>
                Password
              </label>
              <input
                className="input-base"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
              />
            </div>

            {error && (
              <div className="rounded-lg px-4 py-3 text-sm text-red-600 bg-red-50 dark:bg-red-900/20 dark:text-red-400">
                <i className="fa-solid fa-triangle-exclamation mr-2" />
                {error}
              </div>
            )}

            <button
              type="submit"
              className="btn btn-primary w-full justify-center py-2.5"
              disabled={loading}
            >
              {loading ? <Spinner size={16} /> : null}
              Sign in
            </button>
          </form>
        </div>

        <p className="text-center text-xs mt-4" style={{ color: 'var(--ink-3)' }}>
          SupportDesk · Adapt IT
        </p>
      </div>
    </div>
  )
}
