import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../api'
import { useAuth, useToasts } from '../store'
import { Spinner } from '../components/ui'

export default function ChangePasswordPage() {
  const { clearMustChangePassword, fullName } = useAuth()
  const { add: toast } = useToasts()
  const navigate = useNavigate()

  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const validate = () => {
    if (next.length < 8) return 'New password must be at least 8 characters.'
    if (next !== confirm) return 'Passwords do not match.'
    if (next === current) return 'New password must be different from your current password.'
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    const err = validate()
    if (err) { setError(err); return }

    setLoading(true)
    try {
      await authApi.changePassword(current, next)
      clearMustChangePassword()
      toast('Password changed successfully. Welcome!', 'success')
      navigate('/tickets')
    } catch (e: any) {
      setError(e?.response?.data?.error ?? 'Failed to change password. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const strength = (p: string) => {
    if (!p) return { score: 0, label: '', color: '' }
    let score = 0
    if (p.length >= 8) score++
    if (p.length >= 12) score++
    if (/[A-Z]/.test(p)) score++
    if (/[0-9]/.test(p)) score++
    if (/[^A-Za-z0-9]/.test(p)) score++
    if (score <= 1) return { score, label: 'Weak', color: '#EF4444' }
    if (score <= 3) return { score, label: 'Fair', color: '#F59E0B' }
    return { score, label: 'Strong', color: '#10B981' }
  }

  const pw = strength(next)

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ background: 'var(--surface-1)' }}>
      <div style={{ width: 420 }}>
        {/* Icon */}
        <div className="flex flex-col items-center mb-8">
          <div className="w-12 h-12 rounded-2xl flex items-center justify-center text-white text-xl mb-4" style={{ background: '#F59E0B' }}>
            <i className="fa-solid fa-key" />
          </div>
          <h1 className="font-display font-semibold text-2xl tracking-tight" style={{ color: 'var(--ink)' }}>
            Change your password
          </h1>
          <p className="text-sm mt-2 text-center" style={{ color: 'var(--ink-3)', maxWidth: 320 }}>
            Hi {fullName} — for security, you must set a new password before continuing.
          </p>
        </div>

        <div className="card p-8">
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">

            <div>
              <label className="block text-sm font-medium mb-1.5" style={{ color: 'var(--ink-2)' }}>
                Current (temporary) password
              </label>
              <input
                className="input-base"
                type="password"
                placeholder="Enter your temporary password"
                value={current}
                onChange={e => setCurrent(e.target.value)}
                required
                autoFocus
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1.5" style={{ color: 'var(--ink-2)' }}>
                New password
              </label>
              <input
                className="input-base"
                type="password"
                placeholder="Min. 8 characters"
                value={next}
                onChange={e => setNext(e.target.value)}
                required
              />
              {next && (
                <div className="mt-2">
                  <div className="flex gap-1 mb-1">
                    {[1,2,3,4,5].map(i => (
                      <div key={i} className="flex-1 h-1 rounded-full" style={{
                        background: i <= pw.score ? pw.color : 'var(--surface-3)',
                        transition: 'background 0.2s'
                      }} />
                    ))}
                  </div>
                  <p className="text-xs" style={{ color: pw.color }}>{pw.label}</p>
                </div>
              )}
              <ul className="mt-2 text-xs space-y-0.5" style={{ color: 'var(--ink-3)' }}>
                {[
                  { ok: next.length >= 8, text: 'At least 8 characters' },
                  { ok: /[A-Z]/.test(next), text: 'One uppercase letter' },
                  { ok: /[0-9]/.test(next), text: 'One number' },
                  { ok: /[^A-Za-z0-9]/.test(next), text: 'One special character' },
                ].map(r => (
                  <li key={r.text} className="flex items-center gap-1.5">
                    <i className={`fa-solid fa-${r.ok ? 'circle-check' : 'circle'} text-xs`}
                       style={{ color: r.ok ? '#10B981' : 'var(--ink-3)' }} />
                    <span style={{ color: r.ok ? '#10B981' : 'var(--ink-3)' }}>{r.text}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1.5" style={{ color: 'var(--ink-2)' }}>
                Confirm new password
              </label>
              <input
                className="input-base"
                type="password"
                placeholder="Re-enter your new password"
                value={confirm}
                onChange={e => setConfirm(e.target.value)}
                required
              />
              {confirm && next && (
                <p className="text-xs mt-1" style={{ color: next === confirm ? '#10B981' : '#EF4444' }}>
                  {next === confirm ? '✓ Passwords match' : 'Passwords do not match'}
                </p>
              )}
            </div>

            {error && (
              <div className="rounded-lg px-4 py-3 text-sm text-red-600 bg-red-50 dark:bg-red-900/20 dark:text-red-400">
                <i className="fa-solid fa-triangle-exclamation mr-2" />
                {error}
              </div>
            )}

            <button
              type="submit"
              className="btn btn-primary w-full justify-center py-2.5 mt-2"
              disabled={loading || !current || !next || !confirm}
            >
              {loading ? <Spinner size={16} /> : <i className="fa-solid fa-check text-xs" />}
              Set new password & continue
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
