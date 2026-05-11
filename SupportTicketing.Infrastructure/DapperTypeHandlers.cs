using System.Data;
using Dapper;
using SupportTicketing.Core.Enums;

namespace SupportTicketing.Infrastructure;

/// <summary>
/// Registers Dapper type handlers for PostgreSQL enum types.
/// Call DapperConfig.Configure() in Program.cs before any DB access.
/// </summary>
public static class DapperConfig
{
    public static void Configure()
    {
        SqlMapper.AddTypeHandler(new EnumTypeHandler<TicketStatus>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<TicketPriority>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<TicketChannel>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<CommentType>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<UserRole>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<NotificationChannel>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<AutomationTrigger>());

        // Handle Guid? (nullable Guid) from PostgreSQL UUID
        SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
    }
}

public class EnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override T Parse(object value)
    {
        if (value is T enumVal) return enumVal;
        var str = value?.ToString() ?? string.Empty;
        // Handle snake_case from PostgreSQL (e.g. "in_progress" -> "InProgress")
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            str, "_([a-z])", m => m.Groups[1].Value.ToUpper());
        normalized = char.ToUpper(normalized[0]) + normalized[1..];
        return Enum.TryParse<T>(normalized, true, out var result) ? result : default;
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = value.ToString().ToLower();
        // Convert PascalCase to snake_case for PostgreSQL enums
        parameter.Value = System.Text.RegularExpressions.Regex.Replace(
            value.ToString(),
            "([A-Z])",
            m => "_" + m.Value.ToLower()).TrimStart('_');
    }
}

public class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        if (value == null || value == DBNull.Value) return null;
        if (value is Guid g) return g;
        return Guid.TryParse(value.ToString(), out var result) ? result : null;
    }

    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
    }
}
