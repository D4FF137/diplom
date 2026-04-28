using Microsoft.EntityFrameworkCore;
using Shared.Models;
using FeedService.Data;

namespace FeedService.Services;

public class PostService : IPostService
{
    private readonly FeedDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;

    public PostService(FeedDbContext context, IRabbitMQService rabbitMQService)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
    }

    public async Task<Post?> GetByIdAsync(int id, int companyId)
    {
        return await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
    }

    public async Task<List<Post>> GetByCompanyIdAsync(int companyId, int skip = 0, int take = 20)
    {
        return await _context.Posts
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Post> CreateAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        await _rabbitMQService.PublishPostCreatedAsync(
            post.Id, post.CompanyId, post.UserId, post.Content);

        return post;
    }

    public async Task<Post?> UpdateAsync(int id, int companyId, Post post)
    {
        var existingPost = await GetByIdAsync(id, companyId);
        if (existingPost == null) return null;

        existingPost.Content = post.Content;
        await _context.SaveChangesAsync();
        return existingPost;
    }

    public async Task<bool> DeleteAsync(int id, int companyId)
    {
        var post = await GetByIdAsync(id, companyId);
        if (post == null) return false;

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }
}


