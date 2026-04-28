using Shared.Models;

namespace FeedService.Services;

public interface IPostService
{
    Task<Post?> GetByIdAsync(int id, int companyId);
    Task<List<Post>> GetByCompanyIdAsync(int companyId, int skip = 0, int take = 20);
    Task<Post> CreateAsync(Post post);
    Task<Post?> UpdateAsync(int id, int companyId, Post post);
    Task<bool> DeleteAsync(int id, int companyId);
}


