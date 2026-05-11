using System.Data;
using Dapper;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _db;
    public UserRepository(IDbConnection db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM users WHERE id = @Id AND deleted_at IS NULL";
        return await _db.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM users WHERE email = @Email AND deleted_at IS NULL";
        return await _db.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM users WHERE deleted_at IS NULL ORDER BY full_name";
        return await _db.QueryAsync<User>(sql);
    }

    public async Task<User> AddAsync(User entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO users (id, email, full_name, avatar_url, role, team_id, is_active, password_hash, created_at, updated_at)
            VALUES (@Id, @Email, @FullName, @AvatarUrl, @Role::user_role, @TeamId, @IsActive, @PasswordHash, @CreatedAt, @UpdatedAt)";
        await _db.ExecuteAsync(sql, new
        {
            entity.Id, entity.Email, entity.FullName, entity.AvatarUrl,
            Role = entity.Role.ToString().ToLower(),
            entity.TeamId, entity.IsActive, entity.PasswordHash,
            entity.CreatedAt, entity.UpdatedAt
        });
        return entity;
    }

    public async Task UpdateAsync(User entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE users SET
                full_name = @FullName, avatar_url = @AvatarUrl, role = @Role::user_role,
                team_id = @TeamId, is_active = @IsActive, last_login_at = @LastLoginAt, updated_at = @UpdatedAt
            WHERE id = @Id";
        await _db.ExecuteAsync(sql, new
        {
            entity.Id, entity.FullName, entity.AvatarUrl,
            Role = entity.Role.ToString().ToLower(),
            entity.TeamId, entity.IsActive, entity.LastLoginAt, entity.UpdatedAt
        });
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _db.ExecuteAsync("UPDATE users SET deleted_at = NOW(), updated_at = NOW() WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<AgentWorkload>> GetAgentWorkloadsAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT * FROM v_agent_workload ORDER BY full_name";
        return await _db.QueryAsync<AgentWorkload>(sql);
    }
}

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnection _db;
    public CustomerRepository(IDbConnection db) => _db = db;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customers WHERE id = @Id AND deleted_at IS NULL";
        return await _db.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customers WHERE email = @Email AND deleted_at IS NULL";
        return await _db.QueryFirstOrDefaultAsync<Customer>(sql, new { Email = email });
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM customers WHERE deleted_at IS NULL ORDER BY full_name";
        return await _db.QueryAsync<Customer>(sql);
    }

    public async Task<Customer> GetOrCreateAsync(string email, string? fullName, CancellationToken ct = default)
    {
        var existing = await GetByEmailAsync(email, ct);
        if (existing is not null) return existing;

        var customer = new Customer { Email = email, FullName = fullName };
        return await AddAsync(customer, ct);
    }

    public async Task<Customer> AddAsync(Customer entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO customers (id, email, full_name, phone, company, external_id, created_at, updated_at)
            VALUES (@Id, @Email, @FullName, @Phone, @Company, @ExternalId, @CreatedAt, @UpdatedAt)";
        await _db.ExecuteAsync(sql, entity);
        return entity;
    }

    public async Task UpdateAsync(Customer entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE customers SET
                full_name = @FullName, phone = @Phone, company = @Company,
                external_id = @ExternalId, updated_at = @UpdatedAt
            WHERE id = @Id";
        await _db.ExecuteAsync(sql, entity);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _db.ExecuteAsync("UPDATE customers SET deleted_at = NOW(), updated_at = NOW() WHERE id = @Id", new { Id = id });
    }
}
