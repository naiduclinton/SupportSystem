using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace SupportTicketing.Api.Controllers;

/// <summary>
/// One-time setup endpoint to seed the initial admin user.
/// Protected by a setup key. Disable or remove after first use.
/// </summary>
[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _config;

    public SetupController(IDbConnection db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromBody] SeedAdminRequest request)
    {
        // Validate setup key to prevent abuse
        var expectedKey = _config["SetupKey"];
        if (string.IsNullOrEmpty(expectedKey) || request.SetupKey != expectedKey)
            return Unauthorized(new { error = "Invalid setup key." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

        var sql = @"
            INSERT INTO users (id, email, full_name, role, is_active, password_hash, created_at, updated_at)
            VALUES (gen_random_uuid(), @Email, @FullName, 'admin', true, @Hash, NOW(), NOW())
            ON CONFLICT (email) DO UPDATE
                SET password_hash = EXCLUDED.password_hash,
                    full_name     = EXCLUDED.full_name,
                    role          = EXCLUDED.role,
                    is_active     = EXCLUDED.is_active,
                    updated_at    = NOW()
            RETURNING id, email, full_name, role;";

        var result = await _db.QueryFirstOrDefaultAsync(sql, new
        {
            Email    = request.Email,
            FullName = request.FullName,
            Hash     = hash
        });

        return Ok(new
        {
            message  = "Admin user created successfully.",
            id       = result?.id,
            email    = result?.email,
            fullName = result?.full_name,
            role     = result?.role
        });
    }
}

public record SeedAdminRequest(string SetupKey, string Email, string FullName, string Password);
