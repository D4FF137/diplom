using Shared.Models;

namespace FeedService.Services;

public interface ILikeService
{
    Task<Like> CreateAsync(int postId, int userId, int companyId);
    Task<bool> DeleteAsync(int postId, int userId, int companyId);
    Task<bool> IsLikedAsync(int postId, int userId, int companyId);
    Task<int> GetLikesCountAsync(int postId, int companyId);
}






