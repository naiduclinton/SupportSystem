export type TicketStatus   = 'open' | 'in_progress' | 'pending' | 'resolved' | 'closed'
export type TicketPriority = 'low' | 'medium' | 'high' | 'critical'
export type TicketChannel  = 'email' | 'portal' | 'phone' | 'chat' | 'api'
export type CommentType    = 'reply' | 'internal_note'
export type UserRole       = 'admin' | 'agent' | 'viewer'

export interface TicketSummary {
  id: string
  ticketNumber: number
  subject: string
  status: TicketStatus
  priority: TicketPriority
  customerName: string
  customerEmail: string
  assigneeName?: string
  teamName?: string
  categoryName?: string
  slaBreached: boolean
  slaCompliancePct?: number
  resolutionMinutesRemaining?: number
  createdAt: string
  updatedAt: string
}

export interface Ticket extends TicketSummary {
  description?: string
  channel: TicketChannel
  customerId: string
  assigneeId?: string
  teamId?: string
  categoryId?: string
  firstResponseDueAt?: string
  resolutionDueAt?: string
  firstRespondedAt?: string
  resolvedAt?: string
  closedAt?: string
  comments: Comment[]
  tags: Tag[]
}

export interface Comment {
  id: string
  ticketId: string
  authorUserId?: string
  authorCustomerId?: string
  commentType: CommentType
  body: string
  isEdited: boolean
  createdAt: string
  authorUser?: User
  authorCustomer?: Customer
}

export interface User {
  id: string
  email: string
  fullName: string
  avatarUrl?: string
  role: UserRole
  teamId?: string
  isActive: boolean
}

export interface Customer {
  id: string
  email: string
  fullName?: string
  phone?: string
  company?: string
}

export interface Tag { id: string; name: string }

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface DashboardStats {
  openCount: number
  inProgressCount: number
  pendingCount: number
  resolvedTodayCount: number
  avgFirstResponseHours: number
  avgResolutionHours: number
  csatScore: number
  slaBreachCount: number
}

export interface AgentWorkload {
  userId: string
  fullName: string
  teamName?: string
  openCount: number
  inProgressCount: number
  pendingCount: number
  slaBreachCount: number
  avgResolutionHours?: number
}

export interface AuthResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  userId: string
  fullName: string
  role: string
}

export interface CreateTicketDto {
  subject: string
  description?: string
  priority: TicketPriority
  channel: TicketChannel
  customerEmail: string
  customerName?: string
  categoryId?: string
  assigneeId?: string
  teamId?: string
  tags?: string[]
}

export interface TicketSearchParams {
  search?: string
  status?: TicketStatus
  priority?: TicketPriority
  assigneeId?: string
  teamId?: string
  slaBreached?: boolean
  page?: number
  pageSize?: number
  sortBy?: string
  sortDesc?: boolean
}
