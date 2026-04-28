using Microsoft.EntityFrameworkCore;
using Shared.Models;
using FeedService.Data;

namespace FeedService.Services;

public class CommentService : ICommentService
{
    private readonly FeedDbContext _context;

    public CommentService(FeedDbContext context)
    {
        _context = context;
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<List<Comment>> GetByPostIdAsync(int postId, int companyId)
    {
        return await _context.Comments
            .Where(c => c.PostId == postId && c.CompanyId == companyId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id, int companyId)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

        if (comment == null) return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }
}






