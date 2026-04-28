using Microsoft.EntityFrameworkCore;
using Shared.Models;
using FeedService.Data;

namespace FeedService.Services;

public class LikeService : ILikeService
{
    private readonly FeedDbContext _context;

    public LikeService(FeedDbContext context)
    {
        _context = context;
    }

    public async Task<Like> CreateAsync(int postId, int userId, int companyId)
    {
        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CompanyId = companyId
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        return like;
    }

    public async Task<bool> DeleteAsync(int postId, int userId, int companyId)
    {
        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId && l.CompanyId == companyId);

        if (like == null) return false;

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsLikedAsync(int postId, int userId, int companyId)
    {
        return await _context.Likes
            .AnyAsync(l => l.PostId == postId && l.UserId == userId && l.CompanyId == companyId);
    }

    public async Task<int> GetLikesCountAsync(int postId, int companyId)
    {
        return await _context.Likes
            .CountAsync(l => l.PostId == postId && l.CompanyId == companyId);
    }
}






