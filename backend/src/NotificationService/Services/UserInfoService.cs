using System.Text.Json;

namespace NotificationService.Services;

public class UserInfoService : IUserInfoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserInfoService> _logger;

    public UserInfoService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UserInfoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<int>> GetChatMembersAsync(int chatId, int companyId, string jwtToken)
    {
        try
        {
            var chatServiceUrl = _configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.GetAsync($"{chatServiceUrl}/api/chats/{chatId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var chat = JsonSerializer.Deserialize<ChatResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chat?.Members != null)
                {
                    return chat.Members.Select(m => m.Id).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat members for chat {ChatId}", chatId);
        }

        return new List<int>();
    }

    public async Task<List<int>> GetCompanyUsersAsync(int companyId, string jwtToken)
    {
        try
        {
            var userServiceUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.GetAsync($"{userServiceUrl}/api/users?companyId={companyId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (users != null)
                {
                    return users.Select(u => u.Id).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company users for company {CompanyId}", companyId);
        }

        return new List<int>();
    }

    private class ChatResponse
    {
        public int Id { get; set; }
        public List<MemberResponse>? Members { get; set; }
    }

    private class MemberResponse
    {
        public int Id { get; set; }
    }

    private class UserResponse
    {
        public int Id { get; set; }
    }
}




