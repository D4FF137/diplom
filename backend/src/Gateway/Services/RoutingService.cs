using System.Net.Http.Headers;
using System.Text;

namespace Gateway.Services;

public class RoutingService : IRoutingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public RoutingService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HttpResponseMessage> RouteToUserServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToCompanyServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["COMPANY_SERVICE_URL"] ?? "http://companyservice:5002";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToFeedServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["FEED_SERVICE_URL"] ?? "http://feedservice:5003";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToChatServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToNotificationServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["NOTIFICATION_SERVICE_URL"] ?? "http://notificationservice:5005";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToStorageServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["STORAGE_SERVICE_URL"] ?? "http://storageservice:5006";
        return await RouteAsync(request, baseUrl, path);
    }

    public async Task<HttpResponseMessage> RouteToTaskServiceAsync(HttpRequest request, string path)
    {
        var baseUrl = _configuration["TASK_SERVICE_URL"] ?? "http://taskservice:5007";
        return await RouteAsync(request, baseUrl, path);
    }

    private async Task<HttpResponseMessage> RouteAsync(HttpRequest request, string baseUrl, string path)
    {
        var client = _httpClientFactory.CreateClient("proxy");
        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

        var requestMessage = new HttpRequestMessage();
        
        // Copy method
        requestMessage.Method = new HttpMethod(request.Method);
        
        // Copy headers (except Host and Content-* which will be set by Content)
        foreach (var header in request.Headers)
        {
            if (header.Key != "Host" && 
                !header.Key.StartsWith("Content-") && 
                !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Copy query string
        if (request.QueryString.HasValue)
        {
            url += request.QueryString.Value;
        }

        requestMessage.RequestUri = new Uri(url);

        // Copy body for POST, PUT, PATCH
        if (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
        {
            // Check if this is a multipart/form-data request
            var contentTypeHeader = request.ContentType;
            var contentType = contentTypeHeader?.ToString() ?? "application/json";
            
            // Handle multipart/form-data specially - check the string directly
            bool isMultipart = contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
            
            if (isMultipart)
            {
                // For multipart/form-data, we need to copy the stream directly
                MemoryStream? bodyStream = new MemoryStream();
                await request.Body.CopyToAsync(bodyStream);
                var length = bodyStream.Length;
                bodyStream.Position = 0;
                
                Console.WriteLine($"[RoutingService] Routing multipart request to {url}. Length: {length} bytes");

                // Create StreamContent with the original Content-Type (including boundary)
                var streamContent = new StreamContent(bodyStream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                requestMessage.Content = streamContent;
            }
            else
            {
                // For other content types (JSON, etc.), read as string
                string bodyContent;
                
                // Ensure we can read the body multiple times
                if (!request.Body.CanSeek)
                {
                    // If body is not seekable, we need to read it into a buffer
                    using var ms = new MemoryStream();
                    await request.Body.CopyToAsync(ms);
                    ms.Position = 0;
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    bodyContent = await reader.ReadToEndAsync();
                }
                else
                {
                    // Body is seekable, read it
                    request.Body.Position = 0;
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    bodyContent = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
                
                // Set content if body is not empty
                if (!string.IsNullOrWhiteSpace(bodyContent))
                {
                    requestMessage.Content = new StringContent(bodyContent, Encoding.UTF8, contentType);
                }
            }
        }

        try
        {
            return await client.SendAsync(requestMessage);
        }
        catch (HttpRequestException ex)
        {
            // Логируем ошибку подключения
            throw new HttpRequestException($"Failed to connect to {baseUrl}: {ex.Message}", ex);
        }
    }
}

