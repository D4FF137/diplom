using Microsoft.EntityFrameworkCore;
using Shared.Models;
using UserService.Data;
using BCrypt.Net;
using StackExchange.Redis;
using System.Text.Json;

namespace UserService.Services;

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IDatabase _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public UserService(UserDbContext context, IRabbitMQService rabbitMQService, IConnectionMultiplexer redis)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _cache = redis.GetDatabase();
    }

    public async Task<User?> GetByIdAsync(int id, int companyId)
    {
        var cacheKey = $"user_{companyId}_{id}";
        var cachedUser = await _cache.StringGetAsync(cacheKey);
        
        if (!cachedUser.IsNull)
        {
            return JsonSerializer.Deserialize<User>(cachedUser!);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && (companyId == 0 || u.CompanyId == companyId));

        if (user != null)
        {
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(user), _cacheDuration);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Publish event
        await _rabbitMQService.PublishUserCreatedAsync(user.Id, user.CompanyId, user.Email);

        return user;
    }

    public async Task<User?> UpdateAsync(int id, int companyId, User user)
    {
        // Не используем GetByIdAsync, так как он может вернуть объект из кэша (без трекинга EF Core)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && (companyId == 0 || u.CompanyId == companyId));
            
        if (existingUser == null) return null;

        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        if (user.AvatarUrl != null)
        {
            existingUser.AvatarUrl = user.AvatarUrl;
        }
        if (!string.IsNullOrEmpty(user.Email))
        {
            existingUser.Email = user.Email;
        }

        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{id}");
        return existingUser;
    }

    public async Task<bool> DeleteAsync(int id, int companyId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && (companyId == 0 || u.CompanyId == companyId));
            
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{id}");
        return true;
    }

    public async Task<List<User>> GetByCompanyIdAsync(int companyId)
    {
        return await _context.Users
            .Where(u => u.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<bool> ValidatePasswordAsync(string password, string hash)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hash));
    }

    public async Task<List<User>> SearchAsync(int companyId, string query)
    {
        var lowerQuery = query.ToLower();
        return await _context.Users
            .Where(u => u.CompanyId == companyId && 
                (u.Email.ToLower().Contains(lowerQuery) ||
                 u.FirstName.ToLower().Contains(lowerQuery) ||
                 u.LastName.ToLower().Contains(lowerQuery)))
            .Take(20)
            .ToListAsync();
    }

    public async Task<bool> ChangePasswordAsync(int userId, int companyId, string oldPassword, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && (companyId == 0 || u.CompanyId == companyId));
            
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{userId}");
        return true;
    }

    public async Task<bool> BlockAsync(int userId, int companyId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && (companyId == 0 || u.CompanyId == companyId));
            
        if (user == null) return false;
        user.IsBlocked = true;
        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{userId}");
        return true;
    }

    public async Task<bool> UnblockAsync(int userId, int companyId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && (companyId == 0 || u.CompanyId == companyId));
            
        if (user == null) return false;
        user.IsBlocked = false;
        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{userId}");
        return true;
    }

    public async Task<bool> SetPasswordByBossAsync(int userId, int companyId, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && (companyId == 0 || u.CompanyId == companyId));
            
        if (user == null) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        await _cache.KeyDeleteAsync($"user_{companyId}_{userId}");
        return true;
    }

    public async Task UpdateLastSeenAsync(int userId, int companyId, DateTime lastSeen)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId);
        if (user != null)
        {
            user.LastSeen = lastSeen;
            await _context.SaveChangesAsync();
            await _cache.KeyDeleteAsync($"user_{companyId}_{userId}");
        }
    }
}


