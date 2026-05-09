using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Net.Http.Json;
using UserService.Data;

namespace UserService.Services;

public class CompanyGroupService : ICompanyGroupService
{
    private readonly UserDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CompanyGroupService(UserDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<List<CompanyGroupDetails>> GetByCompanyIdAsync(int companyId)
    {
        var groups = await _context.CompanyGroups
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.Name)
            .ToListAsync();

        var groupIds = groups.Select(g => g.Id).ToList();
        var members = await _context.CompanyGroupMembers
            .Where(m => groupIds.Contains(m.CompanyGroupId))
            .ToListAsync();

        return groups.Select(g => new CompanyGroupDetails
        {
            Group = g,
            MemberIds = members
                .Where(m => m.CompanyGroupId == g.Id)
                .Select(m => m.UserId)
                .ToList()
        }).ToList();
    }

    public async Task<CompanyGroupDetails?> GetByIdAsync(int groupId, int companyId)
    {
        var group = await _context.CompanyGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.CompanyId == companyId);

        if (group == null) return null;

        var memberIds = await _context.CompanyGroupMembers
            .Where(m => m.CompanyGroupId == group.Id)
            .Select(m => m.UserId)
            .ToListAsync();

        return new CompanyGroupDetails { Group = group, MemberIds = memberIds };
    }

    public async Task<CompanyGroupDetails> CreateAsync(int companyId, int creatorId, string name, int leaderUserId, List<int> memberIds)
    {
        var leader = await GetUserInCompanyAsync(companyId, leaderUserId)
            ?? throw new InvalidOperationException("Leader not found in company");

        var normalizedName = name.Trim();
        var exists = await _context.CompanyGroups.AnyAsync(g => g.CompanyId == companyId && g.Name == normalizedName);
        if (exists)
            throw new InvalidOperationException("Group with this name already exists");

        var group = new CompanyGroup
        {
            CompanyId = companyId,
            Name = normalizedName,
            LeaderUserId = leader.Id,
            CreatedByUserId = creatorId,
            ChatId = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.CompanyGroups.Add(group);
        await _context.SaveChangesAsync();

        var realMemberIds = new HashSet<int>(memberIds);
        realMemberIds.Add(leader.Id);
        var existingMemberIds = await _context.Users
            .Where(u => u.CompanyId == companyId && realMemberIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in existingMemberIds.Distinct())
        {
            _context.CompanyGroupMembers.Add(new CompanyGroupMember
            {
                CompanyGroupId = group.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        try
        {
            var chatUserIds = await BuildDepartmentChatMemberIdsAsync(companyId, existingMemberIds);
            group.ChatId = await CreateDepartmentChatAsync(companyId, group.Id, group.Name, creatorId, chatUserIds);
            await _context.SaveChangesAsync();
        }
        catch
        {
            _context.CompanyGroupMembers.RemoveRange(_context.CompanyGroupMembers.Where(m => m.CompanyGroupId == group.Id));
            _context.CompanyGroups.Remove(group);
            await _context.SaveChangesAsync();
            throw;
        }

        return new CompanyGroupDetails { Group = group, MemberIds = existingMemberIds.Distinct().ToList() };
    }

    public async Task AddMemberToGroupsAsync(int companyId, int userId, List<int> groupIds)
    {
        foreach (var groupId in groupIds.Distinct())
        {
            await AddMemberAsync(companyId, groupId, userId);
        }
    }

    public async Task AddBossToDepartmentChatsAsync(int companyId, int bossUserId)
    {
        var groups = await _context.CompanyGroups
            .Where(g => g.CompanyId == companyId && g.ChatId != string.Empty)
            .ToListAsync();

        foreach (var group in groups)
        {
            await AddChatMemberAsync(group.ChatId, companyId, bossUserId);
        }
    }

    public async Task<bool> AddMemberAsync(int companyId, int groupId, int userId)
    {
        var group = await _context.CompanyGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.CompanyId == companyId);
        if (group == null) return false;

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.CompanyId == companyId);
        if (!userExists) return false;

        var exists = await _context.CompanyGroupMembers
            .AnyAsync(m => m.CompanyGroupId == groupId && m.UserId == userId);

        if (!exists)
        {
            _context.CompanyGroupMembers.Add(new CompanyGroupMember
            {
                CompanyGroupId = groupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(group.ChatId))
        {
            await AddChatMemberAsync(group.ChatId, companyId, userId);
        }

        return true;
    }

    public async Task<bool> RemoveMemberAsync(int companyId, int groupId, int userId)
    {
        var group = await _context.CompanyGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.CompanyId == companyId);
        if (group == null || group.LeaderUserId == userId) return false;

        var user = await GetUserInCompanyAsync(companyId, userId);
        if (user == null || user.Role == "Boss") return false;

        var member = await _context.CompanyGroupMembers
            .FirstOrDefaultAsync(m => m.CompanyGroupId == groupId && m.UserId == userId);
        if (member == null) return false;

        _context.CompanyGroupMembers.Remove(member);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(group.ChatId))
        {
            await RemoveChatMemberAsync(group.ChatId, companyId, userId);
        }

        return true;
    }

    public async Task<bool> IsGroupLeaderAsync(int companyId, int groupId, int userId)
    {
        return await _context.CompanyGroups
            .AnyAsync(g => g.Id == groupId && g.CompanyId == companyId && g.LeaderUserId == userId);
    }

    private async Task<User?> GetUserInCompanyAsync(int companyId, int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId);
    }

    private async Task<List<int>> BuildDepartmentChatMemberIdsAsync(int companyId, IEnumerable<int> groupMemberIds)
    {
        var bossIds = await _context.Users
            .Where(u => u.CompanyId == companyId && u.Role == "Boss")
            .Select(u => u.Id)
            .ToListAsync();

        return bossIds.Concat(groupMemberIds).Distinct().ToList();
    }

    private async Task<string> CreateDepartmentChatAsync(int companyId, int groupId, string name, int creatorId, List<int> userIds)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
        var response = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/internal/department-chats", new
        {
            companyId,
            companyGroupId = groupId,
            name,
            creatorId,
            userIds
        });

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<DepartmentChatResponse>();
        return created?.Id ?? throw new InvalidOperationException("Chat service returned empty chat id");
    }

    private async Task AddChatMemberAsync(string chatId, int companyId, int userId)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
        var response = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/internal/chats/{chatId}/members", new
        {
            companyId,
            userId
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task RemoveChatMemberAsync(string chatId, int companyId, int userId)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
        var response = await client.DeleteAsync($"{baseUrl.TrimEnd('/')}/api/internal/chats/{chatId}/members/{userId}?companyId={companyId}");
        response.EnsureSuccessStatusCode();
    }

    private class DepartmentChatResponse
    {
        public string Id { get; set; } = string.Empty;
    }
}
