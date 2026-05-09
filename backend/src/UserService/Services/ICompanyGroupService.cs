using Shared.Models;

namespace UserService.Services;

public interface ICompanyGroupService
{
    Task<List<CompanyGroupDetails>> GetByCompanyIdAsync(int companyId);
    Task<CompanyGroupDetails?> GetByIdAsync(int groupId, int companyId);
    Task<CompanyGroupDetails> CreateAsync(int companyId, int creatorId, string name, int leaderUserId, List<int> memberIds);
    Task<bool> AddMemberAsync(int companyId, int groupId, int userId);
    Task AddMemberToGroupsAsync(int companyId, int userId, List<int> groupIds);
    Task AddBossToDepartmentChatsAsync(int companyId, int bossUserId);
    Task<bool> RemoveMemberAsync(int companyId, int groupId, int userId);
    Task<bool> IsGroupLeaderAsync(int companyId, int groupId, int userId);
}

public class CompanyGroupDetails
{
    public CompanyGroup Group { get; set; } = new();
    public List<int> MemberIds { get; set; } = new();
}
