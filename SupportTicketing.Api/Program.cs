using System.Data;
using Dapper;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Services;
using SupportTicketing.Infrastructure.Messaging;
using SupportTicketing.Infrastructure.Repositories;
using SupportTicketing.Infrastructure.Services;

using SupportTicketing.Infrastructure;

// Configure Dapper to handle PostgreSQL enum types
DapperConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

// Tell ASP.NET to allow any host — required for Render's reverse proxy
builder.WebHost.UseSetting("AllowedHosts", "*");
builder.Services.Configure<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>(o =>
{
    o.AllowedHosts = new List<string> { "*" };
    o.AllowEmptyHosts = true;
});

// ── Serilog ───────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ── Database (Dapper / Npgsql) ─────────────────────────────────────────────
builder.Services.AddTransient<IDbConnection>(_ =>
{
    // Read from multiple possible sources in priority order
    var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                  ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                  ?? builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    // Convert postgres:// or postgresql:// URL to Npgsql format
    if (connStr != null && (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://")))
    {
        var uri = new Uri(connStr.Replace("postgres://", "http://").Replace("postgresql://", "http://"));
        var userInfo = uri.UserInfo.Split(':', 2);
        var host = uri.Host;
        var port = uri.Port > 0 && uri.Port != 80 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        connStr = $"Host={host};Port={port};Database={database};Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true";
    }

    return new NpgsqlConnection(connStr);
});

// ── RabbitMQ ──────────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

// ── Auth ──────────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── Repositories (scoped — one per HTTP request) ──────────────────────────
builder.Services.AddScoped<ITicketRepository,          TicketRepository>();
builder.Services.AddScoped<IUserRepository,            UserRepository>();
builder.Services.AddScoped<ICustomerRepository,        CustomerRepository>();
builder.Services.AddScoped<ISlaPolicyRepository,       SlaPolicyRepository>();
builder.Services.AddScoped<IAutomationRuleRepository,  AutomationRuleRepository>();
builder.Services.AddScoped<INotificationRepository,    NotificationRepository>();
builder.Services.AddScoped<ICommentRepository,         CommentRepository>();

// ── Core services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,            AuthService>();
builder.Services.AddScoped<ITicketService,          TicketService>();
builder.Services.AddScoped<ISlaService,             SlaService>();
builder.Services.AddScoped<IAutomationService,      AutomationService>();
builder.Services.AddScoped<INotificationService,    NotificationService>();
builder.Services.AddScoped<IReportingService,        ReportingService>();

// ── Background job: SLA breach evaluation (every minute) ─────────────────
builder.Services.AddHostedService<SlaEvaluationJob>();

// ── Controllers + Swagger ─────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow string enum values (e.g. "high" instead of 2)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SupportTicketing API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Enter: Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.SetIsOriginAllowed(_ => true)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Global exception handling
app.UseExceptionHandler(err => err.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    if (ex is not null)
    {
        var isDev = app.Environment.IsDevelopment();
        await ctx.Response.WriteAsJsonAsync(new
        {
            error   = isDev ? ex.Error.Message : "An unexpected error occurred.",
            details = isDev ? ex.Error.StackTrace : null
        });
    }
}));

app.MapControllers();

// Auto-migrate on startup — creates tables if they don't exist
await MigrationHelper.RunAsync(app);

app.Run();

public static class MigrationHelper
{
    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<System.Data.IDbConnection>();
        try
        {
            db.Open();
            app.Logger.LogInformation("Running auto-migration...");
            var sql = new[]
            {
                "CREATE EXTENSION IF NOT EXISTS pgcrypto",
                "CREATE EXTENSION IF NOT EXISTS citext",
                "DO $$ BEGIN CREATE TYPE ticket_status AS ENUM ('open','in_progress','pending','resolved','closed'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE ticket_priority AS ENUM ('low','medium','high','critical'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE ticket_channel AS ENUM ('email','portal','phone','chat','api'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE comment_type AS ENUM ('reply','internal_note'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE user_role AS ENUM ('admin','agent','viewer'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE notification_channel AS ENUM ('email','in_app','sms'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "DO $$ BEGIN CREATE TYPE automation_trigger AS ENUM ('ticket_created','ticket_updated','sla_breached','status_changed','idle_timeout'); EXCEPTION WHEN duplicate_object THEN null; END $$",
                "CREATE TABLE IF NOT EXISTS teams (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(100) NOT NULL, description TEXT, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ)",
                "CREATE TABLE IF NOT EXISTS users (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), email CITEXT NOT NULL UNIQUE, full_name VARCHAR(150) NOT NULL, avatar_url TEXT, role user_role NOT NULL DEFAULT 'agent', team_id UUID REFERENCES teams(id) ON DELETE SET NULL, is_active BOOLEAN NOT NULL DEFAULT TRUE, password_hash TEXT, last_login_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ)",
                "CREATE TABLE IF NOT EXISTS customers (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), email CITEXT NOT NULL UNIQUE, full_name VARCHAR(150), phone VARCHAR(30), company VARCHAR(150), external_id VARCHAR(100), metadata JSONB DEFAULT '{}', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ)",
                "CREATE TABLE IF NOT EXISTS categories (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(100) NOT NULL UNIQUE, description TEXT, parent_id UUID REFERENCES categories(id) ON DELETE SET NULL, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW())",
                "CREATE TABLE IF NOT EXISTS sla_policies (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(150) NOT NULL, description TEXT, priority ticket_priority NOT NULL, first_response_minutes INT NOT NULL, resolution_minutes INT NOT NULL, business_hours_only BOOLEAN NOT NULL DEFAULT TRUE, is_default BOOLEAN NOT NULL DEFAULT FALSE, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW())",
                "CREATE TABLE IF NOT EXISTS tickets (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), ticket_number BIGSERIAL NOT NULL UNIQUE, subject VARCHAR(500) NOT NULL, description TEXT, status ticket_status NOT NULL DEFAULT 'open', priority ticket_priority NOT NULL DEFAULT 'medium', channel ticket_channel NOT NULL DEFAULT 'portal', customer_id UUID NOT NULL REFERENCES customers(id), assignee_id UUID REFERENCES users(id) ON DELETE SET NULL, team_id UUID REFERENCES teams(id) ON DELETE SET NULL, category_id UUID REFERENCES categories(id) ON DELETE SET NULL, sla_policy_id UUID REFERENCES sla_policies(id) ON DELETE SET NULL, first_response_due_at TIMESTAMPTZ, resolution_due_at TIMESTAMPTZ, first_responded_at TIMESTAMPTZ, resolved_at TIMESTAMPTZ, closed_at TIMESTAMPTZ, sla_breached BOOLEAN NOT NULL DEFAULT FALSE, external_ref VARCHAR(100), metadata JSONB DEFAULT '{}', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ)",
                "CREATE TABLE IF NOT EXISTS tags (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(80) NOT NULL UNIQUE)",
                "CREATE TABLE IF NOT EXISTS ticket_tags (ticket_id UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE, tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE, PRIMARY KEY (ticket_id, tag_id))",
                "CREATE TABLE IF NOT EXISTS comments (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), ticket_id UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE, author_user_id UUID REFERENCES users(id) ON DELETE SET NULL, author_customer_id UUID REFERENCES customers(id) ON DELETE SET NULL, comment_type comment_type NOT NULL DEFAULT 'reply', body TEXT NOT NULL, is_edited BOOLEAN NOT NULL DEFAULT FALSE, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), deleted_at TIMESTAMPTZ)",
                "CREATE TABLE IF NOT EXISTS notifications (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), user_id UUID REFERENCES users(id) ON DELETE CASCADE, ticket_id UUID REFERENCES tickets(id) ON DELETE CASCADE, channel notification_channel NOT NULL DEFAULT 'in_app', subject VARCHAR(300), body TEXT NOT NULL, is_read BOOLEAN NOT NULL DEFAULT FALSE, sent_at TIMESTAMPTZ, read_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW())",
                "CREATE TABLE IF NOT EXISTS automation_rules (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), name VARCHAR(200) NOT NULL, description TEXT, is_active BOOLEAN NOT NULL DEFAULT TRUE, trigger_event automation_trigger NOT NULL, conditions JSONB NOT NULL DEFAULT '[]', actions JSONB NOT NULL DEFAULT '[]', execution_order INT NOT NULL DEFAULT 0, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW())",
                "CREATE TABLE IF NOT EXISTS csat_surveys (id UUID PRIMARY KEY DEFAULT gen_random_uuid(), ticket_id UUID NOT NULL UNIQUE REFERENCES tickets(id) ON DELETE CASCADE, customer_id UUID NOT NULL REFERENCES customers(id), score SMALLINT CHECK (score BETWEEN 1 AND 5), comment TEXT, sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), responded_at TIMESTAMPTZ)",
                "INSERT INTO categories (name,description) VALUES ('Billing','Payment queries'),('Technical','Bugs and issues'),('Account','Login and access'),('General','General enquiries') ON CONFLICT (name) DO NOTHING",
                "INSERT INTO sla_policies (name,priority,first_response_minutes,resolution_minutes,is_default) VALUES ('Critical SLA','critical',60,240,TRUE),('High SLA','high',240,1440,TRUE),('Medium SLA','medium',480,4320,TRUE),('Low SLA','low',1440,10080,TRUE) ON CONFLICT DO NOTHING"
            };
            foreach (var s in sql) await db.ExecuteAsync(s);
            app.Logger.LogInformation("Auto-migration complete.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError("Auto-migration failed: {Error}", ex.Message);
        }
        finally { db.Close(); }
    }
}

// ── SLA background job ────────────────────────────────────────────────────
public class SlaEvaluationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SlaEvaluationJob> _logger;

    public SlaEvaluationJob(IServiceProvider services, ILogger<SlaEvaluationJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
            try
            {
                using var scope = _services.CreateScope();
                var sla = scope.ServiceProvider.GetRequiredService<ISlaService>();
                await sla.EvaluateBreachesAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "SLA evaluation job failed");
            }
        }
    }
}
