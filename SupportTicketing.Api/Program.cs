using System.Data;
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

// Pass AllowedHosts=* via args to override Render's host filtering before middleware runs
var overrideArgs = args.Concat(new[] { "--AllowedHosts=*" }).ToArray();
var builder = WebApplication.CreateBuilder(overrideArgs);

// Disable host filtering — allow requests from any host (required for Render deployment)
builder.WebHost.ConfigureKestrel(options => { });
builder.Services.Configure<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>(options =>
{
    options.AllowedHosts = new List<string> { "*" };
    options.AllowEmptyHosts = true;
    options.IncludeFailureMessage = false;
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
builder.Services.AddScoped<ITicketRepository,       TicketRepository>();
builder.Services.AddScoped<IUserRepository,         UserRepository>();
builder.Services.AddScoped<ICustomerRepository,     CustomerRepository>();

// ── Core services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,            AuthService>();
builder.Services.AddScoped<ITicketService,          TicketService>();
builder.Services.AddScoped<ISlaService,             SlaService>();
builder.Services.AddScoped<IAutomationService,      AutomationService>();
builder.Services.AddScoped<INotificationService,    NotificationService>();

// ── Background job: SLA breach evaluation (every minute) ─────────────────
builder.Services.AddHostedService<SlaEvaluationJob>();

// ── Controllers + Swagger ─────────────────────────────────────────────────
builder.Services.AddControllers();
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
// Remove host filtering middleware — Render's reverse proxy causes Host header mismatches
var hostFilteringFeature = app.Services.GetService<Microsoft.AspNetCore.HostFiltering.IHostFilteringFeature>();
app.Use(async (context, next) =>
{
    context.Features.Set<Microsoft.AspNetCore.HostFiltering.IHostFilteringFeature>(null);
    await next();
});

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
app.Run();

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
