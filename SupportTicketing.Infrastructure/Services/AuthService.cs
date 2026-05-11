using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Infrastructure.Services;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly JwtSettings _jwt;

    // In production: store refresh tokens in DB/Redis
    private static readonly Dictionary<string, (string UserId, DateTime Expiry)> RefreshTokens = new();

    public AuthService(IUserRepository users, IOptions<JwtSettings> jwt)
    {
        _users = users;
        _jwt = jwt.Value;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return GenerateTokens(user.Id.ToString(), user.FullName, user.Role.ToString());
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (!RefreshTokens.TryGetValue(refreshToken, out var entry))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (entry.Expiry < DateTime.UtcNow)
        {
            RefreshTokens.Remove(refreshToken);
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        RefreshTokens.Remove(refreshToken);
        return Task.FromResult(GenerateTokens(entry.UserId, string.Empty, string.Empty));
    }

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    private AuthResult GenerateTokens(string userId, string fullName, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate refresh token
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        RefreshTokens[refreshToken] = (userId, DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays));

        return new AuthResult(accessToken, refreshToken, expiry, userId, fullName, role);
    }
}
