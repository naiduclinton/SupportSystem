namespace SupportTicketing.Core.Enums;

public enum TicketStatus
{
    Open,
    InProgress,
    Pending,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketChannel
{
    Email,
    Portal,
    Phone,
    Chat,
    Api
}

public enum CommentType
{
    Reply,
    InternalNote
}

public enum UserRole
{
    Admin,
    Agent,
    Viewer
}

public enum NotificationChannel
{
    Email,
    InApp,
    Sms
}

public enum AutomationTrigger
{
    TicketCreated,
    TicketUpdated,
    SlaBreach,
    StatusChanged,
    IdleTimeout
}
