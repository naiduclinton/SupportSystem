using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SupportTicketing.Api.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly IConfiguration _config;

    public SetupController(IConfiguration config)
    {
        _config = config;
    }

    private IDbConnection GetConnection()
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                      ?? _config.GetConnectionString("DefaultConnection")
                      ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        // Convert postgres:// URL format to Npgsql connection string if needed
        if (connStr != null && connStr.StartsWith("postgres"))
        {
            var uri = new Uri(connStr.Replace("postgres://", "http://").Replace("postgresql://", "http://"));
            var userInfo = uri.UserInfo.Split(':');
            connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }

        return new NpgsqlConnection(connStr);
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        try
        {
            var rawConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                          ?? _config.GetConnectionString("DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? "NOT SET";

            // Mask password
            var masked = System.Text.RegularExpressions.Regex.Replace(rawConn, @"(?<=Password=|:[^@]{0,3})[^;@]+(?=;|@)", "***");

            using var db = GetConnection();
            db.Open();
            var version = await db.ExecuteScalarAsync<string>("SELECT version()");
            return Ok(new { status = "ok", database = "connected", version, connectionString = masked });
        }
        catch (Exception ex)
        {
            var rawConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                          ?? _config.GetConnectionString("DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? "NOT SET";
            var masked = System.Text.RegularExpressions.Regex.Replace(rawConn, @"(?<=Password=|:[^@]{0,3})[^;@]+(?=;|@)", "***");
            return StatusCode(500, new { status = "error", error = ex.Message, connectionString = masked });
        }
    }

    [HttpPost("migrate")]
    public async Task<IActionResult> Migrate([FromBody] SetupKeyRequest request)
    {
        var expectedKey = _config["SetupKey"];
        if (string.IsNullOrEmpty(expectedKey) || request.SetupKey != expectedKey)
            return Unauthorized(new { error = "Invalid setup key." });

        try
        {
            using var db = GetConnection();
            db.Open();

            await db.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
            await db.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS citext;");

            var enums = new[]
            {
                "DO $$ BEGIN CREATE TYPE ticket_status AS ENUM ('open','in_progress','pending','resolved','closed'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE ticket_priority AS ENUM ('low','medium','high','critical'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE ticket_channel AS ENUM ('email','portal','phone','chat','api'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE comment_type AS ENUM ('reply','internal_note'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE user_role AS ENUM ('admin','agent','viewer'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE notification_channel AS ENUM ('email','in_app','sms'); EXCEPTION WHEN duplicate_object THEN null; END $$;",
                "DO $$ BEGIN CREATE TYPE automation_trigger AS ENUM ('ticket_created','ticket_updated','sla_breached','status_changed','idle_timeout'); EXCEPTION WHEN duplicate_object THEN null; END $$;"
            };
            foreach (var sql in enums) await db.ExecuteAsync(sql);

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS teams (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(100) NOT NULL, description TEXT, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ);");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS users (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), email CITEXT NOT NULL UNIQUE, full_name VARCHAR(150) NOT NULL, avatar_url TEXT, role user_role NOT NULL DEFAULT 'agent', team_id UUID REFERENCES teams(id) ON DELETE SET NULL, is_active BOOLEAN NOT NULL DEFAULT TRUE, password_hash TEXT, last_login_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ);");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS customers (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), email CITEXT NOT NULL UNIQUE, full_name VARCHAR(150), phone VARCHAR(30), company VARCHAR(150), external_id VARCHAR(100), metadata JSONB DEFAULT '{}', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ);");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS categories (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(100) NOT NULL UNIQUE, description TEXT, parent_id UUID REFERENCES categories(id) ON DELETE SET NULL, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS sla_policies (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(150) NOT NULL, description TEXT, priority ticket_priority NOT NULL, first_response_minutes INT NOT NULL, resolution_minutes INT NOT NULL, business_hours_only BOOLEAN NOT NULL DEFAULT TRUE, is_default BOOLEAN NOT NULL DEFAULT FALSE, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW());");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS tickets (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), ticket_number BIGSERIAL NOT NULL UNIQUE, subject VARCHAR(500) NOT NULL, description TEXT, status ticket_status NOT NULL DEFAULT 'open', priority ticket_priority NOT NULL DEFAULT 'medium', channel ticket_channel NOT NULL DEFAULT 'portal', customer_id UUID NOT NULL REFERENCES customers(id), assignee_id UUID REFERENCES users(id) ON DELETE SET NULL, team_id UUID REFERENCES teams(id) ON DELETE SET NULL, category_id UUID REFERENCES categories(id) ON DELETE SET NULL, sla_policy_id UUID REFERENCES sla_policies(id) ON DELETE SET NULL, first_response_due_at TIMESTAMPTZ, resolution_due_at TIMESTAMPTZ, first_responded_at TIMESTAMPTZ, resolved_at TIMESTAMPTZ, closed_at TIMESTAMPTZ, sla_breached BOOLEAN NOT NULL DEFAULT FALSE, external_ref VARCHAR(100), metadata JSONB DEFAULT '{}', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ);");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS tags (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(80) NOT NULL UNIQUE);");
            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS ticket_tags (ticket_id UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE, tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE, PRIMARY KEY (ticket_id, tag_id));");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS comments (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), ticket_id UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE, author_user_id UUID REFERENCES users(id) ON DELETE SET NULL, author_customer_id UUID REFERENCES customers(id) ON DELETE SET NULL, comment_type comment_type NOT NULL DEFAULT 'reply', body TEXT NOT NULL, is_edited BOOLEAN NOT NULL DEFAULT FALSE, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ);");

            await db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS notifications (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), user_id UUID REFERENCES users(id) ON DELETE CASCADE, ticket_id UUID REFERENCES tickets(id) ON DELETE CASCADE, channel notification_channel NOT NULL DEFAULT 'in_app', subject VARCHAR(300), body TEXT NOT NULL, is_read BOOLEAN NOT NULL DEFAULT FALSE, sent_at TIMESTAMPTZ, read_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());");

            await db.ExecuteAsync(@"INSERT INTO categories (name, description) VALUES ('Billing','Payment and invoice queries'),('Technical','Bugs and product issues'),('Account','Login and access'),('General','General enquiries') ON CONFLICT (name) DO NOTHING;");

            await db.ExecuteAsync(@"INSERT INTO sla_policies (name, priority, first_response_minutes, resolution_minutes, is_default) VALUES ('Critical SLA','critical',60,240,TRUE),('High SLA','high',240,1440,TRUE),('Medium SLA','medium',480,4320,TRUE),('Low SLA','low',1440,10080,TRUE) ON CONFLICT DO NOTHING;");

            return Ok(new { status = "ok", message = "Schema created and seeded successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromBody] SeedAdminRequest request)
    {
        var expectedKey = _config["SetupKey"];
        if (string.IsNullOrEmpty(expectedKey) || request.SetupKey != expectedKey)
            return Unauthorized(new { error = "Invalid setup key." });

        try
        {
            using var db = GetConnection();
            db.Open();

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

            var result = await db.QueryFirstOrDefaultAsync(@"
                INSERT INTO users (id, email, full_name, role, is_active, password_hash, created_at, updated_at)
                VALUES (gen_random_uuid(), @Email, @FullName, 'admin', true, @Hash, NOW(), NOW())
                ON CONFLICT (email) DO UPDATE SET password_hash = EXCLUDED.password_hash, full_name = EXCLUDED.full_name, role = EXCLUDED.role, is_active = EXCLUDED.is_active, updated_at = NOW()
                RETURNING id, email, full_name, role;",
                new { Email = request.Email, FullName = request.FullName, Hash = hash });

            return Ok(new { message = "Admin user created.", email = result?.email, role = result?.role });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}

public record SetupKeyRequest(string SetupKey);
public record SeedAdminRequest(string SetupKey, string Email, string FullName, string Password);
