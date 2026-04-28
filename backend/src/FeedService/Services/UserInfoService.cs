using System.Net.Http.Json;
using System.Net.Http.Headers;
using FeedService.DTOs;
using Shared.Common;

namespace FeedService.Services;

public class UserInfoService : IUserInfoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInfoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserDto?> GetUserInfoAsync(int userId, int companyId, string? jwtToken = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var userServiceUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
            
            // Получаем токен из HttpContext, если он не передан
            if (string.IsNullOrEmpty(jwtToken))
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    jwtToken = AuthTokenHelper.ExtractToken(httpContext);
                }
            }
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/users/{userId}?companyId={companyId}");
            if (!string.IsNullOrEmpty(jwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            }
            
            var response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                return user;
            }
        }
        catch (Exception)
        {
            // Логируем ошибку, но не падаем
        }
        
        return null;
    }

    public async Task<Dictionary<int, UserDto>> GetUsersInfoAsync(List<int> userIds, int companyId, string? jwtToken = null)
    {
        var result = new Dictionary<int, UserDto>();
        
        // Получаем информацию о пользователях параллельно
        var tasks = userIds.Select(async userId =>
        {
            var user = await GetUserInfoAsync(userId, companyId, jwtToken);
            return user != null ? (userId, user) : (userId, (UserDto?)null);
        });

        var results = await Task.WhenAll(tasks);
        
        foreach (var (userId, user) in results)
        {
            if (user != null)
            {
                result[userId] = user;
            }
        }

        return result;
    }
}
