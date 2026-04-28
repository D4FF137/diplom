using Shared.Models;

namespace FeedService.Services;

public interface ICommentService
{
    Task<Comment> CreateAsync(Comment comment);
    Task<List<Comment>> GetByPostIdAsync(int postId, int companyId);
    Task<bool> DeleteAsync(int id, int companyId);
}






