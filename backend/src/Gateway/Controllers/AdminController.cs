using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace Gateway.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет X-Admin-Secret. Возвращает null если ок, иначе ActionResult с 401.
    /// </summary>
    private ActionResult? CheckAdminSecret()
    {
        var secret = _configuration["ADMIN_SECRET"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("ADMIN_SECRET is not configured");
            return Unauthorized(new { message = "Admin access not configured" });
        }

        var header = Request.Headers["X-Admin-Secret"].FirstOrDefault();
        if (string.IsNullOrEmpty(header))
        {
            return Unauthorized(new { message = "Missing X-Admin-Secret header" });
        }
        if (header != secret)
        {
            return Unauthorized(new { message = "Invalid X-Admin-Secret" });
        }

        return null;
    }

    /// <summary>
    /// Создать организацию (компанию).
    /// </summary>
    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyAdminRequest request)
    {
        var fail = CheckAdminSecret();
        if (fail != null) return fail;

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        var baseUrl = _configuration["COMPANY_SERVICE_URL"] ?? "http://companyservice:5002";
        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl.TrimEnd('/')}/api/companies";

        try
        {
            var payload = new { name = request.Name.Trim() };
            var response = await client.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CompanyService create failed: {StatusCode} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, TryParseJson(content));
            }

            return StatusCode((int)response.StatusCode, TryParseJson(content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create company");
            return StatusCode(500, new { message = "Failed to create company", error = ex.Message });
        }
    }

    /// <summary>
    /// Создать начальника организации (логин/пароль).
    /// </summary>
    [HttpPost("companies/{companyId:int}/boss")]
    public async Task<IActionResult> CreateBoss(int companyId, [FromBody] CreateBossAdminRequest request)
    {
        var fail = CheckAdminSecret();
        if (fail != null) return fail;

        if (request == null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and Password are required" });
        }

        var baseUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl.TrimEnd('/')}/api/users";
        var adminSecret = _configuration["ADMIN_SECRET"] ?? "";

        var payload = new
        {
            companyId,
            email = request.Email.Trim(),
            password = request.Password,
            firstName = request.FirstName ?? string.Empty,
            lastName = request.LastName ?? string.Empty,
            role = "Boss"
        };

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.TryAddWithoutValidation("X-Admin-Secret", adminSecret);
            req.Content = System.Net.Http.Json.JsonContent.Create(payload);
            var response = await client.SendAsync(req);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserService create boss failed: {StatusCode} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, TryParseJson(content));
            }

            try
            {
                var doc = JsonDocument.Parse(content);
                var r = doc.RootElement;
                var dto = new
                {
                    id = r.GetProperty("id").GetInt32(),
                    companyId = r.GetProperty("companyId").GetInt32(),
                    email = r.GetProperty("email").GetString(),
                    firstName = r.GetProperty("firstName").GetString() ?? "",
                    lastName = r.GetProperty("lastName").GetString() ?? ""
                };
                return StatusCode((int)response.StatusCode, dto);
            }
            catch
            {
                return StatusCode((int)response.StatusCode, TryParseJson(content));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create boss user");
            return StatusCode(500, new { message = "Failed to create boss user", error = ex.Message });
        }
    }

    private static object? TryParseJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var t = content.Trim();
        if ((t.StartsWith("{") && t.EndsWith("}")) || (t.StartsWith("[") && t.EndsWith("]")))
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            catch
            {
                // ignore
            }
        }

        return new { message = content };
    }
}

public class CreateCompanyAdminRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateBossAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
