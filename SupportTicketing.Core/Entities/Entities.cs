using SupportTicketing.Core.Enums;

namespace SupportTicketing.Core.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Agent;
    public Guid? TeamId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool MustChangePassword { get; set; } = false;

    public Team? Team { get; set; }
}

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Members { get; set; } = new List<User>();
}

public class Customer : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? ExternalId { get; set; }
    public string Metadata { get; set; } = "{}";
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
}

public class SlaPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketPriority Priority { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

public class Ticket : BaseEntity
{
    public long TicketNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketChannel Channel { get; set; } = TicketChannel.Portal;

    public Guid CustomerId { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SlaPolicyId { get; set; }

    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool SlaBreached { get; set; } = false;

    // Account details
    public string? AccountHolder { get; set; }        // "AdaptIT" or "ChannelPartner"
    public string? ChannelPartnerName { get; set; }
    public string? AccountCustomer { get; set; }      // max 15 chars
    public string? AccountProduct { get; set; }       // max 10 chars

    public string? ExternalRef { get; set; }
    public string Metadata { get; set; } = "{}";

    // Navigation
    public Customer? Customer { get; set; }
    public User? Assignee { get; set; }
    public Team? Team { get; set; }
    public Category? Category { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

public class Comment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public Guid? AuthorCustomerId { get; set; }
    public CommentType CommentType { get; set; } = CommentType.Reply;
    public string Body { get; set; } = string.Empty;
    public bool IsEdited { get; set; } = false;

    public Ticket? Ticket { get; set; }
    public User? AuthorUser { get; set; }
    public Customer? AuthorCustomer { get; set; }
}

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class CsatSurvey : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid CustomerId { get; set; }
    public int? Score { get; set; }
    public string? Comment { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Ticket? Ticket { get; set; }
    public Customer? Customer { get; set; }
}

public class AutomationRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public AutomationTrigger TriggerEvent { get; set; }
    public string ConditionsJson { get; set; } = "[]";
    public string ActionsJson { get; set; } = "[]";
    [System.Text.Json.Serialization.JsonIgnore]
    public List<AutomationCondition> Conditions { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public List<AutomationAction> Actions { get; set; } = new();
    public int ExecutionOrder { get; set; } = 0;
}

public class AutomationCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty; // eq, neq, contains, gt, lt
    public string Value { get; set; } = string.Empty;
}

public class AutomationAction
{
    public string Action { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? TicketId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
