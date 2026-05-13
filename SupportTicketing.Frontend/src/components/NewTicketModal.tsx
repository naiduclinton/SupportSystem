import React from 'react'
import { createPortal } from 'react-dom'
import { useForm } from 'react-hook-form'
import { useMutation } from 'react-query'
import { ticketsApi } from '../api'
import type { CreateTicketDto } from '../types'
import { Spinner } from './ui'

export default function NewTicketModal({ onClose, onCreated }: {
  onClose: () => void
  onCreated: () => void
}) {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateTicketDto>({
    defaultValues: { priority: 'medium', channel: 'portal' }
  })

  const mut = useMutation((dto: CreateTicketDto) => ticketsApi.create(dto), {
    onSuccess: onCreated
  })

  return createPortal(
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between px-6 pt-6 pb-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="font-display font-semibold text-base">New ticket</h2>
          <button className="btn-ghost btn p-2" onClick={onClose}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        <form onSubmit={handleSubmit(dto => mut.mutate(dto))} className="p-6 flex flex-col gap-4">
          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
              Subject <span className="text-red-500">*</span>
            </label>
            <input
              className="input-base"
              placeholder="Brief description of the issue"
              {...register('subject', { required: 'Subject is required' })}
            />
            {errors.subject && <p className="text-xs text-red-500 mt-1">{errors.subject.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Priority</label>
              <select className="input-base" {...register('priority')}>
                <option value="low">Low</option>
                <option value="medium">Medium</option>
                <option value="high">High</option>
                <option value="critical">Critical</option>
              </select>
            </div>
            <div>
              <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Channel</label>
              <select className="input-base" {...register('channel')}>
                <option value="portal">Portal</option>
                <option value="email">Email</option>
                <option value="phone">Phone</option>
                <option value="chat">Chat</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
              Customer email <span className="text-red-500">*</span>
            </label>
            <input
              className="input-base"
              type="email"
              placeholder="customer@company.com"
              {...register('customerEmail', { required: 'Email is required' })}
            />
            {errors.customerEmail && <p className="text-xs text-red-500 mt-1">{errors.customerEmail.message}</p>}
          </div>

          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Customer name</label>
            <input className="input-base" placeholder="Full name (optional)" {...register('customerName')} />
          </div>

          <div>
            <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Description</label>
            <textarea
              className="input-base resize-none"
              rows={4}
              placeholder="Full details of the issue…"
              {...register('description')}
            />
          </div>

          {mut.isError && (
            <p className="text-xs text-red-500">Failed to create ticket. Please try again.</p>
          )}

          <div className="flex gap-2 justify-end pt-2" style={{ borderTop: '1px solid var(--border)' }}>
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={mut.isLoading}>
              {mut.isLoading ? <Spinner size={14} /> : <i className="fa-solid fa-check text-xs" />}
              Create ticket
            </button>
          </div>
        </form>
      </div>
    </div>
  , document.body)
}
