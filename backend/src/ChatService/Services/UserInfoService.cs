using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Linq;
using Shared.Common;
using Shared.Models;

namespace ChatService.Services;

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

    public async Task<User?> GetUserInfoAsync(int userId, int companyId, string? jwtToken = null)
    {
        try
        {
            var user = await TryGetInternalUserInfoAsync(userId, companyId);
            if (user != null) return user;

            return await TryGetPublicUserInfoAsync(userId, companyId, jwtToken);
        }
        catch (Exception)
        {
            // Логируем ошибку, но не падаем
        }
        
        return null;
    }

    private async Task<User?> TryGetInternalUserInfoAsync(int userId, int companyId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var userServiceUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
            
            var url = $"{userServiceUrl}/api/internal/users/{userId}";
            if (companyId > 0) url += $"?companyId={companyId}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }
        }
        catch { }
        return null;
    }

    private async Task<User?> TryGetPublicUserInfoAsync(int userId, int companyId, string? jwtToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var userServiceUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";

            if (string.IsNullOrEmpty(jwtToken))
            {
                jwtToken = AuthTokenHelper.ExtractToken(_httpContextAccessor.HttpContext);
            }

            if (string.IsNullOrEmpty(jwtToken)) return null;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/users/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }
        }
        catch { }
        return null;
    }

    public async Task<Dictionary<int, User>> GetUsersInfoAsync(List<int> userIds, int companyId, string? jwtToken = null)
    {
        var result = new Dictionary<int, User>();
        
        // Получаем информацию о пользователях параллельно
        var tasks = userIds.Select(async userId =>
        {
            var user = await GetUserInfoAsync(userId, companyId, jwtToken);
            return user != null ? (userId, user) : (userId, (User?)null);
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
