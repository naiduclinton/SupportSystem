import React, { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from 'react-query'
import { format } from 'date-fns'
import { clsx } from 'clsx'
import { ticketsApi } from '../api'
import { useDrawer, useAuth, useToasts } from '../store'
import { StatusBadge, PriorityBadge, Avatar, Spinner } from './ui'
import type { TicketStatus } from '../types'

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: 'Open',       label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Pending',    label: 'Pending' },
  { value: 'Resolved',   label: 'Resolved' },
  { value: 'Closed',     label: 'Closed' },
]

export default function TicketDrawer({ onUpdated }: { onUpdated: () => void }) {
  const { ticketId, close } = useDrawer()
  const { userId } = useAuth()
  const { add: toast } = useToasts()

  const [replyBody, setReplyBody] = useState('')
  const [replyType, setReplyType] = useState<'reply' | 'internal_note'>('reply')
  const [activeTab, setActiveTab] = useState<'thread' | 'details'>('thread')

  const { data: ticket, isLoading, refetch } = useQuery(
    ['ticket', ticketId],
    () => ticketsApi.getById(ticketId!),
    { enabled: !!ticketId }
  )

  const statusMut = useMutation(
    (status: string) => {
      const actor = userId ?? '00000000-0000-0000-0000-000000000001'
      return ticketsApi.updateStatus(ticketId!, status, actor)
    },
    {
      onSuccess: () => { refetch(); onUpdated(); toast('Status updated', 'success') },
      onError: (err: any) => {
        const msg = err?.response?.data?.error ?? err?.message ?? 'Failed to update status'
        toast(msg, 'error')
      }
    }
  )

  const commentMut = useMutation(
    () => ticketsApi.addComment(ticketId!, replyBody, replyType, userId ?? undefined),
    {
      onSuccess: () => { setReplyBody(''); refetch(); toast('Comment added', 'success') },
      onError: () => toast('Failed to add comment', 'error')
    }
  )

  return (
    <>
      <div className="drawer-backdrop" onClick={close} />
      <div className="drawer flex flex-col">
        {/* Header */}
        <div className="flex items-start justify-between p-5" style={{ borderBottom: '1px solid var(--border)' }}>
          {isLoading || !ticket ? (
            <div className="flex items-center gap-2" style={{ color: 'var(--ink-3)' }}><Spinner size={14} /> Loading…</div>
          ) : (
            <div className="flex-1 min-w-0 mr-4">
              <div className="flex items-center gap-2 mb-1">
                <span className="font-mono text-xs" style={{ color: 'var(--ink-3)' }}>#{ticket.ticketNumber}</span>
                <StatusBadge status={ticket.status} />
                <PriorityBadge priority={ticket.priority} />
              </div>
              <h2 className="font-display font-semibold text-base leading-snug">{ticket.subject}</h2>
            </div>
          )}
          <button className="btn-ghost btn p-2" onClick={close}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        {ticket && (
          <>
            {/* Tabs */}
            <div className="px-5 tab-bar">
              <button className={clsx('tab', activeTab === 'thread' && 'active')} onClick={() => setActiveTab('thread')}>
                Thread
              </button>
              <button className={clsx('tab', activeTab === 'details' && 'active')} onClick={() => setActiveTab('details')}>
                Details
              </button>
            </div>

            {/* Thread tab */}
            {activeTab === 'thread' && (
              <div className="flex-1 overflow-y-auto p-5 flex flex-col gap-4">
                {/* Description */}
                {ticket.description && (
                  <div
                    className="rounded-lg p-4 text-sm"
                    style={{ background: 'var(--surface-1)', color: 'var(--ink-2)', lineHeight: 1.7 }}
                  >
                    {ticket.description}
                  </div>
                )}

                {/* Comments */}
                {ticket.comments?.map(c => (
                  <div key={c.id} className={clsx('rounded-lg p-4 text-sm', c.commentType === 'internal_note' ? 'border-l-2 border-amber-400' : '')}
                    style={{
                      background: c.commentType === 'internal_note' ? 'rgba(251,191,36,0.06)' : 'var(--surface-1)',
                    }}
                  >
                    <div className="flex items-center gap-2 mb-2">
                      {(c.authorUser?.fullName || c.authorCustomer?.fullName) && (
                        <Avatar name={c.authorUser?.fullName ?? c.authorCustomer?.fullName ?? 'Unknown'} size={22} />
                      )}
                      <span className="font-medium text-xs" style={{ color: 'var(--ink)' }}>
                        {c.authorUser?.fullName ?? c.authorCustomer?.fullName ?? 'Unknown'}
                      </span>
                      {c.commentType === 'internal_note' && (
                        <span className="badge bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 text-[10px]">
                          Internal note
                        </span>
                      )}
                      <span className="text-xs ml-auto" style={{ color: 'var(--ink-3)' }}>
                        {format(new Date(c.createdAt), 'MMM d, HH:mm')}
                      </span>
                    </div>
                    <p style={{ color: 'var(--ink-2)', lineHeight: 1.65 }}>{c.body}</p>
                  </div>
                ))}

                {(!ticket.comments || ticket.comments.length === 0) && !ticket.description && (
                  <p className="text-sm text-center py-8" style={{ color: 'var(--ink-3)' }}>No activity yet.</p>
                )}

                {/* Reply box */}
                <div className="mt-auto pt-2" style={{ borderTop: '1px solid var(--border)' }}>
                  <div className="flex gap-2 mb-2">
                    {(['reply', 'internal_note'] as const).map(t => (
                      <button
                        key={t}
                        onClick={() => setReplyType(t)}
                        className={clsx('text-xs px-3 py-1.5 rounded-full border transition-colors', replyType === t
                          ? t === 'reply' ? 'bg-blue-50 border-blue-200 text-blue-600 font-medium' : 'bg-amber-50 border-amber-200 text-amber-700 font-medium'
                          : 'border-transparent text-[color:var(--ink-2)] hover:bg-[var(--surface-2)]'
                        )}
                      >
                        {t === 'reply' ? 'Reply' : 'Internal note'}
                      </button>
                    ))}
                  </div>
                  <textarea
                    className="input-base text-sm resize-none"
                    rows={3}
                    placeholder={replyType === 'reply' ? 'Write a reply…' : 'Write an internal note (not visible to customer)…'}
                    value={replyBody}
                    onChange={e => setReplyBody(e.target.value)}
                  />
                  <div className="flex justify-end mt-2">
                    <button
                      className="btn btn-primary text-sm"
                      disabled={!replyBody.trim() || commentMut.isLoading}
                      onClick={() => commentMut.mutate()}
                    >
                      {commentMut.isLoading ? <Spinner size={14} /> : <i className="fa-solid fa-paper-plane text-xs" />}
                      {replyType === 'reply' ? 'Send reply' : 'Add note'}
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* Details tab */}
            {activeTab === 'details' && (
              <div className="flex-1 overflow-y-auto p-5">
                <div className="grid grid-cols-2 gap-4 mb-6">
                  {[
                    { label: 'Customer', value: (ticket as any).customer?.fullName || ticket.customerName || '—' },
                    { label: 'Email',    value: (ticket as any).customer?.email || ticket.customerEmail || '—' },
                    { label: 'Channel',  value: ticket.channel },
                    { label: 'Category', value: ticket.categoryName ?? '—' },
                    { label: 'Team',     value: ticket.teamName ?? '—' },
                    { label: 'Created',  value: format(new Date(ticket.createdAt), 'MMM d, yyyy HH:mm') },
                  ].map(f => (
                    <div key={f.label}>
                      <div className="text-[11px] uppercase tracking-wide mb-1" style={{ color: 'var(--ink-3)' }}>{f.label}</div>
                      <div className="text-sm" style={{ color: 'var(--ink)' }}>{f.value}</div>
                    </div>
                  ))}
                </div>

                {/* SLA info */}
                {ticket.resolutionDueAt && (
                  <div className="rounded-lg p-4 mb-4" style={{ background: 'var(--surface-1)' }}>
                    <div className="text-xs uppercase tracking-wide mb-2" style={{ color: 'var(--ink-3)' }}>SLA</div>
                    <div className="flex justify-between text-sm">
                      <span style={{ color: 'var(--ink-2)' }}>Resolution due</span>
                      <span className="font-medium" style={{ color: ticket.slaBreached ? '#EF4444' : 'var(--ink)' }}>
                        {format(new Date(ticket.resolutionDueAt), 'MMM d, HH:mm')}
                      </span>
                    </div>
                    {ticket.slaBreached && (
                      <p className="text-xs mt-2 text-red-500 font-medium">⚠ SLA breached</p>
                    )}
                  </div>
                )}

                {/* Update status */}
                <div className="rounded-lg p-4" style={{ background: 'var(--surface-1)' }}>
                  <div className="text-xs uppercase tracking-wide mb-3" style={{ color: 'var(--ink-3)' }}>Update status</div>
                  <div className="grid grid-cols-2 gap-2">
                    {STATUS_OPTIONS.map(o => (
                      <button
                        key={o.value}
                        className={clsx('btn text-xs py-2', ticket.status?.toLowerCase() === o.value.toLowerCase() && 'btn-primary')}
                        onClick={() => statusMut.mutate(o.value)}
                        disabled={statusMut.isLoading || ticket.status?.toLowerCase() === o.value.toLowerCase()}
                      >
                        {o.label}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </>
  )
}
