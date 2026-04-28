using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FeedService.Services;
using FeedService.DTOs;
using FeedService.Hubs;
using Shared.Models;
using Shared.Common;
using System.Linq;

namespace FeedService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILikeService _likeService;
    private readonly ICommentService _commentService;
    private readonly IUserInfoService _userInfoService;
    private readonly JwtHelper _jwtHelper;
    private readonly IHubContext<FeedHub> _hubContext;
    private readonly IFileService _fileService;

    public PostsController(
        IPostService postService,
        ILikeService likeService,
        ICommentService commentService,
        IUserInfoService userInfoService,
        JwtHelper jwtHelper,
        IHubContext<FeedHub> hubContext,
        IFileService fileService)
    {
        _postService = postService;
        _likeService = likeService;
        _commentService = commentService;
        _userInfoService = userInfoService;
        _jwtHelper = jwtHelper;
        _hubContext = hubContext;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PostDto>>> GetPosts([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var posts = await _postService.GetByCompanyIdAsync(companyId.Value, skip, take);
        
        // Получаем JWT токен из заголовков
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        
        // Получаем информацию о пользователях
        var userIds = posts.Select(p => p.UserId).Distinct().ToList();
        var usersInfo = await _userInfoService.GetUsersInfoAsync(userIds, companyId.Value, jwtToken);
        
        // Получаем информацию о лайках и комментариях
        var postDtos = new List<PostDto>();
        foreach (var post in posts)
        {
            var likesCount = await _likeService.GetLikesCountAsync(post.Id, companyId.Value);
            var comments = await _commentService.GetByPostIdAsync(post.Id, companyId.Value);
            var commentsCount = comments.Count;
            var isLiked = userId > 0 && await _likeService.IsLikedAsync(post.Id, userId, companyId.Value);
            
            usersInfo.TryGetValue(post.UserId, out var author);
            
            postDtos.Add(new PostDto
            {
                Id = post.Id,
                CompanyId = post.CompanyId,
                UserId = post.UserId,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                Author = author,
                LikesCount = likesCount,
                CommentsCount = commentsCount,
                IsLiked = isLiked
            });
        }
        
        return Ok(postDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var post = await _postService.GetByIdAsync(id, companyId.Value);
        if (post == null)
        {
            return NotFound();
        }

        return Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost([FromForm] CreatePostRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        // Обработка загрузки изображения
        string? imageUrl = null;
        if (request.Image != null && request.Image.Length > 0)
        {
            try
            {
                // Сначала создаем пост, чтобы получить его ID
                var newPost = new Post
                {
                    CompanyId = companyId.Value,
                    UserId = userId,
                    Content = request.Content ?? string.Empty,
                    ImageUrl = null
                };

                var postWithImage = await _postService.CreateAsync(newPost);
                
                // Сохраняем изображение с ID поста
                var fileName = await _fileService.SavePostImageAsync(request.Image, postWithImage.Id);
                imageUrl = _fileService.GetImageUrl(fileName);
                
                // Обновляем пост с URL изображения
                postWithImage.ImageUrl = imageUrl;
                await _postService.UpdateAsync(postWithImage.Id, companyId.Value, postWithImage);
                
                // Используем обновленный пост
                var imageUserIds = new List<int> { postWithImage.UserId };
                var imageJwtToken = AuthTokenHelper.ExtractToken(Request);
                var imageUsersInfo = await _userInfoService.GetUsersInfoAsync(imageUserIds, companyId.Value, imageJwtToken);
                imageUsersInfo.TryGetValue(postWithImage.UserId, out var imageAuthor);
                
                var imagePostDto = new PostDto
                {
                    Id = postWithImage.Id,
                    CompanyId = postWithImage.CompanyId,
                    UserId = postWithImage.UserId,
                    Content = postWithImage.Content,
                    ImageUrl = postWithImage.ImageUrl,
                    CreatedAt = postWithImage.CreatedAt,
                    Author = imageAuthor,
                    LikesCount = 0,
                    CommentsCount = 0,
                    IsLiked = false
                };
                
                // Отправляем реалтайм обновление
                await _hubContext.Clients.Group($"company_{companyId.Value}_feed").SendAsync("NewPost", imagePostDto);
                
                return CreatedAtAction(nameof(GetPost), new { id = postWithImage.Id }, imagePostDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        var post = new Post
        {
            CompanyId = companyId.Value,
            UserId = userId,
            Content = request.Content ?? string.Empty,
            ImageUrl = imageUrl
        };

        var createdPost = await _postService.CreateAsync(post);
        
        // Отправляем реалтайм обновление через SignalR
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        var userIds = new List<int> { createdPost.UserId };
        var usersInfo = await _userInfoService.GetUsersInfoAsync(userIds, companyId.Value, jwtToken);
        usersInfo.TryGetValue(createdPost.UserId, out var author);
        
        var postDto = new PostDto
        {
            Id = createdPost.Id,
            CompanyId = createdPost.CompanyId,
            UserId = createdPost.UserId,
            Content = createdPost.Content,
            ImageUrl = createdPost.ImageUrl,
            CreatedAt = createdPost.CreatedAt,
            Author = author,
            LikesCount = 0,
            CommentsCount = 0,
            IsLiked = false
        };
        
        // Отправляем новое сообщение всем пользователям компании
        await _hubContext.Clients.Group($"company_{companyId.Value}_feed").SendAsync("NewPost", postDto);
        
        return CreatedAtAction(nameof(GetPost), new { id = createdPost.Id }, postDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Post>> UpdatePost(int id, [FromBody] UpdatePostRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var post = new Post
        {
            Content = request.Content
        };

        var updatedPost = await _postService.UpdateAsync(id, companyId.Value, post);
        if (updatedPost == null)
        {
            return NotFound();
        }

        return Ok(updatedPost);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        // Проверяем, что пост существует и принадлежит текущему пользователю
        var post = await _postService.GetByIdAsync(id, companyId.Value);
        if (post == null)
        {
            return NotFound();
        }

        if (post.UserId != userId)
        {
            return Forbid(); // Только автор может удалить свой пост
        }

        var deleted = await _postService.DeleteAsync(id, companyId.Value);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id}/like")]
    public async Task<ActionResult<Like>> LikePost(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        // Проверяем, что пост существует и принадлежит компании
        var post = await _postService.GetByIdAsync(id, companyId.Value);
        if (post == null)
        {
            return NotFound();
        }

        // Проверяем, не лайкнул ли уже пользователь
        var isLiked = await _likeService.IsLikedAsync(id, userId, companyId.Value);
        if (isLiked)
        {
            return BadRequest("Post already liked");
        }

        var like = await _likeService.CreateAsync(id, userId, companyId.Value);
        
        // Отправляем реалтайм обновление
        var likesCount = await _likeService.GetLikesCountAsync(id, companyId.Value);
        await _hubContext.Clients.Group($"company_{companyId.Value}_feed").SendAsync("PostLiked", new { PostId = id, LikesCount = likesCount });
        
        return Ok(like);
    }

    [HttpDelete("{id}/like")]
    public async Task<IActionResult> UnlikePost(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

            var deleted = await _likeService.DeleteAsync(id, userId, companyId.Value);
            if (!deleted)
            {
                return NotFound();
            }

            // Отправляем реалтайм обновление
            var likesCount = await _likeService.GetLikesCountAsync(id, companyId.Value);
            await _hubContext.Clients.Group($"company_{companyId.Value}_feed").SendAsync("PostUnliked", new { PostId = id, LikesCount = likesCount });

            return NoContent();
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        // Проверяем, что пост существует
        var post = await _postService.GetByIdAsync(id, companyId.Value);
        if (post == null)
        {
            return NotFound();
        }

        var comments = await _commentService.GetByPostIdAsync(id, companyId.Value);
        
        // Получаем JWT токен из заголовков
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        
        // Получаем информацию о пользователях
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var usersInfo = await _userInfoService.GetUsersInfoAsync(userIds, companyId.Value, jwtToken);
        
        var commentDtos = comments.Select(c =>
        {
            usersInfo.TryGetValue(c.UserId, out var author);
            return new CommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                UserId = c.UserId,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                Author = author
            };
        }).ToList();
        
        return Ok(commentDtos);
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> CreateComment(int id, [FromBody] CreateCommentRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        // Проверяем, что пост существует
        var post = await _postService.GetByIdAsync(id, companyId.Value);
        if (post == null)
        {
            return NotFound();
        }

        var comment = new Comment
        {
            PostId = id,
            UserId = userId,
            CompanyId = companyId.Value,
            Content = request.Content
        };

        var createdComment = await _commentService.CreateAsync(comment);
        
        // Получаем JWT токен из заголовков
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        
        // Получаем информацию об авторе
        var author = await _userInfoService.GetUserInfoAsync(userId, companyId.Value, jwtToken);
        
        var commentDto = new CommentDto
        {
            Id = createdComment.Id,
            PostId = createdComment.PostId,
            UserId = createdComment.UserId,
            Content = createdComment.Content,
            CreatedAt = createdComment.CreatedAt,
            Author = author
        };
        
        // Отправляем реалтайм обновление
        var commentsCount = (await _commentService.GetByPostIdAsync(id, companyId.Value)).Count;
        await _hubContext.Clients.Group($"company_{companyId.Value}_feed").SendAsync("NewComment", new { PostId = id, Comment = commentDto, CommentsCount = commentsCount });
        
        return CreatedAtAction(nameof(GetComments), new { id }, commentDto);
    }
}

public class CreatePostRequest
{
    public string? Content { get; set; }
    public IFormFile? Image { get; set; }
}

public class UpdatePostRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

