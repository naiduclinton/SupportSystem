import React, { useState } from 'react'
import { createPortal } from 'react-dom'
import { useForm } from 'react-hook-form'
import { useMutation } from 'react-query'
import { ticketsApi } from '../api'
import type { CreateTicketDto } from '../types'
import { Spinner } from './ui'

const ACCOUNT_HOLDER_OPTIONS = [
  { value: '', label: 'Select…' },
  { value: 'AdaptIT', label: 'Adapt IT' },
  { value: 'ChannelPartner', label: 'Channel Partner' },
]

export default function NewTicketModal({ onClose, onCreated }: {
  onClose: () => void
  onCreated: () => void
}) {
  const { register, handleSubmit, watch, formState: { errors } } = useForm<CreateTicketDto>({
    defaultValues: { priority: 'medium', channel: 'portal' }
  })

  const accountHolder = watch('accountHolder')
  const isChannelPartner = accountHolder === 'ChannelPartner'
  const customerVal = watch('accountCustomer') ?? ''
  const productVal  = watch('accountProduct') ?? ''

  const mut = useMutation((dto: CreateTicketDto) => ticketsApi.create(dto), {
    onSuccess: onCreated
  })

  // Hierarchy steps
  const steps = isChannelPartner
    ? ['Account holder', 'Channel partner name', 'Customer', 'Product']
    : accountHolder
    ? ['Account holder', 'Customer', 'Product']
    : ['Account holder', 'Customer', 'Product']

  return createPortal(
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" style={{ width: 540 }} onClick={e => e.stopPropagation()}>

        {/* Header */}
        <div className="flex items-center justify-between px-6 pt-5 pb-4" style={{ borderBottom: '1px solid var(--border)' }}>
          <h2 className="font-display font-semibold text-base">New ticket</h2>
          <button className="btn-ghost btn p-2" onClick={onClose}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        <form onSubmit={handleSubmit(dto => mut.mutate(dto))} className="flex flex-col">
          <div className="px-6 py-5 flex flex-col gap-4" style={{ maxHeight: '70vh', overflowY: 'auto' }}>

            {/* ── Account details section ── */}
            <div>
              <p className="text-xs font-medium uppercase tracking-wider mb-3" style={{ color: 'var(--ink-3)' }}>
                Account details
              </p>

              {/* Hierarchy flow indicator */}
              {accountHolder && (
                <div className="flex items-center gap-1.5 flex-wrap mb-4 px-3 py-2 rounded-lg text-xs" style={{ background: 'var(--surface-2)' }}>
                  {steps.map((s, i) => (
                    <React.Fragment key={s}>
                      <span className="px-2 py-0.5 rounded-full font-medium" style={{
                        background: 'var(--surface-0)',
                        border: '1px solid var(--border)',
                        color: 'var(--accent-fg)'
                      }}>{s}</span>
                      {i < steps.length - 1 && <span style={{ color: 'var(--ink-3)' }}>›</span>}
                    </React.Fragment>
                  ))}
                </div>
              )}

              <div className="grid grid-cols-2 gap-3">
                {/* Account Holder */}
                <div>
                  <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
                    Account holder <span className="text-red-500">*</span>
                  </label>
                  <select className="input-base" {...register('accountHolder', { required: 'Required' })}>
                    {ACCOUNT_HOLDER_OPTIONS.map(o => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                  {errors.accountHolder && <p className="text-xs text-red-500 mt-1">{errors.accountHolder.message}</p>}
                </div>

                {/* Channel Partner Name — only shown when Channel Partner selected */}
                <div style={{ opacity: isChannelPartner ? 1 : 0.3, pointerEvents: isChannelPartner ? 'auto' : 'none', transition: 'opacity 0.2s' }}>
                  <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
                    Channel partner name {isChannelPartner && <span className="text-red-500">*</span>}
                  </label>
                  <input
                    className="input-base"
                    placeholder="e.g. ABC Solutions"
                    {...register('channelPartnerName', {
                      required: isChannelPartner ? 'Required for Channel Partner' : false
                    })}
                  />
                  {errors.channelPartnerName && <p className="text-xs text-red-500 mt-1">{errors.channelPartnerName.message}</p>}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3 mt-3">
                {/* Customer — max 15 */}
                <div>
                  <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
                    Customer <span className="text-red-500">*</span>
                  </label>
                  <input
                    className="input-base"
                    placeholder="e.g. Acme Corp"
                    maxLength={15}
                    {...register('accountCustomer', { required: 'Required', maxLength: { value: 15, message: 'Max 15 characters' } })}
                  />
                  <div className="flex justify-between mt-1">
                    <span className="text-xs" style={{ color: 'var(--ink-3)' }}>Free text</span>
                    <span className={`text-xs font-mono ${customerVal.length >= 15 ? 'text-red-500' : ''}`} style={{ color: customerVal.length >= 15 ? undefined : 'var(--ink-3)' }}>
                      {customerVal.length} / 15
                    </span>
                  </div>
                  {errors.accountCustomer && <p className="text-xs text-red-500 mt-1">{errors.accountCustomer.message}</p>}
                </div>

                {/* Product — max 10 */}
                <div>
                  <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>
                    Product <span className="text-red-500">*</span>
                  </label>
                  <input
                    className="input-base"
                    placeholder="e.g. EPM Cloud"
                    maxLength={10}
                    {...register('accountProduct', { required: 'Required', maxLength: { value: 10, message: 'Max 10 characters' } })}
                  />
                  <div className="flex justify-between mt-1">
                    <span className="text-xs" style={{ color: 'var(--ink-3)' }}>Free text</span>
                    <span className={`text-xs font-mono ${productVal.length >= 10 ? 'text-red-500' : ''}`} style={{ color: productVal.length >= 10 ? undefined : 'var(--ink-3)' }}>
                      {productVal.length} / 10
                    </span>
                  </div>
                  {errors.accountProduct && <p className="text-xs text-red-500 mt-1">{errors.accountProduct.message}</p>}
                </div>
              </div>
            </div>

            {/* Divider */}
            <div style={{ height: '1px', background: 'var(--border)' }} />

            {/* ── Ticket details section ── */}
            <div>
              <p className="text-xs font-medium uppercase tracking-wider mb-3" style={{ color: 'var(--ink-3)' }}>
                Ticket details
              </p>

              <div className="flex flex-col gap-3">
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
                  <label className="block text-xs uppercase tracking-wide mb-1.5" style={{ color: 'var(--ink-2)' }}>Description</label>
                  <textarea
                    className="input-base resize-none"
                    rows={3}
                    placeholder="Full details of the issue…"
                    {...register('description')}
                  />
                </div>
              </div>
            </div>

            {mut.isError && (
              <p className="text-xs text-red-500">Failed to create ticket. Please try again.</p>
            )}
          </div>

          {/* Footer */}
          <div className="flex gap-2 justify-end px-6 py-4" style={{ borderTop: '1px solid var(--border)' }}>
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={mut.isLoading}>
              {mut.isLoading ? <Spinner size={14} /> : <i className="fa-solid fa-check text-xs" />}
              Create ticket
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body
  )
}
